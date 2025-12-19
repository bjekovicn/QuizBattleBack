namespace QuizBattle.Domain.Shared.Abstractions
{

    public sealed record Error(string Code, string Message)
    {
        // Existing errors...
        public static readonly Error None = new(string.Empty, string.Empty);
        public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");

        // User errors
        public static readonly Error UserNotFound = new("User.NotFound", "User was not found.");
        public static readonly Error UserAlreadyExists = new("User.AlreadyExists", "User already exists.");

        // Game errors
        public static readonly Error GameNotFound = new("Game.NotFound", "Game was not found.");
        public static readonly Error GameAlreadyStarted = new("Game.AlreadyStarted", "Game has already started.");
        public static readonly Error GameFull = new("Game.Full", "Game room is full.");
        public static readonly Error PlayerNotInGame = new("Game.PlayerNotInGame", "Player is not in this game.");
        public static readonly Error PlayerAlreadyInGame = new("Game.PlayerAlreadyInGame", "Player is already in this game.");
        public static readonly Error NotEnoughPlayers = new("Game.NotEnoughPlayers", "Not enough players to start.");
        public static readonly Error RoundNotActive = new("Game.RoundNotActive", "No active round.");
        public static readonly Error AlreadyAnswered = new("Game.AlreadyAnswered", "Player has already answered.");

        // Question errors
        public static readonly Error QuestionNotFound = new("Question.NotFound", "Question was not found.");
        public static readonly Error NotEnoughQuestions = new("Question.NotEnough", "Not enough questions available.");

        // Auth errors - NOVO
        public static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", "Invalid credentials provided.");
        public static readonly Error InvalidToken = new("Auth.InvalidToken", "Invalid or expired token.");
        public static readonly Error TokenExpired = new("Auth.TokenExpired", "Token has expired.");
        public static readonly Error Unauthorized = new("Auth.Unauthorized", "You are not authorized to perform this action.");
        public static readonly Error InvalidGoogleToken = new("Auth.InvalidGoogleToken", "Invalid Google ID token.");
        public static readonly Error InvalidAppleToken = new("Auth.InvalidAppleToken", "Invalid Apple ID token.");

        // Auth errors
        public static readonly Error RefreshTokenNotFound = new("Auth.RefreshTokenNotFound", "Refresh token not found.");
        public static readonly Error RefreshTokenExpired = new("Auth.RefreshTokenExpired", "Refresh token has expired.");
        public static readonly Error RefreshTokenRevoked = new("Auth.RefreshTokenRevoked", "Refresh token has been revoked.");
        public static readonly Error RefreshTokenReused = new("Auth.RefreshTokenReused", "Refresh token reuse detected. All sessions have been revoked for security.");

        // Friendship errors
        public static readonly Error FriendshipAlreadyExists = new("Friendship.AlreadyExists", "Friendship already exists.");
        public static readonly Error FriendshipNotFound = new("Friendship.NotFound", "Friendship not found.");
        public static readonly Error CannotAddYourself = new("Friendship.CannotAddYourself", "Cannot add yourself as friend.");

    }

}
