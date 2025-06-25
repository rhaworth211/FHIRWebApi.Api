using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Task = System.Threading.Tasks.Task;

namespace FHIRWebApi.Services
{
    public interface IFhirPatientService
    {
        Task<Bundle?> SearchPatientsAsync(SearchParams parameters);
        Task<Patient?> ReadPatientAsync(string id);
        Task<Patient?> CreatePatientAsync(Patient patient);
        Task<Patient?> UpdatePatientAsync(Patient patient);
        Task DeletePatientAsync(string id);
    }
}
