using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Application.Shared.Abstractions.Notifications
{
    public interface IPushNotificationService
    {
        Task SendToUserAsync(int userId, PushNotification notification, CancellationToken ct = default);
        Task SendToUsersAsync(IEnumerable<int> userIds, PushNotification notification, CancellationToken ct = default);
        Task SendToTokenAsync(string token, DevicePlatform platform, PushNotification notification, CancellationToken ct = default);
        Task SendToTokensAsync(IEnumerable<(string Token, DevicePlatform Platform)> tokens, PushNotification notification, CancellationToken ct = default);
    }

    public sealed record PushNotification(
        string Title,
        string Body,
        Dictionary<string, string>? Data = null,
        string? ImageUrl = null);
}
