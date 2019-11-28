using System.Collections.Generic;
using System.Linq;
using eventorientation;
using tictactoe_demo.data;
using tictactoe_demo.domain;
using Version = eventorientation.Version;

namespace tictactoe_demo.pipelines
{
    public class PlaceTokenPipeline : ICommandPipeline<PlaceToken, PlaceTokenPipeline.Model>
    {
        public class Model : MessageModel {
            private readonly Context _ctx;

            internal Model(Context ctx) {
                _ctx = ctx;
            }

            public (Players Player, (int Column, int Row) Coordinate)[] Moves => _ctx.Moves.ToArray();
            public Players CurrentPlayer => _ctx.CurrentPlayer;
            public bool IsGameOver => _ctx.IsGameOver != GamePipeline.Model.GameOverStatuses.NotOverYet;
        }

        
        public (Model model, Version version) Load(IEventstore es, PlaceToken command)
            => (new Model(new Context(es, command.GameId)), null);

        
        public (CommandStatus commandStatus, Event[] events, Notification[] notifications) Execute(PlaceToken command, Model model) {
            if (model.IsGameOver)
                return (
                    new Failure("Game already over! No more moves possible."),
                    new Event[0],
                    new Notification[0]
                );
            if (model.Moves.Any(x => x.Coordinate == command.Coordinate))
                return (
                    new Failure("Invalid move! Cell already occupied."),
                    new Event[0],
                    new Notification[0]
                );
            
            var events = new List<Event> {
                new TokenPlaced {GameId = command.GameId, Coordinate = command.Coordinate},
                Referee.DetermineGameStatus(model.Moves.Concat(new[] {(model.CurrentPlayer, command.Coordinate)}).ToArray()).ToEvent(command.GameId)    
            };
            
            return (
                new Success(),
                events.ToArray(),
                new Notification[0]
            );
        }

        
        public void Update(IEventstore es, Event[] events, Version version)
        {}
    }


    static class PlaceTokenPipelineMappings
    {
        public static Event ToEvent(this Referee.Statuses status, string gameId)
            => status switch {
                Referee.Statuses.XWon => (Event)new GameWon{GameId = gameId, Winner = Players.PlayerX},
                Referee.Statuses.OWon => new GameWon{GameId = gameId, Winner = Players.PlayerO},
                Referee.Statuses.Tied => new GameTied{GameId = gameId},
                Referee.Statuses.CurrentPlayerX => new CurrentPlayerChanged{GameId = gameId, Current = Players.PlayerX},
                Referee.Statuses.CurrentPlayerO => new CurrentPlayerChanged{GameId = gameId, Current = Players.PlayerO},
            };
    }
}