using System.ComponentModel.DataAnnotations;

namespace FHIRWebApi.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new FHIR Patient resource.
    /// Includes validation attributes for required fields and format constraints.
    /// </summary>
    public class CreatePatientRequest
    {
        /// <summary>
        /// The given (first) name of the patient.
        /// </summary>
        [Required(ErrorMessage = "Given name is required.")]
        public string GivenName { get; set; } = string.Empty;

        /// <summary>
        /// The family (last) name of the patient.
        /// </summary>
        [Required(ErrorMessage = "Family name is required.")]
        public string FamilyName { get; set; } = string.Empty;

        /// <summary>
        /// The administrative gender of the patient.
        /// Must be one of: male, female, other, unknown.
        /// </summary>
        [Required(ErrorMessage = "Gender is required.")]
        [RegularExpression("male|female|other|unknown", ErrorMessage = "Gender must be one of: male, female, other, unknown.")]
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// The birth date of the patient in ISO format (YYYY-MM-DD).
        /// </summary>
        [Required(ErrorMessage = "Birth date is required.")]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Birth date must be in format YYYY-MM-DD.")]
        public string BirthDate { get; set; } = string.Empty;
    }
}
