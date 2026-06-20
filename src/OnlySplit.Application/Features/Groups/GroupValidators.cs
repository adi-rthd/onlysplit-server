using FluentValidation;

namespace OnlySplit.Application.Features.Groups;

public sealed class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupRequestValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty()
            .MaximumLength(160)
            .When(r => r.Name is not null);

        RuleFor(r => r.Description)
            .MaximumLength(500)
            .When(r => r.Description is not null);

        RuleFor(r => r.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a 3-letter uppercase ISO 4217 code.")
            .When(r => r.Currency is not null);
    }
}
