using System.Buffers.Text;
using eventorientation;

namespace tictactoe_demo.data
{
    public enum Players {
        PlayerX,
        PlayerO
    }

    
    public abstract class IntraGameEvent : Event {
        public string GameId;
    }
    
    
    public class GameStarted : IntraGameEvent{
        public string NamePlayerX;
        public string NamePlayerO;
    }


    public class TokenPlaced : IntraGameEvent {
        public (int Column, int Row) Coordinate;
    }

    public class CurrentPlayerChanged : IntraGameEvent {
        public Players Current;
    }

    
    public class GameWon : IntraGameEvent {
        public Players Winner;
    }

    public class GameTied : IntraGameEvent
    {}
}