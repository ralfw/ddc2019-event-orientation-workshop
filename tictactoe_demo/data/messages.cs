using System;
using eventorientation;

namespace tictactoe_demo.data
{
    public interface IMessageHandling {
        CommandStatus Handle(StartGame cmd);
        CommandStatus Handle(PlaceToken cmd);
        GameView.Result Handle(GameView qry);
    }


    public class StartGame : Command
    {
        public string NamePlayerX;
        public string NamePlayerO;
    }

    public class PlaceToken : Command
    {
        public string GameId;
        public (int Column, int Row) Coordinate;
    }

    public class GameView : Query
    {
        public string GameId;
        
        
        public class Result : QueryResult
        {
            public string GameId;

            public string NamePlayerX;
            public string NamePlayerO;
            
            public enum CellValues {
                Empty,
                X,
                O
            }
            public CellValues[,] Board;

            public enum Statuses {
                CurrentPlayerX,
                CurrentPlayerO,
                XWon,
                OWon,
                Tied,
                Canceled
            }

            public Statuses Status;
        }
    }
}