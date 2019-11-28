using System;
using System.IO;
using eventorientation;
using tictactoe_demo.data;
using Xunit;

namespace tictactoe_demo
{
    public class ttt_usecases
    {
        private const string PATH = "ttt.test.db";
            
        [Fact]
        public void Play_game()
        {
            if (Directory.Exists(PATH)) Directory.Delete(PATH, true);
            var sut = new MessageHandling(new FilesInFolderEventstore(PATH));

            var id = (sut.Handle(new StartGame{NamePlayerX = "Bruce", NamePlayerO = "Janine"}) as Success<string>).Value;
            var result = sut.Handle(new GameView {GameId = id});
            Assert.Equal(id, result.GameId);
            Assert.Equal(GameView.Result.Statuses.CurrentPlayerX, result.Status);
            Assert.Equal("Bruce", result.NamePlayerX);
            Assert.Equal("Janine", result.NamePlayerO);
            Assert.Equal(new GameView.Result.CellValues[3,3], result.Board);
            
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (1, 1)});
            result = sut.Handle(new GameView {GameId = id});
            Assert.Equal(GameView.Result.Statuses.CurrentPlayerO, result.Status);
            
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (0, 0)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (0, 1)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (0, 2)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (2, 1)});

            result = sut.Handle(new GameView {GameId = id});
            Assert.Equal(GameView.Result.Statuses.XWon, result.Status);
            Assert.Equal(new[,] {
                {GameView.Result.CellValues.O, GameView.Result.CellValues.Empty, GameView.Result.CellValues.Empty},
                {GameView.Result.CellValues.X, GameView.Result.CellValues.X, GameView.Result.CellValues.X},
                {GameView.Result.CellValues.O, GameView.Result.CellValues.Empty, GameView.Result.CellValues.Empty},
            }, result.Board);
        }
        
        
        [Fact]
        public void Play_game_with_diagonale_win()
        {
            if (Directory.Exists(PATH)) Directory.Delete(PATH, true);
            var sut = new MessageHandling(new FilesInFolderEventstore(PATH));

            var id = (sut.Handle(new StartGame{NamePlayerX = "Bruce", NamePlayerO = "Janine"}) as Success<string>).Value;
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (1, 0)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (0, 0)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (1, 2)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (1, 1)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (2, 1)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (2, 2)});

            var result = sut.Handle(new GameView {GameId = id});
            Assert.Equal(GameView.Result.Statuses.OWon, result.Status);
        }
        
        
        [Fact]
        public void Play_game_with_tie_and_errors()
        {
            if (Directory.Exists(PATH)) Directory.Delete(PATH, true);
            var sut = new MessageHandling(new FilesInFolderEventstore(PATH));

            var id = (sut.Handle(new StartGame{NamePlayerX = "Bruce", NamePlayerO = "Janine"}) as Success<string>).Value;
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (0, 0)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (1, 0)});
            
            var error = sut.Handle(new PlaceToken {GameId=id, Coordinate = (1, 0)});
            Assert.IsType<Failure>(error);
            
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (2, 0)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (1, 1)});
            
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (0, 1)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (0, 2)});
            
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (2, 1)});
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (2, 2)});
            
            sut.Handle(new PlaceToken {GameId=id, Coordinate = (1, 2)});

            var result = sut.Handle(new GameView {GameId = id});
            Assert.Equal(GameView.Result.Statuses.Tied, result.Status);
            
            error = sut.Handle(new PlaceToken {GameId=id, Coordinate = (1, 2)});
            Assert.IsType<Failure>(error);
        }
    }
}