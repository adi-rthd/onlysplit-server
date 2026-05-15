using FluentValidation;

namespace OnlySplit.Application.Features.Groups;

public sealed class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(160);
    }
}

public sealed class InviteGroupRequestValidator : AbstractValidator<InviteGroupRequest>
{
    public InviteGroupRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(255);
    }
}

public sealed class JoinGroupRequestValidator : AbstractValidator<JoinGroupRequest>
{
    public JoinGroupRequestValidator()
    {
        RuleFor(request => request.InviteCode).NotEmpty().MaximumLength(128);
    }
}
