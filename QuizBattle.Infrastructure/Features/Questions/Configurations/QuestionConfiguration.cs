using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Features.Questions;

namespace QuizBattle.Infrastructure.Features.Questions.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");

        builder.HasKey(question => question.Id);

        builder.Property(question => question.Id)
            .HasConversion(questionId => questionId.Value, value => new QuestionId(value))
            .HasColumnName("question_id")
            .ValueGeneratedOnAdd();

        builder.Property(question => question.Language)
            .HasConversion(language => language.Code, code => new Language(code))
            .IsRequired()
            .HasMaxLength(2)
            .HasColumnName("language_code");

        builder.Property(question => question.Text)
            .IsRequired()
            .HasColumnName("question_text");

        builder.Property(question => question.AnswerA)
            .IsRequired()
            .HasColumnName("answer_a");

        builder.Property(question => question.AnswerB)
            .IsRequired()
            .HasColumnName("answer_b");

        builder.Property(question => question.AnswerC)
            .IsRequired()
            .HasColumnName("answer_c");
    }
}