using FluentValidation;
using QuizBattle.Application.Features.Auth.Commands;

namespace QuizBattle.Application.Features.Auth.Validators
{
    public sealed class LoginWithGoogleCommandValidator : AbstractValidator<LoginWithGoogleCommand>
    {
        public LoginWithGoogleCommandValidator()
        {
            RuleFor(x => x.IdToken)
                .NotEmpty().WithMessage("Google ID token is required.");

            RuleFor(x => x.DeviceToken)
                .MaximumLength(500).WithMessage("Device token cannot exceed 500 characters.")
                .When(x => x.DeviceToken is not null);

            RuleFor(x => x.DeviceInfo)
                .MaximumLength(500).WithMessage("Device info cannot exceed 500 characters.")
                .When(x => x.DeviceInfo is not null);
        }
    }
}