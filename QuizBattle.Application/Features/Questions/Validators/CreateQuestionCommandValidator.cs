using FluentValidation;
using QuizBattle.Application.Features.Questions.Commands;

namespace QuizBattle.Application.Features.Questions.Validators
{
    public sealed class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
    {
        private static readonly string[] SupportedLanguages = { "sr", "en" };

        public CreateQuestionCommandValidator()
        {
            RuleFor(x => x.LanguageCode)
                .NotEmpty().WithMessage("Language code is required.")
                .Length(2).WithMessage("Language code must be exactly 2 characters.")
                .Must(code => SupportedLanguages.Contains(code.ToLower()))
                .WithMessage("Language must be 'sr' or 'en'.");

            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Question text is required.")
                .MaximumLength(1000).WithMessage("Question text cannot exceed 1000 characters.");

            RuleFor(x => x.AnswerA)
                .NotEmpty().WithMessage("Answer A is required.")
                .MaximumLength(500).WithMessage("Answer A cannot exceed 500 characters.");

            RuleFor(x => x.AnswerB)
                .NotEmpty().WithMessage("Answer B is required.")
                .MaximumLength(500).WithMessage("Answer B cannot exceed 500 characters.");

            RuleFor(x => x.AnswerC)
                .NotEmpty().WithMessage("Answer C is required.")
                .MaximumLength(500).WithMessage("Answer C cannot exceed 500 characters.");
        }
    }
}