using FluentValidation;
using QuizBattle.Application.Features.Users.Commands;

namespace QuizBattle.Application.Features.Users.Validators
{
    public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.GoogleId) || !string.IsNullOrWhiteSpace(x.AppleId))
                .WithMessage("Either GoogleId or AppleId must be provided.");

            When(x => !string.IsNullOrWhiteSpace(x.GoogleId), () =>
            {
                RuleFor(x => x.GoogleId)
                    .MaximumLength(100).WithMessage("GoogleId cannot exceed 100 characters.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.AppleId), () =>
            {
                RuleFor(x => x.AppleId)
                    .MaximumLength(100).WithMessage("AppleId cannot exceed 100 characters.");
            });

            RuleFor(x => x.FirstName)
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.")
                .When(x => x.FirstName is not null);

            RuleFor(x => x.LastName)
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.")
                .When(x => x.LastName is not null);

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format.")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Photo)
                .MaximumLength(500).WithMessage("Photo URL cannot exceed 500 characters.")
                .When(x => x.Photo is not null);
        }
    }
}