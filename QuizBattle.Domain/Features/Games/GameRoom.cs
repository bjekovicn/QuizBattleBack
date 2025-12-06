using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Games
{

    public sealed class GameRoom : Entity<GameRoomId>
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 5;
        public const int DefaultRounds = 10;
        public const int RoundDurationSeconds = GameTimingConfig.RoundDurationSeconds;
        public const int MaxPointsPerRound = 1000;
        public const int PointDecrement = 150;  // Each subsequent correct answer gets less points

        public GameType GameType { get; private set; }
        public GameStatus Status { get; private set; }
        public string LanguageCode { get; private set; }
        public int TotalRounds { get; private set; }
        public int CurrentRound { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? RoundStartedAt { get; private set; }
        public DateTime? RoundEndsAt { get; private set; }
        public UserId? HostPlayerId { get; private set; }

        private readonly List<GamePlayer> _players = new();
        public IReadOnlyList<GamePlayer> Players => _players.AsReadOnly();

        private readonly List<GameQuestion> _questions = new();
        public IReadOnlyList<GameQuestion> Questions => _questions.AsReadOnly();

        private GameRoom() : base(GameRoomId.Empty)
        {
            LanguageCode = "sr";
            Status = GameStatus.WaitingForPlayers;
        }

        public GameRoom(
            GameRoomId id,
            GameType gameType,
            string languageCode,
            int totalRounds = DefaultRounds) : base(id)
        {
            GameType = gameType;
            LanguageCode = languageCode ?? throw new ArgumentNullException(nameof(languageCode));
            TotalRounds = totalRounds > 0 ? totalRounds : DefaultRounds;
            Status = GameStatus.WaitingForPlayers;
            CurrentRound = 0;
            CreatedAt = DateTime.UtcNow;
        }

        #region Player Management

        public Result<GamePlayer> AddPlayer(UserId userId, string displayName, string? photoUrl)
        {
            if (Status != GameStatus.WaitingForPlayers)
            {
                return Result.Failure<GamePlayer>(Error.GameAlreadyStarted);
            }

            if (_players.Count >= MaxPlayers)
            {
                return Result.Failure<GamePlayer>(Error.GameFull);
            }

            if (_players.Any(p => p.Id.Value == userId.Value))
            {
                return Result.Failure<GamePlayer>(Error.PlayerAlreadyInGame);
            }

            var color = PlayerColor.GetByIndex(_players.Count);
            var player = new GamePlayer(userId, displayName, photoUrl, color);
            _players.Add(player);

            // First player becomes host
            if (_players.Count == 1)
            {
                HostPlayerId = userId;
            }

            return Result.Success(player);
        }

        public Result RemovePlayer(UserId userId)
        {
            var player = _players.FirstOrDefault(p => p.Id.Value == userId.Value);
            if (player is null)
            {
                return Result.Failure(Error.PlayerNotInGame);
            }

            _players.Remove(player);

            // If host left, assign new host
            if (HostPlayerId?.Value == userId.Value && _players.Any())
            {
                HostPlayerId = _players[0].Id;
            }

            // Cancel game if not enough players
            if (_players.Count < MinPlayers && Status == GameStatus.RoundInProgress)
            {
                Status = GameStatus.Cancelled;
            }

            return Result.Success();
        }

        public Result SetPlayerReady(UserId userId, bool isReady)
        {
            var player = _players.FirstOrDefault(p => p.Id.Value == userId.Value);
            if (player is null)
            {
                return Result.Failure(Error.PlayerNotInGame);
            }

            player.SetReady(isReady);
            return Result.Success();
        }

        public Result SetPlayerConnected(UserId userId, bool isConnected)
        {
            var player = _players.FirstOrDefault(p => p.Id.Value == userId.Value);
            if (player is null)
            {
                return Result.Failure(Error.PlayerNotInGame);
            }

            player.SetConnected(isConnected);
            return Result.Success();
        }

        public bool AllPlayersReady => _players.Count >= MinPlayers && _players.All(p => p.IsReady);

        public int ConnectedPlayersCount => _players.Count(p => p.IsConnected);

        #endregion

        #region Game Flow

        public Result SetQuestions(IEnumerable<GameQuestion> questions)
        {
            var questionList = questions.ToList();

            if (questionList.Count < TotalRounds)
            {
                return Result.Failure(Error.NotEnoughQuestions);
            }

            _questions.Clear();
            _questions.AddRange(questionList.Take(TotalRounds));

            return Result.Success();
        }

        public Result StartGame()
        {
            if (Status != GameStatus.WaitingForPlayers)
            {
                return Result.Failure(Error.GameAlreadyStarted);
            }

            if (_players.Count < MinPlayers)
            {
                return Result.Failure(Error.NotEnoughPlayers);
            }

            if (_questions.Count < TotalRounds)
            {
                return Result.Failure(Error.NotEnoughQuestions);
            }

            Status = GameStatus.Starting;
            StartedAt = DateTime.UtcNow;

            return Result.Success();
        }

        public Result StartNextRound()
        {
            if (Status != GameStatus.Starting && Status != GameStatus.RoundEnded)
            {
                return Result.Failure(new Error("Game.InvalidState", "Cannot start round in current state."));
            }

            if (CurrentRound >= TotalRounds)
            {
                Status = GameStatus.GameEnded;
                return Result.Failure(new Error("Game.Finished", "All rounds completed."));
            }

            CurrentRound++;
            Status = GameStatus.RoundInProgress;
            RoundStartedAt = DateTime.UtcNow;
            RoundEndsAt = RoundStartedAt.Value.AddSeconds(GameTimingConfig.RoundDurationSeconds); // ← Koristi config

            // Prepare players for new round
            foreach (var player in _players)
            {
                player.PrepareForNextRound();
            }

            return Result.Success();
        }

        public GameQuestion? GetCurrentQuestion()
        {
            if (CurrentRound <= 0 || CurrentRound > _questions.Count)
            {
                return null;
            }

            return _questions[CurrentRound - 1];
        }

        #endregion

        #region Answer Handling

        public Result SubmitAnswer(UserId userId, string answer)
        {
            if (Status != GameStatus.RoundInProgress)
            {
                return Result.Failure(Error.RoundNotActive);
            }

            var player = _players.FirstOrDefault(p => p.Id.Value == userId.Value);
            if (player is null)
            {
                return Result.Failure(Error.PlayerNotInGame);
            }

            if (player.HasAnswered)
            {
                return Result.Failure(Error.AlreadyAnswered);
            }

            if (RoundStartedAt is null)
            {
                return Result.Failure(Error.RoundNotActive);
            }

            var responseTime = DateTime.UtcNow - RoundStartedAt.Value;

            // Don't accept answers after round ends
            if (responseTime.TotalSeconds > GameTimingConfig.RoundDurationSeconds)
            {
                return Result.Failure(new Error("Game.RoundExpired", "Round time has expired."));
            }

            player.SubmitAnswer(answer, responseTime);
            return Result.Success();
        }

        public bool AllPlayersAnswered => _players.All(p => p.HasAnswered || !p.IsConnected);

        #endregion

        #region Scoring

        public Result<RoundResult> EndRound()
        {
            if (Status != GameStatus.RoundInProgress)
            {
                return Result.Failure<RoundResult>(Error.RoundNotActive);
            }

            var currentQuestion = GetCurrentQuestion();
            if (currentQuestion is null)
            {
                return Result.Failure<RoundResult>(new Error("Game.NoQuestion", "No question for current round."));
            }

            // Get players who answered correctly, sorted by response time
            var correctAnswers = _players
                .Where(p => p.CurrentAnswer is not null &&
                           currentQuestion.IsCorrectAnswer(p.CurrentAnswer.Answer))
                .OrderBy(p => p.CurrentAnswer!.ResponseTime)
                .ToList();

            // Award points - first correct gets max, each subsequent gets less
            var playerResults = new List<PlayerRoundResult>();
            int points = MaxPointsPerRound;

            foreach (var player in correctAnswers)
            {
                player.AwardPoints(points);
                playerResults.Add(new PlayerRoundResult(
                    player.Id.Value,
                    player.DisplayName,
                    player.CurrentAnswer!.Answer,
                    player.CurrentAnswer.ResponseTime,
                    points,
                    true));

                points = Math.Max(100, points - PointDecrement);  // Minimum 100 points
            }

            // Add players who answered incorrectly or didn't answer
            foreach (var player in _players.Where(p => !correctAnswers.Contains(p)))
            {
                player.AwardPoints(0);
                playerResults.Add(new PlayerRoundResult(
                    player.Id.Value,
                    player.DisplayName,
                    player.CurrentAnswer?.Answer,
                    player.CurrentAnswer?.ResponseTime,
                    0,
                    false));
            }

            Status = GameStatus.RoundEnded;

            var roundResult = new RoundResult(
                CurrentRound,
                currentQuestion.QuestionId,
                currentQuestion.CorrectOption,
                currentQuestion.GetCorrectAnswerText(),
                playerResults,
                _players.Select(p => new PlayerScore(p.Id.Value, p.DisplayName, p.TotalScore)).ToList());

            return Result.Success(roundResult);
        }

        public Result<GameResult> EndGame()
        {
            if (Status != GameStatus.RoundEnded && Status != GameStatus.Cancelled)
            {
                return Result.Failure<GameResult>(new Error("Game.InvalidState", "Cannot end game in current state."));
            }

            Status = GameStatus.GameEnded;

            var finalStandings = _players
                .OrderByDescending(p => p.TotalScore)
                .Select((p, index) => new FinalStanding(
                    index + 1,
                    p.Id.Value,
                    p.DisplayName,
                    p.PhotoUrl,
                    p.TotalScore,
                    p.Color.HexCode))
                .ToList();

            var winnerId = finalStandings.FirstOrDefault()?.UserId;

            return Result.Success(new GameResult(
                Id.Value,
                GameType,
                TotalRounds,
                winnerId,
                finalStandings,
                StartedAt,
                DateTime.UtcNow));
        }

        #endregion

        #region Time Helpers

        public TimeSpan? TimeRemainingInRound
        {
            get
            {
                if (RoundEndsAt is null || Status != GameStatus.RoundInProgress)
                {
                    return null;
                }

                var remaining = RoundEndsAt.Value - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        public bool IsRoundExpired =>
            Status == GameStatus.RoundInProgress &&
            RoundEndsAt.HasValue &&
            DateTime.UtcNow >= RoundEndsAt.Value;

        #endregion
    }
}
