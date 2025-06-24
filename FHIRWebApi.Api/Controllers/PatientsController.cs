using FHIRWebApi.Api.Controllers;
using FHIRWebApi.Application.DTOs;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace FHIRHelpers.Api.Controllers
{    

    /// <summary>
    /// API controller for managing FHIR Patient resources.
    /// Provides endpoints to create, read, update, and delete patients via FHIR server.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase, IPatientController
    {        
        private readonly FhirClient _fhirClient;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PatientsController> _logger;

        /// <summary>
        /// Constructs a new instance of the <see cref="PatientsController"/>.
        /// </summary>
        /// <param name="fhirClient">Injected FHIR client used to interact with the FHIR server.</param>
        public PatientsController(FhirClient fhirClient, IDistributedCache cache, ILogger<PatientsController> logger)
        {
            _fhirClient = fhirClient;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the latest 20 patients using a summary-only FHIR query.
        /// </summary>
        /// <returns>A list of FHIR Patient resources.</returns>
        [Authorize]
        [HttpGet()]
        public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
        {
            const string cacheKey = "patients:latest";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("Cache hit for patient list.");
                var cachedPatients = JsonConvert.DeserializeObject<List<Patient>>(cached);
                return Ok(cachedPatients);
            }

            var searchParams = new SearchParams().LimitTo(20).SummaryOnly();
            var bundle = await _fhirClient.SearchAsync<Patient>(searchParams);

            if (bundle?.Entry == null)
            {
                return Ok(new List<Patient>());
            }

            var patients = bundle.Entry
                .Where(e => e.Resource is Patient)
                .Select(e => (Patient)e.Resource!)
                .ToList();

            return Ok(patients);
        }

        /// <summary>
        /// Retrieves a patient by its FHIR ID.
        /// </summary>
        /// <param name="id">The FHIR Patient ID.</param>
        /// <returns>The requested Patient or 404 if not found.</returns>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(string id)
        {
            var cacheKey = $"patient:{id}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("Cache hit for patient {id}", id);
                var patient = JsonConvert.DeserializeObject<Patient>(cached);
                return Ok(patient);
            }

            try
            {
                var patient = await _fhirClient.ReadAsync<Patient>($"Patient/{id}");

                var json = JsonConvert.SerializeObject(patient); 
                await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                return Ok(patient);
            }
            catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound($"Patient with ID {id} not found.");
            }
        }

        /// <summary>
        /// Creates a new patient from input DTO and returns the created FHIR Patient.
        /// </summary>
        /// <param name="patient">The patient data to create.</param>
        /// <returns>The created patient resource with HTTP 201 or an error response.</returns>
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Patient>> CreatePatient([FromBody] CreatePatientRequest patient)
        {
            if (patient == null)
            {
                return BadRequest("Patient data is required.");
            }

            var fhirPatient = new Patient
            {
                Name = new List<HumanName>
                {
                    new HumanName
                    {
                        Use = HumanName.NameUse.Official,
                        Given = string.IsNullOrWhiteSpace(patient.GivenName) ? null : new List<string> { patient.GivenName },
                        Family = patient.FamilyName
                    }
                },
                Gender = Enum.TryParse<AdministrativeGender>(patient.Gender, true, out var gender)
                    ? gender
                    : AdministrativeGender.Unknown,
                BirthDate = patient.BirthDate
            };

            var created = await _fhirClient.CreateAsync(fhirPatient);

            if (created == null)
            {
                return BadRequest("Failed to create patient.");
            }

            await _cache.RemoveAsync("patients:latest");

            return CreatedAtAction(nameof(GetPatient), new { id = created.Id }, created);
        }

        /// <summary>
        /// Updates an existing patient resource using the provided ID and patient payload.
        /// </summary>
        /// <param name="id">The FHIR Patient ID to update.</param>
        /// <param name="updatedPatient">The updated patient resource.</param>
        /// <returns>The updated patient object.</returns>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<Patient>> UpdatePatient(string id, [FromBody] Patient updatedPatient)
        {
            updatedPatient.Id = id;
            var result = await _fhirClient.UpdateAsync(updatedPatient);

            var cacheKey = $"patient:{id}";
            await _cache.RemoveAsync(cacheKey);
            await _cache.RemoveAsync("patients:latest");

            return Ok(result);
        }

        /// <summary>
        /// Deletes a patient from the FHIR server by ID.
        /// </summary>
        /// <param name="id">The FHIR Patient ID to delete.</param>
        /// <returns>204 No Content if deleted, 404 if not found.</returns>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(string id)
        {
            try
            {
                await _fhirClient.DeleteAsync($"Patient/{id}");
                var cacheKey = $"patient:{id}";
                await _cache.RemoveAsync(cacheKey);
                await _cache.RemoveAsync("patients:latest");
                return NoContent();
            }
            catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound($"Patient with ID {id} not found.");
            }
        }
    }
}
