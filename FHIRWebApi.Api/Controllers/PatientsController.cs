using FHIRWebApi.Api.Controllers;
using FHIRWebApi.Application.DTOs;
using FHIRWebApi.Services;
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
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PatientsController : ControllerBase, IPatientController
    {
        private readonly IFhirPatientService _fhirService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(IFhirPatientService fhirService, IDistributedCache cache, ILogger<PatientsController> logger)
        {
            _fhirService = fhirService;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a list of FHIR patients from the server, with optional limit.
        /// </summary>
        /// <param name="limit">Maximum number of patients to return. Defaults to 20.</param>
        /// <returns>List of Patient resources.</returns>
        /// <response code="200">List of patients retrieved successfully.</response>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Patient>), 200)]
        public async Task<ActionResult<IEnumerable<Patient>>> GetPatients([FromQuery] int limit = 20)
        {
            string cacheKey = $"patients:latest:{limit}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("Cache hit for patient list with limit {Limit}", limit);
                var cachedPatients = JsonConvert.DeserializeObject<List<Patient>>(cached);
                return Ok(cachedPatients);
            }

            var searchParams = new SearchParams().LimitTo(limit);
            var bundle = await _fhirService.SearchPatientsAsync(searchParams);

            if (bundle?.Entry == null)
                return Ok(new List<Patient>());

            var patients = bundle.Entry
                .Where(e => e.Resource is Patient)
                .Select(e => (Patient)e.Resource!)
                .ToList();

            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(patients), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return Ok(patients);
        }


        /// <summary>
        /// Retrieves a patient by their FHIR ID.
        /// </summary>
        /// <param name="id">The unique FHIR Patient ID.</param>
        /// <returns>The requested Patient resource.</returns>
        /// <response code="200">Patient found and returned.</response>
        /// <response code="404">Patient with the given ID does not exist.</response>
        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Patient), 200)]
        [ProducesResponseType(404)]
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
                var patient = await _fhirService.ReadPatientAsync(id);

                await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(patient), new DistributedCacheEntryOptions
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
        /// Creates a new FHIR Patient using provided data.
        /// </summary>
        /// <param name="patient">DTO containing new patient information.</param>
        /// <returns>The newly created Patient resource.</returns>
        /// <response code="201">Patient created successfully.</response>
        /// <response code="400">Invalid or missing patient data.</response>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(Patient), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Patient>> CreatePatient([FromBody] CreatePatientRequest patient)
        {
            if (patient == null)
                return BadRequest("Patient data is required.");

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

            var created = await _fhirService.CreatePatientAsync(fhirPatient);

            if (created == null)
                return BadRequest("Failed to create patient.");

            await _cache.RemoveAsync("patients:latest");

            return CreatedAtAction(nameof(GetPatient), new { id = created.Id }, created);
        }

        /// <summary>
        /// Updates an existing FHIR patient by ID.
        /// </summary>
        /// <param name="id">The patient ID to update.</param>
        /// <param name="updatedPatient">The updated patient resource.</param>
        /// <returns>The updated patient object.</returns>
        /// <response code="200">Patient updated successfully.</response>
        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Patient), 200)]
        public async Task<ActionResult<Patient>> UpdatePatient(string id, [FromBody] Patient updatedPatient)
        {
            updatedPatient.Id = id;
            var result = await _fhirService.UpdatePatientAsync(updatedPatient);

            await _cache.RemoveAsync($"patient:{id}");
            await _cache.RemoveAsync("patients:latest");

            return Ok(result);
        }

        /// <summary>
        /// Deletes a FHIR patient by ID.
        /// </summary>
        /// <param name="id">The ID of the patient to delete.</param>
        /// <returns>No content on success; 404 if patient not found.</returns>
        /// <response code="204">Patient deleted successfully.</response>
        /// <response code="404">Patient not found.</response>
        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePatient(string id)
        {
            try
            {
                await _fhirService.DeletePatientAsync(id);

                await _cache.RemoveAsync($"patient:{id}");
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
