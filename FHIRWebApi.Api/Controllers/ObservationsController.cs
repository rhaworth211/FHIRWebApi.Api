using FHIRWebApi.Application.DTOs;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace FHIRWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ObservationsController : ControllerBase
    {
        private readonly FhirClient _fhirClient;

        public ObservationsController(FhirClient fhirClient)
        {
            _fhirClient = fhirClient;
        }

        // GET: api/observations
        // GET: api/observations?patientId=123
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Observation>>> GetObservations([FromQuery] string? patientId = null)
        {
            if (!string.IsNullOrEmpty(patientId))
            {
                try
                {
                    var observation = await _fhirClient.ReadAsync<Observation>($"Observation/{patientId}");
                    return Ok(new List<Observation> { observation });
                }
                catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
            }

            // Default: return latest 50
            var searchParams = new SearchParams().LimitTo(50);
            var bundle = await _fhirClient.SearchAsync<Observation>(searchParams);

            if (bundle?.Entry == null)
                return Ok(new List<Observation>());

            var observations = bundle.Entry
                .Where(e => e.Resource is Observation)
                .Select(e => (Observation)e.Resource!)
                .ToList();

            return Ok(observations);
        }

        // GET: api/observations/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Observation>> GetObservation(string id)
        {
            try
            {
                var observation = await _fhirClient.ReadAsync<Observation>($"Observation/{id}");
                return Ok(observation);
            }
            catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }

        // POST: api/observations
        [HttpPost]
        public async Task<ActionResult<Observation>> CreateObservation([FromBody] CreateObservationRequest observation)
        {
            if (observation == null)
                return BadRequest("Observation data is required.");

            var fhirObservation = new Observation
            {                
                Status = ObservationStatus.Final,
                Subject = new ResourceReference
                {
                    Reference = observation.SubjectId // e.g. "Patient/123"
                },
                Code = new CodeableConcept
                {
                    Coding = new List<Coding>
            {
                new Coding
                {
                    System = observation.CodeSystem,
                    Code = observation.Code,
                    Display = observation.CodeDisplay
                }
            },
                    Text = observation.CodeDisplay
                },
                Value = new Quantity
                {
                    Value = decimal.TryParse(observation.Value, out var val) ? val : null,
                    Unit = observation.Unit,
                    System = observation.UnitSystem,
                    Code = observation.UnitCode
                },
                Effective = new FhirDateTime(observation.EffectiveDate)
            };

            var created = await _fhirClient.CreateAsync(fhirObservation);
            return CreatedAtAction(nameof(GetObservation), new { id = created.Id }, created);
        }

        // PUT: api/observations/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Observation>> UpdateObservation(string id, [FromBody] Observation observation)
        {
            if (id != observation.Id)
                return BadRequest("Mismatched Observation ID");

            var updated = await _fhirClient.UpdateAsync(observation);
            return Ok(updated);
        }

        // DELETE: api/observations/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteObservation(string id)
        {
            try
            {
                await _fhirClient.DeleteAsync($"Observation/{id}");
                return NoContent();
            }
            catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }
    }
}
