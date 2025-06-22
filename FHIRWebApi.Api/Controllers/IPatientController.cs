using FHIRWebApi.Application.DTOs;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;

namespace FHIRWebApi.Api.Controllers
{
    public interface IPatientController
    {
        Task<ActionResult<IEnumerable<Patient>>> GetPatients();
        Task<ActionResult<Patient>> GetPatient(string id);
        Task<ActionResult<Patient>> CreatePatient(CreatePatientRequest newPatient);
        Task<ActionResult<Patient>> UpdatePatient(string id, Patient updatedPatient);
        Task<IActionResult> DeletePatient(string id);
    }
}
