using QuizBattle.Domain.Features.Games;

namespace QuizBattle.Application.Features.Games.RedisModels
{

    public static class GameRoomMapper
    {
        public static GameRoomDto ToDto(GameRoom room)
        {
            return new GameRoomDto
            {
                Id = room.Id.Value.ToString(),
                GameType = (int)room.GameType,
                Status = (int)room.Status,
                LanguageCode = room.LanguageCode,
                TotalRounds = room.TotalRounds,
                CurrentRound = room.CurrentRound,
                CreatedAt = new DateTimeOffset(room.CreatedAt).ToUnixTimeMilliseconds(),
                StartedAt = room.StartedAt.HasValue
                    ? new DateTimeOffset(room.StartedAt.Value).ToUnixTimeMilliseconds()
                    : null,
                RoundStartedAt = room.RoundStartedAt.HasValue
                    ? new DateTimeOffset(room.RoundStartedAt.Value).ToUnixTimeMilliseconds()
                    : null,
                RoundEndsAt = room.RoundEndsAt.HasValue
                    ? new DateTimeOffset(room.RoundEndsAt.Value).ToUnixTimeMilliseconds()
                    : null,
                HostPlayerId = room.HostPlayerId?.Value,
                Players = room.Players.Select(ToDto).ToList(),
                Questions = room.Questions.Select(ToDto).ToList()
            };
        }

        public static GamePlayerDto ToDto(GamePlayer player)
        {
            return new GamePlayerDto
            {
                UserId = player.Id.Value,
                DisplayName = player.DisplayName,
                PhotoUrl = player.PhotoUrl,
                ColorHex = player.Color.HexCode,
                ColorName = player.Color.Name,
                TotalScore = player.TotalScore,
                CurrentRoundScore = player.CurrentRoundScore,
                IsReady = player.IsReady,
                IsConnected = player.IsConnected,
                CurrentAnswer = player.CurrentAnswer is not null ? ToDto(player.CurrentAnswer) : null,
                JoinedAt = new DateTimeOffset(player.JoinedAt).ToUnixTimeMilliseconds()
            };
        }

        public static PlayerAnswerDto ToDto(PlayerAnswer answer)
        {
            return new PlayerAnswerDto
            {
                Answer = answer.Answer,
                ResponseTimeMs = (long)answer.ResponseTime.TotalMilliseconds,
                AnsweredAt = new DateTimeOffset(answer.AnsweredAt).ToUnixTimeMilliseconds()
            };
        }

        public static GameQuestionDto ToDto(GameQuestion question)
        {
            return new GameQuestionDto
            {
                QuestionId = question.QuestionId,
                RoundNumber = question.RoundNumber,
                Text = question.Text,
                OptionA = question.OptionA,
                OptionB = question.OptionB,
                OptionC = question.OptionC,
                CorrectOption = question.CorrectOption
            };
        }
    }
}
