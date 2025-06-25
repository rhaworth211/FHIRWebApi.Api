using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Task = System.Threading.Tasks.Task;

namespace FHIRWebApi.Services
{
    public class FhirPatientService : IFhirPatientService
    {
        private readonly FhirClient _fhirClient;

        public FhirPatientService(FhirClient fhirClient)
        {
            _fhirClient = fhirClient;
        }

        public Task<Bundle?> SearchPatientsAsync(SearchParams parameters) =>
            _fhirClient.SearchAsync<Patient>(parameters);

        public Task<Patient?> ReadPatientAsync(string id) =>
            _fhirClient.ReadAsync<Patient>($"Patient/{id}");

        public Task<Patient?> CreatePatientAsync(Patient patient) =>
            _fhirClient.CreateAsync(patient);

        public Task<Patient?> UpdatePatientAsync(Patient patient) =>
            _fhirClient.UpdateAsync(patient);

        public Task DeletePatientAsync(string id) =>
            _fhirClient.DeleteAsync($"Patient/{id}");
    }
}
