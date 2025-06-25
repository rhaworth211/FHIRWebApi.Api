using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Task = System.Threading.Tasks.Task;

namespace FHIRWebApi.Services
{
    public class FhirObservationService : IFhirObservationService
    {
        private readonly FhirClient _fhirClient;

        public FhirObservationService(FhirClient fhirClient)
        {
            _fhirClient = fhirClient;
        }

        public Task<Bundle?> SearchObservationsAsync(SearchParams parameters) =>
            _fhirClient.SearchAsync<Observation>(parameters);

        public Task<Observation?> ReadObservationAsync(string id) =>
            _fhirClient.ReadAsync<Observation>($"Observation/{id}");

        public Task<Observation?> CreateObservationAsync(Observation observation) =>
            _fhirClient.CreateAsync(observation);

        public Task<Observation?> UpdateObservationAsync(Observation observation) =>
            _fhirClient.UpdateAsync(observation);

        public Task DeleteObservationAsync(string id) =>
            _fhirClient.DeleteAsync($"Observation/{id}");
    }
}
