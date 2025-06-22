using System.ComponentModel.DataAnnotations;

namespace FHIRWebApi.Application.DTOs
{
    public class CreatePatientRequest
    {
        [Required(ErrorMessage = "Given name is required.")]
        public string GivenName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Family name is required.")]
        public string FamilyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required.")]
        [RegularExpression("male|female|other|unknown", ErrorMessage = "Gender must be one of: male, female, other, unknown.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birth date is required.")]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Birth date must be in format YYYY-MM-DD.")]
        public string BirthDate { get; set; } = string.Empty;
    }
}