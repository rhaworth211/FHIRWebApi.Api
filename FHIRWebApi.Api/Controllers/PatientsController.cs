using FHIRWebApi.Application.DTOs;
using FHIRWebApi.Api.Controllers;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Mvc;

namespace FHIRHelpers.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase, IPatientController
    {
        private readonly FhirClient _fhirClient;

        public PatientsController(FhirClient fhirClient)
        {
            _fhirClient = fhirClient;

        }

        // GET: api/Patients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
        {
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

        // GET: api/Patients/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(string id)
        {
            try
            {
                var patient = await _fhirClient.ReadAsync<Patient>($"Patient/{id}");
                return Ok(patient);
            }
            catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound($"Patient with ID {id} not found.");
            }
        }

        // POST: api/Patients
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

            return CreatedAtAction(nameof(GetPatient), new { id = created.Id }, created);
        }

        // PUT: api/Patients/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Patient>> UpdatePatient(string id, [FromBody] Patient updatedPatient)
        {
            updatedPatient.Id = id;
            var result = await _fhirClient.UpdateAsync(updatedPatient);
            return Ok(result);
        }

        // DELETE: api/Patients/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(string id)
        {
            try
            {
                await _fhirClient.DeleteAsync($"Patient/{id}");
                return NoContent();
            }
            catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound($"Patient with ID {id} not found.");
            }
        }
    }
}
