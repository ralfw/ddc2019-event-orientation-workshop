using System.Linq;
using tictactoe_demo.data;

namespace tictactoe_demo.domain
{
    static class Referee
    {
        public enum Statuses {
            CurrentPlayerX,
            CurrentPlayerO,
            XWon,
            OWon,
            Tied
        }


        public static Statuses DetermineGameStatus((Players Player, (int Column, int Row) Coordinate)[] moves) {
            if (moves.Length == 0) 
                return Map_player(InitialPlayer);

            if (Game_won(moves, out var winner))
                return winner == Players.PlayerX ? Statuses.XWon : Statuses.OWon;
            if (Game_tied(moves))
                return Statuses.Tied;

            return Next_player(moves);

            
            Statuses Map_player(Players player) => player == Players.PlayerX ? Statuses.CurrentPlayerX : Statuses.CurrentPlayerO;
        }

        
        static bool Game_won((Players Player, (int Column, int Row) Coordinate)[] moves, out Players winner) {
            var candidateWinner = winner = moves.Last().Player;
            var currentPlayerCells = moves.Where(x => x.Player == candidateWinner).Select(x => x.Coordinate).ToArray();
                
            // 3 in a row
            var cellsByRow = currentPlayerCells.GroupBy(x => x.Row);
            if (cellsByRow.Any(x => x.Count() == 3)) return true;
                
            // 3 in a col
            var cellsByCol = currentPlayerCells.GroupBy(x => x.Column);
            if (cellsByCol.Any(x => x.Count() == 3)) return true;
                
            // 3 in a diagonal
            var diagonales = new[] {
                new[] {(0, 0), (1, 1), (2, 2)},
                new[] {(0, 2), (1, 1), (2, 0)},
            };
            foreach(var diag in diagonales)
                if (diag.Intersect(currentPlayerCells).Count() == 3)
                    return true;
                
            return false;
        }
        
        
        static bool Game_tied((Players Player, (int Column, int Row))[] moves) => moves.Length == 9;

        
        static Statuses Next_player((Players Player, (int Column, int Row) Coordinate)[] moves) 
            => moves.Last().Player == Players.PlayerX ? Statuses.CurrentPlayerO : Statuses.CurrentPlayerX;
        
        
        public static Players InitialPlayer => Players.PlayerX;
    }
}