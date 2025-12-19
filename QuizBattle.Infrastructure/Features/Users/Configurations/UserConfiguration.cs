using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Infrastructure.Features.Users.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value))
            .HasColumnName("user_id")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.GoogleId)
            .HasColumnName("google_id")
            .HasMaxLength(100);

        builder.Property(u => u.AppleId)
            .HasColumnName("apple_id")
            .HasMaxLength(100);

        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(50);

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(50);

        builder.Property(u => u.Photo)
            .HasColumnName("photo_url")
            .HasMaxLength(500);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(100);

        builder.Property(u => u.Coins)
            .HasColumnName("coins")
            .HasDefaultValue(0);

        builder.Property(u => u.Tokens)
            .HasColumnName("tokens")
            .HasDefaultValue(50);

        builder.Property(u => u.GamesWon)
            .HasColumnName("games_won")
            .HasDefaultValue(0);

        builder.Property(u => u.GamesLost)
            .HasColumnName("games_lost")
            .HasDefaultValue(0);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        // Device tokens as owned entity
        builder.OwnsMany(u => u.DeviceTokens, dtBuilder =>
        {
            dtBuilder.ToTable("user_device_tokens");

            dtBuilder.WithOwner().HasForeignKey("user_id");

            dtBuilder.Property(dt => dt.Token)
                .HasColumnName("token")
                .HasMaxLength(500)
                .IsRequired();

            dtBuilder.Property(dt => dt.Platform)
                .HasColumnName("platform")
                .IsRequired();

            dtBuilder.Property(dt => dt.CreatedAt)
                .HasColumnName("created_at");

            dtBuilder.HasKey("user_id", "Token");
        });

        // Indexes
        builder.HasIndex(u => u.GoogleId).IsUnique().HasFilter("google_id IS NOT NULL");
        builder.HasIndex(u => u.AppleId).IsUnique().HasFilter("apple_id IS NOT NULL");
        builder.HasIndex(u => u.Email);
    }
}