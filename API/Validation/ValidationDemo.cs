using FluentValidation;

namespace API.Validation;

public record ValidationDemoRequest(string Name, int Hours);

public class ValidationDemoRequestValidator : AbstractValidator<ValidationDemoRequest>
{
    public ValidationDemoRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Hours).InclusiveBetween(1, 40);
    }
}
