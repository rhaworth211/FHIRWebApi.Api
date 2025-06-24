using FHIRWebApi.Application.DTOs;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace FHIRWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ObservationsController : ControllerBase
    {
        private readonly FhirClient _fhirClient;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ObservationsController> _logger;

        public ObservationsController(FhirClient fhirClient, IDistributedCache cache, ILogger<ObservationsController> logger)
        {
            _fhirClient = fhirClient;
            _cache = cache;
            _logger = logger;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Observation>>> GetObservations([FromQuery] string? patientId = null)
        {
            if (!string.IsNullOrEmpty(patientId))
            {
                var key = $"observation:patient:{patientId}";
                var cached = await _cache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(cached))
                {
                    _logger.LogInformation("Cache hit for patient observation {PatientId}", patientId);
                    var obs = JsonConvert.DeserializeObject<List<Observation>>(cached);
                    return Ok(obs);
                }

                try
                {
                    var obs = await _fhirClient.ReadAsync<Observation>($"Observation/{patientId}");
                    var result = new List<Observation> { obs };

                    await _cache.SetStringAsync(key, JsonConvert.SerializeObject(result), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });

                    return Ok(result);
                }
                catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
            }

            const string cacheKey = "observations:latest";
            var cachedAll = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedAll))
            {
                _logger.LogInformation("Cache hit for all observations");
                var obs = JsonConvert.DeserializeObject<List<Observation>>(cachedAll);
                return Ok(obs);
            }

            var searchParams = new SearchParams().LimitTo(50);
            var bundle = await _fhirClient.SearchAsync<Observation>(searchParams);

            if (bundle?.Entry == null)
                return Ok(new List<Observation>());

            var observations = bundle.Entry
                .Where(e => e.Resource is Observation)
                .Select(e => (Observation)e.Resource!)
                .ToList();

            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(observations), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(observations);
        }

        [Authorize]
        [HttpGet("{id}")]
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
                var observation = await _fhirClient.ReadAsync<Observation>($"Observation/{id}");

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

        [Authorize]
        [HttpPost]
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

            var created = await _fhirClient.CreateAsync(fhirObservation);

            // Invalidate caches
            await _cache.RemoveAsync("observations:latest");
            if (!string.IsNullOrEmpty(observation.SubjectId))
                await _cache.RemoveAsync($"observation:patient:{observation.SubjectId.Replace("Patient/", "")}");

            return CreatedAtAction(nameof(GetObservation), new { id = created.Id }, created);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<Observation>> UpdateObservation(string id, [FromBody] Observation observation)
        {
            if (id != observation.Id)
                return BadRequest("Mismatched Observation ID");

            var updated = await _fhirClient.UpdateAsync(observation);

            // Invalidate caches
            await _cache.RemoveAsync($"observation:{id}");
            await _cache.RemoveAsync("observations:latest");
            if (!string.IsNullOrEmpty(observation.Subject?.Reference))
                await _cache.RemoveAsync($"observation:patient:{observation.Subject.Reference.Replace("Patient/", "")}");

            return Ok(updated);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteObservation(string id)
        {
            try
            {
                await _fhirClient.DeleteAsync($"Observation/{id}");

                // Invalidate caches
                await _cache.RemoveAsync($"observation:{id}");
                await _cache.RemoveAsync("observations:latest");

                return NoContent();
            }
            catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }
    }
}
