using Microsoft.Extensions.Caching.Distributed;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using System.Text.Json;

namespace QuizBattle.Infrastructure.Features.RealTime
{



    internal sealed class ConnectionManager : IConnectionManager
    {
        private readonly IDistributedCache _cache;
        private const string KeyPrefix = "connections:";
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(24);

        public ConnectionManager(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task AddConnectionAsync(int userId, string connectionId, CancellationToken ct = default)
        {
            var key = GetKey(userId);
            var connections = await GetConnectionsAsync(userId, ct);

            if (!connections.Contains(connectionId))
            {
                connections.Add(connectionId);
                await SetConnectionsAsync(userId, connections, ct);
            }
        }

        public async Task RemoveConnectionAsync(int userId, string connectionId, CancellationToken ct = default)
        {
            var key = GetKey(userId);
            var connections = await GetConnectionsAsync(userId, ct);

            connections.Remove(connectionId);

            if (connections.Any())
            {
                await SetConnectionsAsync(userId, connections, ct);
            }
            else
            {
                await _cache.RemoveAsync(key, ct);
            }
        }

        public async Task<List<string>> GetConnectionsAsync(int userId, CancellationToken ct = default)
        {
            var key = GetKey(userId);
            var json = await _cache.GetStringAsync(key, ct);

            if (string.IsNullOrEmpty(json))
            {
                return new List<string>();
            }

            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        public async Task<bool> IsUserConnectedAsync(int userId, CancellationToken ct = default)
        {
            var connections = await GetConnectionsAsync(userId, ct);
            return connections.Any();
        }

        private static string GetKey(int userId) => $"{KeyPrefix}{userId}";

        private async Task SetConnectionsAsync(int userId, List<string> connections, CancellationToken ct = default)
        {
            var key = GetKey(userId);
            var json = JsonSerializer.Serialize(connections);

            await _cache.SetStringAsync(
                key,
                json,
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = Expiration
                },
                ct);
        }
    }
}
