using FHIRWebApi.Application.DTOs;
using FHIRWebApi.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace FHIRWebApi.Controllers
{
    /// <summary>
    /// Manages FHIR Observation resources.
    /// Supports creation, retrieval, update, deletion, and filtering by patient.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ObservationsController : ControllerBase
    {
        private readonly IFhirObservationService _fhirService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ObservationsController> _logger;

        public ObservationsController(IFhirObservationService fhirService, IDistributedCache cache, ILogger<ObservationsController> logger)
        {
            _fhirService = fhirService;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Gets all FHIR observations or filters by patient ID.
        /// </summary>
        /// <param name="patientId">Optional patient ID to filter observations.</param>
        /// <returns>List of FHIR observations.</returns>
        /// <response code="200">Returns the list of observations.</response>
        /// <response code="404">No observations found for specified patient.</response>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Observation>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<Observation>>> GetObservations([FromQuery] string? patientId = null)
        {
            if (!string.IsNullOrEmpty(patientId))
            {
                var key = $"observation:patient:{patientId.ToLower()}";
                var cached = await _cache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(cached))
                {
                    _logger.LogInformation("Cache hit for patient observation {PatientId}", patientId);
                    var obs = JsonConvert.DeserializeObject<List<Observation>>(cached);
                    return Ok(obs);
                }

                var searchParams = new SearchParams().Where($"subject=Patient/{patientId}");
                var bundle = await _fhirService.SearchObservationsAsync(searchParams);

                var result = bundle?.Entry?
                    .Where(e => e.Resource is Observation)
                    .Select(e => (Observation)e.Resource!)
                    .ToList() ?? new List<Observation>();

                await _cache.SetStringAsync(key, JsonConvert.SerializeObject(result), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

                return Ok(result);
            }

            const string cacheKey = "observations:all";
            var cachedAll = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedAll))
            {
                _logger.LogInformation("Cache hit for all observations");
                var obs = JsonConvert.DeserializeObject<List<Observation>>(cachedAll);
                return Ok(obs);
            }

            var allSearchParams = new SearchParams();
            var allBundle = await _fhirService.SearchObservationsAsync(allSearchParams);

            var observations = allBundle?.Entry?
                .Where(e => e.Resource is Observation)
                .Select(e => (Observation)e.Resource!)
                .ToList() ?? new List<Observation>();

            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(observations), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(observations);
        }

        /// <summary>
        /// Gets a specific FHIR observation by ID.
        /// </summary>
        /// <param name="id">Observation ID.</param>
        /// <returns>The observation resource.</returns>
        /// <response code="200">Returns the observation.</response>
        /// <response code="404">Observation not found.</response>
        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Observation), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Observation>> GetObservation(string id)
        {
            var cacheKey = $"observation:{id}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("Cache hit for observation {id}", id);
                var observation = JsonConvert.DeserializeObject<Observation>(cached);
                return Ok(observation);
            }

            try
            {
                var observation = await _fhirService.ReadObservationAsync(id);

                await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(observation), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                return Ok(observation);
            }
            catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Creates a new FHIR observation.
        /// </summary>
        /// <param name="observation">Observation creation request.</param>
        /// <returns>The newly created observation resource.</returns>
        /// <response code="201">Observation created successfully.</response>
        /// <response code="400">Invalid request payload.</response>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(Observation), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Observation>> CreateObservation([FromBody] CreateObservationRequest observation)
        {
            if (observation == null)
                return BadRequest("Observation data is required.");

            var fhirObservation = new Observation
            {
                Status = ObservationStatus.Final,
                Subject = new ResourceReference { Reference = observation.SubjectId },
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

            var created = await _fhirService.CreateObservationAsync(fhirObservation);

            await _cache.RemoveAsync("observations:all");
            if (!string.IsNullOrEmpty(observation.SubjectId))
                await _cache.RemoveAsync($"observation:patient:{observation.SubjectId.Replace("Patient/", "").ToLower()}");

            return CreatedAtAction(nameof(GetObservation), new { id = created.Id }, created);
        }

        /// <summary>
        /// Updates an existing FHIR observation by ID.
        /// </summary>
        /// <param name="id">Observation ID to update.</param>
        /// <param name="observation">Updated observation resource.</param>
        /// <returns>The updated observation.</returns>
        /// <response code="200">Observation updated successfully.</response>
        /// <response code="400">Mismatched or invalid ID.</response>
        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Observation), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Observation>> UpdateObservation(string id, [FromBody] Observation observation)
        {
            if (id != observation.Id)
                return BadRequest("Mismatched Observation ID");

            var updated = await _fhirService.UpdateObservationAsync(observation);

            await _cache.RemoveAsync($"observation:{id}");
            await _cache.RemoveAsync("observations:all");
            if (!string.IsNullOrEmpty(observation.Subject?.Reference))
                await _cache.RemoveAsync($"observation:patient:{observation.Subject.Reference.Replace("Patient/", "").ToLower()}");

            return Ok(updated);
        }

        /// <summary>
        /// Deletes an observation by its FHIR ID.
        /// </summary>
        /// <param name="id">Observation ID to delete.</param>
        /// <returns>No content if deleted, or 404 if not found.</returns>
        /// <response code="204">Observation deleted.</response>
        /// <response code="404">Observation not found.</response>
        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteObservation(string id)
        {
            try
            {
                await _fhirService.DeleteObservationAsync(id);

                await _cache.RemoveAsync($"observation:{id}");
                await _cache.RemoveAsync("observations:all");

                return NoContent();
            }
            catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }
    }
}
