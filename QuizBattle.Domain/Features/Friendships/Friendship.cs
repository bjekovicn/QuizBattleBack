
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Friendships
{

    public sealed class Friendship
    {
        public UserId SenderId { get; private set; } = null!;
        public UserId ReceiverId { get; private set; } = null!;
        public FriendshipStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? AcceptedAt { get; private set; }

        private Friendship() { }

        public static Friendship Create(UserId senderId, UserId receiverId)
        {
            return new Friendship
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = FriendshipStatus.Accepted,
                CreatedAt = DateTime.UtcNow,
                AcceptedAt = DateTime.UtcNow
            };
        }

        public void Accept()
        {
            if (Status != FriendshipStatus.Pending)
            {
                throw new InvalidOperationException("Only pending friendships can be accepted");
            }

            Status = FriendshipStatus.Accepted;
            AcceptedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            if (Status != FriendshipStatus.Pending)
            {
                throw new InvalidOperationException("Only pending friendships can be rejected");
            }

            Status = FriendshipStatus.Rejected;
        }
    }

    public enum FriendshipStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Blocked = 3
    }
}
