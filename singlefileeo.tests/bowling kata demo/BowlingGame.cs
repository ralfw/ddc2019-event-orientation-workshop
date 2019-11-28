using System.Collections.Generic;
using System.Linq;

namespace eventorientation.tests.bowling_kata_demo
{
    class BowlingGame
    {
        private readonly IEventstore _es;

        public BowlingGame(IEventstore es) { _es = es; }


        public void Roll(int pins) {
            var frameTotal = 0;
            var rollsInFrame = 0;
            if (_es.Replay().LastOrDefault() is Rolled previous) {
                frameTotal = previous.Pins;
                rollsInFrame++;
            }

            _es.Record(new Rolled {Pins = pins});
            frameTotal += pins;
            rollsInFrame++;

            if (pins == 10) {
                _es.Record(new FrameFinished {Total = frameTotal});
                _es.Record(new StrikeAchieved());
            }
            else {
                if (rollsInFrame == 2)
                    _es.Record(new FrameFinished {Total = frameTotal});
                if (frameTotal == 10)
                    _es.Record(new SpareAchieved());
            } 
        }

        
        public int Score {
            get {
                var score = 0;
                var events = _es.Replay();
                for (var i = 0; i < events.Length; i++) {
                    switch (events[i]) {
                        case FrameFinished ff:
                            score += ff.Total;
                            break;
                        case SpareAchieved _:
                            score += AdditionalRolls(1).Sum();
                            break;
                        case StrikeAchieved _:
                            score += AdditionalRolls(2).Sum();
                            break;
                    }
                    
                    
                    IEnumerable<int> AdditionalRolls(int count) {
                        for (var j = i+1; j < events.Length; j++) {
                            if (count <= 0) break;
                            if (events[j] is Rolled rolled) {
                                yield return rolled.Pins;
                                count--;
                            }
                        }
                    }
                }
                return score;
            }
        }
    }
}