using System.ComponentModel.DataAnnotations;
using System.Linq;
using CompanyHubService.DTOs;

namespace CompanyHubService.Validation
{
    public class EnsureAtLeastOneProject : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var request = (CreateCompanyRequestDTO)validationContext.ObjectInstance;

            if (request.Portfolio == null || !request.Portfolio.Any())
            {
                return new ValidationResult("At least one previous project must be provided in Portfolio.");
            }

            return ValidationResult.Success;
        }
    }
}
