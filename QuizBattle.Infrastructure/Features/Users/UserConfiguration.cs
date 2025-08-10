using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Infrastructure.Features.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasConversion(userId => userId.Value, value => new UserId(value))
            .HasColumnName("user_id");

        builder.Property(user => user.GoogleId)
            .IsRequired()
            .HasColumnName("google_id");

        builder.Property(user => user.FirstName)
            .IsRequired(false)
            .HasMaxLength(50)
            .HasColumnName("first_name");

        builder.Property(user => user.LastName)
            .IsRequired(false)
            .HasMaxLength(50)
            .HasColumnName("last_name");

        builder.Property(user => user.Photo)
            .IsRequired(false)
            .HasColumnName("photo_url");

        builder.Property(user => user.Coins)
            .HasDefaultValue(0)
            .HasColumnName("coins");

        builder.Property(user => user.Tokens)
            .HasDefaultValue(50)
            .HasColumnName("tokens");

        builder.Property(user => user.GamesWon)
            .HasDefaultValue(0)
            .HasColumnName("games_won");

        builder.Property(user => user.GamesLost)
            .HasDefaultValue(0)
            .HasColumnName("games_lost");
    }
}