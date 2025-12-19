using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Features.Auth;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Infrastructure.Features.Auth.Services
{

    internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasConversion(
                    id => id.Value,
                    value => RefreshTokenId.Create(value))
                .HasColumnName("id");

            builder.Property(t => t.UserId)
                .HasConversion(
                    id => id.Value,
                    value => UserId.Create(value))
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(t => t.TokenHash)
                .HasColumnName("token_hash")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(t => t.DeviceInfo)
                .HasColumnName("device_info")
                .HasMaxLength(500);

            builder.Property(t => t.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(50);

            builder.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(t => t.ExpiresAt)
                .HasColumnName("expires_at")
                .IsRequired();

            builder.Property(t => t.RevokedAt)
                .HasColumnName("revoked_at");

            builder.Property(t => t.ReplacedByTokenId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? RefreshTokenId.Create(value.Value) : null)
                .HasColumnName("replaced_by_token_id");

            builder.Property(t => t.RevokedReason)
                .HasColumnName("revoked_reason")
                .HasMaxLength(256);

            // Indexes
            builder.HasIndex(t => t.TokenHash).IsUnique();
            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t => t.ExpiresAt);

            // Foreign key to User (no navigation property needed)
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
