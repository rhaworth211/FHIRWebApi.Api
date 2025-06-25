using FHIRWebApi.Application.DTOs;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;

namespace FHIRWebApi.Api.Controllers
{
    /// <summary>
    /// Interface for managing FHIR Patient resources.
    /// </summary>
    public interface IPatientController
    {
        /// <summary>
        /// Retrieves a list of FHIR Patient resources.
        /// </summary>
        /// <param name="limit">Maximum number of patients to return. Defaults to 20.</param>
        /// <returns>List of FHIR Patient resources.</returns>
        Task<ActionResult<IEnumerable<Patient>>> GetPatients(int limit = 20);

        /// <summary>
        /// Retrieves a specific patient by ID.
        /// </summary>
        /// <param name="id">The FHIR Patient ID.</param>
        /// <returns>The requested Patient resource.</returns>
        Task<ActionResult<Patient>> GetPatient(string id);

        /// <summary>
        /// Creates a new Patient resource.
        /// </summary>
        /// <param name="newPatient">DTO representing the new patient.</param>
        /// <returns>The created Patient resource.</returns>
        Task<ActionResult<Patient>> CreatePatient(CreatePatientRequest newPatient);

        /// <summary>
        /// Updates an existing Patient resource by ID.
        /// </summary>
        /// <param name="id">The Patient ID to update.</param>
        /// <param name="updatedPatient">The updated Patient resource.</param>
        /// <returns>The updated Patient resource.</returns>
        Task<ActionResult<Patient>> UpdatePatient(string id, Patient updatedPatient);

        /// <summary>
        /// Deletes a Patient resource by ID.
        /// </summary>
        /// <param name="id">The Patient ID to delete.</param>
        /// <returns>No content if successful, or not found.</returns>
        Task<IActionResult> DeletePatient(string id);
    }
}
