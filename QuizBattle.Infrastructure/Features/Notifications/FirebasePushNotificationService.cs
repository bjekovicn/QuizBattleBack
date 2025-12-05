using FirebaseAdmin.Messaging;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Notifications;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Infrastructure.Features.Notifications
{

    internal sealed class FirebasePushNotificationService : IPushNotificationService
    {
        private readonly FirebaseMessaging _messaging;
        private readonly IUserQueryRepository _userRepository;
        private readonly ILogger<FirebasePushNotificationService> _logger;

        public FirebasePushNotificationService(
            IConfiguration configuration,
            IUserQueryRepository userRepository,
            ILogger<FirebasePushNotificationService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;

            // Initialize Firebase if not already initialized
            if (FirebaseApp.DefaultInstance is null)
            {
                var credentialPath = configuration["Firebase:CredentialPath"];

                if (!string.IsNullOrEmpty(credentialPath) && File.Exists(credentialPath))
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(credentialPath)
                    });
                }
                else
                {
                    // Use default credentials (for cloud environments)
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.GetApplicationDefault()
                    });
                }
            }

            _messaging = FirebaseMessaging.DefaultInstance;
        }

        public async Task SendToUserAsync(int userId, PushNotification notification, CancellationToken ct = default)
        {
            await SendToUsersAsync(new[] { userId }, notification, ct);
        }

        public async Task SendToUsersAsync(IEnumerable<int> userIds, PushNotification notification, CancellationToken ct = default)
        {
            var tokens = new List<(string Token, DevicePlatform Platform)>();

            foreach (var userId in userIds)
            {
                var user = await _userRepository.GetByIdWithTokensAsync(new UserId(userId), ct);
                if (user?.DeviceTokens is not null)
                {
                    tokens.AddRange(user.DeviceTokens.Select(dt => (dt.Token, dt.Platform)));
                }
            }

            if (tokens.Count > 0)
            {
                await SendToTokensAsync(tokens, notification, ct);
            }
        }

        public async Task SendToTokenAsync(
            string token,
            DevicePlatform platform,
            PushNotification notification,
            CancellationToken ct = default)
        {
            await SendToTokensAsync(new[] { (token, platform) }, notification, ct);
        }

        public async Task SendToTokensAsync(
            IEnumerable<(string Token, DevicePlatform Platform)> tokens,
            PushNotification notification,
            CancellationToken ct = default)
        {
            var tokenList = tokens.ToList();
            if (tokenList.Count == 0) return;

            try
            {
                var messages = tokenList.Select(t => CreateMessage(t.Token, t.Platform, notification)).ToList();

                if (messages.Count == 1)
                {
                    await _messaging.SendAsync(messages[0], ct);
                }
                else
                {
                    // Batch send (max 500 messages per batch)
                    foreach (var batch in messages.Chunk(500))
                    {
                        var response = await _messaging.SendEachAsync(batch, ct);

                        if (response.FailureCount > 0)
                        {
                            _logger.LogWarning(
                                "Failed to send {FailureCount} of {TotalCount} push notifications",
                                response.FailureCount,
                                batch.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notifications");
            }
        }

        private static Message CreateMessage(string token, DevicePlatform platform, PushNotification notification)
        {
            var message = new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = notification.Title,
                    Body = notification.Body,
                    ImageUrl = notification.ImageUrl
                },
                Data = notification.Data
            };

            // Platform-specific configuration
            switch (platform)
            {
                case DevicePlatform.Android:
                    message.Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                        }
                    };
                    break;

                case DevicePlatform.iOS:
                    message.Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = "default",
                            Badge = 1
                        }
                    };
                    break;

                case DevicePlatform.Web:
                    message.Webpush = new WebpushConfig
                    {
                        Notification = new WebpushNotification
                        {
                            Icon = notification.ImageUrl
                        }
                    };
                    break;
            }

            return message;
        }
    }
}
