
namespace QuizBattle.Domain.Features.Games
{
    /// <summary>
    /// Centralized game timing configuration
    /// All time-related constants for game flow in one place
    /// </summary>
    public static class GameTimingConfig
    {
        /// <summary>
        /// Duration of each round in seconds (time players have to answer)
        /// </summary>
        public const int RoundDurationSeconds = 15;

        /// <summary>
        /// Delay before starting the first round after GameStarting event (seconds)
        /// </summary>
        public const int DelayBeforeFirstRoundSeconds = 4;

        /// <summary>
        /// Delay between RoundEnded and next RoundStarted (seconds)
        /// </summary>
        public const int DelayBetweenRoundsSeconds = 4;

        /// <summary>
        /// Delay before showing GameEnded results after final round (seconds)
        /// </summary>
        public const int DelayBeforeGameEndSeconds = 4;

        /// <summary>
        /// Maximum time to wait for all players to be ready (seconds)
        /// Set to 0 to disable timeout
        /// </summary>
        public const int ReadyTimeoutSeconds = 0; // Disabled for now

        /// <summary>
        /// SignalR connection keep-alive interval (seconds)
        /// </summary>
        public const int SignalRKeepAliveSeconds = 15;

        /// <summary>
        /// SignalR client timeout interval (seconds)
        /// </summary>
        public const int SignalRClientTimeoutSeconds = 30;

        /// <summary>
        /// SignalR handshake timeout (seconds)
        /// </summary>
        public const int SignalRHandshakeTimeoutSeconds = 15;
    }
}
