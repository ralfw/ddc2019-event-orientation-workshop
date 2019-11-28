using System.Collections.Generic;
using System.Linq;
using eventorientation;
using tictactoe_demo.data;

namespace tictactoe_demo.pipelines
{
    class Context
    {
        public Context(IEventstore es, string gameId) {
            Events =  es.Replay()
                        .OfType<IntraGameEvent>()
                        .Where(x => x.GameId == gameId)
                        .ToArray();
            
            var eStarted = Events[0] as GameStarted;
            NamePlayerX = eStarted.NamePlayerX;
            NamePlayerO = eStarted.NamePlayerO;
        }
        
        
        public IntraGameEvent[] Events { get; }

        
        public string NamePlayerX { get; }
        public string NamePlayerO { get; }
        
        
        public IEnumerable<(Players, (int, int))> Moves {
            get {
                Players currentPlayer = Players.PlayerX;
                foreach (var e in Events) {
                    switch (e) {
                        case CurrentPlayerChanged cpc:
                            currentPlayer = cpc.Current;
                            break;
                        case TokenPlaced tp:
                            yield return (currentPlayer, (tp.Coordinate.Column, tp.Coordinate.Row));
                            break;
                    }
                }
            }

        }

        
        public GamePipeline.Model.GameOverStatuses IsGameOver
            => Events.Last() switch {
                GameWon gw => gw.Winner == Players.PlayerX ? GamePipeline.Model.GameOverStatuses.XWon : GamePipeline.Model.GameOverStatuses.OWon,
                GameTied _ => GamePipeline.Model.GameOverStatuses.Tied,
                _ => GamePipeline.Model.GameOverStatuses.NotOverYet
            };

        
        public Players CurrentPlayer
            => ((CurrentPlayerChanged) Events.Last(x => x is CurrentPlayerChanged)).Current == Players.PlayerX
                ? Players.PlayerX
                : Players.PlayerO;
    }
}