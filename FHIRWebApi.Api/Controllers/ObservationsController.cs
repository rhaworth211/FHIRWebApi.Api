using FHIRWebApi.Application.DTOs;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace FHIRWebApi.Controllers
{
    /// <summary>
    /// API controller for managing FHIR Observation resources.
    /// Supports CRUD operations with optional patient filtering.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ObservationsController : ControllerBase
    {
        private readonly FhirClient _fhirClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservationsController"/> class.
        /// </summary>
        /// <param name="fhirClient">The FHIR client used to communicate with the FHIR server.</param>
        public ObservationsController(FhirClient fhirClient)
        {
            _fhirClient = fhirClient;
        }

        /// <summary>
        /// Retrieves either a single observation by ID or the latest 50 observations.
        /// Optionally filters by a patient ID.
        /// </summary>
        /// <param name="patientId">Optional FHIR resource ID for a specific patient.</param>
        /// <returns>A list of matching observations or a single observation if patientId is provided.</returns>
        [Authorize]
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

        /// <summary>
        /// Retrieves a single observation by its FHIR ID.
        /// </summary>
        /// <param name="id">The FHIR ID of the observation to retrieve.</param>
        /// <returns>The requested observation or a 404 Not Found result.</returns>
        [Authorize]
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

        /// <summary>
        /// Creates a new FHIR Observation resource.
        /// </summary>
        /// <param name="observation">The DTO representing observation input data.</param>
        /// <returns>The created observation with HTTP 201 response.</returns>
        [Authorize]
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

        /// <summary>
        /// Updates an existing observation by ID.
        /// </summary>
        /// <param name="id">The ID of the observation to update.</param>
        /// <param name="observation">The updated observation payload.</param>
        /// <returns>The updated observation or a 400 Bad Request if IDs mismatch.</returns>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<Observation>> UpdateObservation(string id, [FromBody] Observation observation)
        {
            if (id != observation.Id)
                return BadRequest("Mismatched Observation ID");

            var updated = await _fhirClient.UpdateAsync(observation);
            return Ok(updated);
        }

        /// <summary>
        /// Deletes an observation by its FHIR ID.
        /// </summary>
        /// <param name="id">The ID of the observation to delete.</param>
        /// <returns>204 No Content if successful, 404 if not found.</returns>
        [Authorize]
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
