using System.IO;
using System.Linq;
using Xunit;

namespace eventorientation.tests.bowling_kata_demo
{
    public class demo
    {
        private const string PATH = "bowlingkata.db";

        public demo() {
            if(Directory.Exists(PATH)) Directory.Delete(PATH, true);
        }
        
        [Fact]
        public void Acceptance_scenario() {
            var sut = new BowlingGame(new FilesInFolderEventstore(PATH));
            var game = new[] {1, 4, 4, 5, 6, 4, 5, 5, 10, 0, 1, 7, 3, 6, 4, 10, 2, 8, 6};
            foreach(var pins in game)
                sut.Roll(pins);
            Assert.Equal(133, sut.Score);
        }
        

        void Roll_many(BowlingGame bg, int n, int pins) =>
            Enumerable.Range(0, n).ToList().ForEach(_ => bg.Roll(pins));
    }
}