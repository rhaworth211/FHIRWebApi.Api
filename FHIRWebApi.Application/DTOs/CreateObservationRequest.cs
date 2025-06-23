using System.ComponentModel.DataAnnotations;

namespace FHIRWebApi.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new FHIR Observation resource.
    /// Includes validation annotations for all required fields.
    /// </summary>
    public class CreateObservationRequest
    {
        /// <summary>
        /// The FHIR subject reference (usually a patient), e.g., "Patient/123".
        /// </summary>
        [Required(ErrorMessage = "Subject ID is required.")]
        public string SubjectId { get; set; } = string.Empty;

        /// <summary>
        /// The code system URI that defines the coding namespace, e.g., "http://loinc.org".
        /// </summary>
        [Required(ErrorMessage = "Code system is required.")]
        public string CodeSystem { get; set; } = string.Empty;

        /// <summary>
        /// The actual code value from the code system, e.g., "85354-9".
        /// </summary>
        [Required(ErrorMessage = "Code value is required.")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable display string for the code, e.g., "Blood pressure panel".
        /// </summary>
        [Required(ErrorMessage = "Code display is required.")]
        public string CodeDisplay { get; set; } = string.Empty;

        /// <summary>
        /// The observation result value (numeric or textual).
        /// </summary>
        [Required(ErrorMessage = "Value is required.")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// The unit string for the value, e.g., "mmHg".
        /// </summary>
        [Required(ErrorMessage = "Unit is required.")]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// The system URI for the unit of measure, defaulted to "http://unitsofmeasure.org".
        /// </summary>
        [Required(ErrorMessage = "Unit system is required.")]
        public string UnitSystem { get; set; } = "http://unitsofmeasure.org";

        /// <summary>
        /// The machine-readable unit code, e.g., "mm[Hg]".
        /// </summary>
        [Required(ErrorMessage = "Unit code is required.")]
        public string UnitCode { get; set; } = string.Empty;

        /// <summary>
        /// The effective date of the observation in format YYYY-MM-DD.
        /// </summary>
        [Required(ErrorMessage = "Effective date is required.")]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Date must be in format YYYY-MM-DD.")]
        public string EffectiveDate { get; set; } = string.Empty;
    }
}
