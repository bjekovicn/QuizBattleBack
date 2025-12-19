namespace QuizBattle.Application.Features.Games.Services
{
    public interface IGameTimerService
    {
        Task ScheduleFirstRoundAsync(Guid roomId, CancellationToken ct = default);
        Task ScheduleRoundEndAsync(Guid roomId, TimeSpan delay, CancellationToken ct = default);
        Task CancelRoundTimerAsync(Guid roomId, CancellationToken ct = default);
        Task ForceEndRoundAsync(Guid roomId, CancellationToken ct = default);
    }
}