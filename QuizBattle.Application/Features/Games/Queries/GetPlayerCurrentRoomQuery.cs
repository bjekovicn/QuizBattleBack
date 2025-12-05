using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Queries
{
    public sealed record GetPlayerCurrentRoomQuery(int UserId) : IQuery<GameRoomDto?>;

    internal sealed class GetPlayerCurrentRoomQueryHandler : IQueryHandler<GetPlayerCurrentRoomQuery, GameRoomDto?>
    {
        private readonly IGameRoomRepository _repository;

        public GetPlayerCurrentRoomQueryHandler(IGameRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<GameRoomDto?>> Handle(
            GetPlayerCurrentRoomQuery query,
            CancellationToken cancellationToken)
        {
            var roomId = await _repository.GetRoomIdByPlayerAsync(query.UserId, cancellationToken);

            if (roomId is null)
                return Result.Success<GameRoomDto?>(null);

            var room = await _repository.GetByIdAsync(roomId, cancellationToken);
            return Result.Success(room);
        }
    }
}
