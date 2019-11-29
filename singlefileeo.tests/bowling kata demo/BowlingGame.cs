using System.Collections.Generic;
using System.Linq;

namespace eventorientation.tests.bowling_kata_demo
{
    class BowlingGame
    {
        private readonly IEventstore _es;

        public BowlingGame(IEventstore es) { _es = es; }


        public void Roll(string gameId, int pins) {
            var frameTotal = 0;
            var rollsInFrame = 0;
            if (_es.ReplayContext(gameId).events.LastOrDefault() is Rolled previous) {
                frameTotal = previous.Pins;
                rollsInFrame++;
            }

            _es.Record(new Rolled {ContextId = gameId, Pins = pins});
            frameTotal += pins;
            rollsInFrame++;

            if (pins == 10) {
                _es.Record(new FrameCompleted {ContextId = gameId, Total = frameTotal});
                _es.Record(new StrikeBonusEarned{ContextId = gameId});
            }
            else {
                if (rollsInFrame == 2)
                    _es.Record(new FrameCompleted {ContextId = gameId, Total = frameTotal});
                if (frameTotal == 10)
                    _es.Record(new SpareBonusEarned{ContextId = gameId});
            } 
        }

        
        public int Score(string gameId) {
            var score = 0;
            var events = _es.ReplayContext(gameId).events;
            for (var i = 0; i < events.Length; i++) {
                switch (events[i]) {
                    case FrameCompleted ff:
                        score += ff.Total;
                        break;
                    case SpareBonusEarned _:
                        score += AdditionalRolls(1).Sum();
                        break;
                    case StrikeBonusEarned _:
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