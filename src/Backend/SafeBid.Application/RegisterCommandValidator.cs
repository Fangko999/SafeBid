using FluentValidation;

namespace SafeBid.Application;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("The passwords do not match.");
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty();
    }
}
