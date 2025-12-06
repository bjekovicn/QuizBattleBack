
namespace QuizBattle.Application.Features.Games
{
    public enum GameHubEvent
    {
        // Connection Events (Client → Server)
        Connect,
        Disconnect,

        // Room Events (Bidirectional)
        RoomCreated,
        PlayerJoined,
        PlayerLeft,
        PlayerReadyChanged,
        PlayerDisconnected,
        PlayerReconnected,

        // Matchmaking Events (Server → Client)
        MatchmakingUpdate,  
        MatchFound,       

        // Game Flow Events (Server → Client)
        GameStarting,     
        RoundStarted,      
        PlayerAnswered,    
        RoundEnded,       
        GameEnded,        

        // Error Events
        Error
    }
}
