using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Queries
{
    public sealed record GetGameRoomQuery(Guid RoomId) : IQuery<GameRoomDto>;

    internal sealed class GetGameRoomQueryHandler : IQueryHandler<GetGameRoomQuery, GameRoomDto>
    {
        private readonly IGameRoomRepository _repository;

        public GetGameRoomQueryHandler(IGameRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<GameRoomDto>> Handle(GetGameRoomQuery query, CancellationToken cancellationToken)
        {
            var room = await _repository.GetByIdAsync(GameRoomId.Create(query.RoomId), cancellationToken);

            return room is null
                ? Result.Failure<GameRoomDto>(Error.GameNotFound)
                : Result.Success(room);
        }
    }
}
