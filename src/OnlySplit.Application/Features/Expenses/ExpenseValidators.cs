using FluentValidation;
using OnlySplit.Domain.Constants;

namespace OnlySplit.Application.Features.Expenses;

public sealed class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(request => request.GroupId).NotEmpty();
        RuleFor(request => request.Title).NotEmpty().MaximumLength(180);
        RuleFor(request => request.Description).MaximumLength(1000);
        RuleFor(request => request.Amount).GreaterThan(0);
        RuleFor(request => request.Category).NotEmpty().MaximumLength(80);
        RuleFor(request => request.SplitType)
            .NotEmpty()
            .Must(value => SplitTypes.All.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SplitType must be equal, percentage, or exact.");

        RuleForEach(request => request.Splits).SetValidator(new SplitInputDtoValidator());

        RuleFor(request => request)
            .Custom((request, context) =>
            {
                var splitType = request.SplitType.ToLowerInvariant();
                var splits = request.Splits ?? Array.Empty<SplitInputDto>();

                if (splitType is SplitTypes.Percentage or SplitTypes.Exact && splits.Count == 0)
                {
                    context.AddFailure("Splits", "Splits are required for percentage and exact splits.");
                }

                if (splitType == SplitTypes.Percentage)
                {
                    var totalPercentage = splits.Sum(split => split.Percentage ?? 0);
                    if (totalPercentage != 100)
                    {
                        context.AddFailure("Splits", "Percentage splits must total 100.");
                    }
                }

                if (splitType == SplitTypes.Exact)
                {
                    var totalAmount = splits.Sum(split => split.Amount ?? 0);
                    if (decimal.Round(totalAmount, 2) != decimal.Round(request.Amount, 2))
                    {
                        context.AddFailure("Splits", "Exact split amounts must equal the expense amount.");
                    }
                }
            });
    }
}

public sealed class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
{
    public UpdateExpenseRequestValidator()
    {
        RuleFor(request => request.Title).MaximumLength(180);
        RuleFor(request => request.Description).MaximumLength(1000);
        RuleFor(request => request.Amount).GreaterThan(0).When(request => request.Amount.HasValue);
        RuleFor(request => request.Category).MaximumLength(80);
        RuleFor(request => request.SplitType)
            .Must(value => value is null || SplitTypes.All.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SplitType must be equal, percentage, or exact.");
        RuleForEach(request => request.Splits).SetValidator(new SplitInputDtoValidator());
    }
}

public sealed class SplitInputDtoValidator : AbstractValidator<SplitInputDto>
{
    public SplitInputDtoValidator()
    {
        RuleFor(split => split.UserId).NotEmpty();
        RuleFor(split => split.Amount).GreaterThan(0).When(split => split.Amount.HasValue);
        RuleFor(split => split.Percentage).GreaterThan(0).LessThanOrEqualTo(100).When(split => split.Percentage.HasValue);
    }
}
