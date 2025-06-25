using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Task = System.Threading.Tasks.Task;

namespace FHIRWebApi.Services
{
    public interface IFhirObservationService
    {
        Task<Bundle?> SearchObservationsAsync(SearchParams parameters);
        Task<Observation?> ReadObservationAsync(string id);
        Task<Observation?> CreateObservationAsync(Observation observation);
        Task<Observation?> UpdateObservationAsync(Observation observation);
        Task DeleteObservationAsync(string id);
    }
}