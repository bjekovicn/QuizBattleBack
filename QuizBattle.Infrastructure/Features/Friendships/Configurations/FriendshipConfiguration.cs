using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using QuizBattle.Domain.Features.Friendships;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Infrastructure.Features.Friendships.Configurations
{

    internal sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
    {
        public void Configure(EntityTypeBuilder<Friendship> builder)
        {
            builder.ToTable("friendships");

            builder.HasKey(f => new { f.SenderId, f.ReceiverId });

            builder.Property(f => f.SenderId)
                .HasConversion(
                    id => id.Value,
                    value => UserId.Create(value))
                .HasColumnName("sender_id");

            builder.Property(f => f.ReceiverId)
                .HasConversion(
                    id => id.Value,
                    value => UserId.Create(value))
                .HasColumnName("receiver_id");

            builder.Property(f => f.Status)
                .HasColumnName("status")
                .HasConversion<int>();

            builder.Property(f => f.CreatedAt)
                .HasColumnName("created_at");

            builder.Property(f => f.AcceptedAt)
                .HasColumnName("accepted_at");

            builder.HasIndex(f => f.SenderId);
            builder.HasIndex(f => f.ReceiverId);
            builder.HasIndex(f => f.Status);
        }
    }
}
