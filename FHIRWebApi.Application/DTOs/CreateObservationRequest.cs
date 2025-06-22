using System.ComponentModel.DataAnnotations;

namespace FHIRWebApi.Application.DTOs
{
    public class CreateObservationRequest
    {
        [Required(ErrorMessage = "Subject ID is required.")]
        public string SubjectId { get; set; } = string.Empty; // e.g. "Patient/123"

        [Required(ErrorMessage = "Code system is required.")]
        public string CodeSystem { get; set; } = string.Empty; // e.g. "http://loinc.org"

        [Required(ErrorMessage = "Code value is required.")]
        public string Code { get; set; } = string.Empty; // e.g. "85354-9"

        [Required(ErrorMessage = "Code display is required.")]
        public string CodeDisplay { get; set; } = string.Empty; // e.g. "Blood pressure panel"

        [Required(ErrorMessage = "Value is required.")]
        public string Value { get; set; } = string.Empty; // can be numeric or text

        [Required(ErrorMessage = "Unit is required.")]
        public string Unit { get; set; } = string.Empty; // e.g. "mmHg"

        [Required(ErrorMessage = "Unit system is required.")]
        public string UnitSystem { get; set; } = "http://unitsofmeasure.org";

        [Required(ErrorMessage = "Unit code is required.")]
        public string UnitCode { get; set; } = string.Empty; // e.g. "mm[Hg]"

        [Required(ErrorMessage = "Effective date is required.")]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Date must be in format YYYY-MM-DD.")]
        public string EffectiveDate { get; set; } = string.Empty;
    }
}