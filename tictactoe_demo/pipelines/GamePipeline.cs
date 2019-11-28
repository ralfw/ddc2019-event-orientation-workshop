using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using eventorientation;
using tictactoe_demo.data;

namespace tictactoe_demo.pipelines
{
    public class GamePipeline : IQueryPipeline<GameView, GamePipeline.Model, GameView.Result>
    {
        public class Model : MessageModel {
            private readonly Context _ctx;

            public enum GameOverStatuses {
                NotOverYet,
                XWon,
                OWon,
                Tied
            }
            
            internal Model(Context ctx) { _ctx = ctx; }

            
            public string NamePlayerX => _ctx.NamePlayerX;
            public string NamePlayerO => _ctx.NamePlayerO;
            public (Players Player, (int Column, int Row) Coordinate)[] Moves => _ctx.Moves.ToArray();
            public Players CurrentPlayer => _ctx.CurrentPlayer;
            public GameOverStatuses GameOverStatus => _ctx.IsGameOver;
        }

        
        public (Model model, Version version) Load(IEventstore es, GameView query)
            => (new Model(new Context(es, query.GameId)), null);

        
        public GameView.Result Project(GameView query, Model model) {
            var result = new GameView.Result {
                GameId = query.GameId,
                NamePlayerX = model.NamePlayerX,
                NamePlayerO = model.NamePlayerO
            };

            result.Board = model.Moves.Aggregate(new GameView.Result.CellValues[3, 3],
                                                (board, move) => {
                                                    board[move.Coordinate.Row, move.Coordinate.Column] = move.Player == Players.PlayerX
                                                        ? GameView.Result.CellValues.X
                                                        : GameView.Result.CellValues.O;
                                                    return board;
                                                });

            result.Status = model.GameOverStatus switch {
                Model.GameOverStatuses.XWon => GameView.Result.Statuses.XWon,
                Model.GameOverStatuses.OWon => GameView.Result.Statuses.OWon,
                Model.GameOverStatuses.Tied => GameView.Result.Statuses.Tied,
                _ => model.CurrentPlayer switch {
                    Players.PlayerX => GameView.Result.Statuses.CurrentPlayerX,
                    Players.PlayerO => GameView.Result.Statuses.CurrentPlayerO
                }
            };
            
            return result;
        }

        
        public void Update(IEventstore es, Event[] events, Version version)
        {}
    }
}