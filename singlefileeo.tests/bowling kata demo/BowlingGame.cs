using System.Collections.Generic;
using System.Linq;

namespace eventorientation.tests.bowling_kata_demo
{
    class BowlingGame
    {
        private readonly IEventstore _es;

        public BowlingGame(IEventstore es) { _es = es; }


        public void Roll(string gameId, int pins) {
            var frame = Determine_frame_state();            
            Check_bonus_applicability(frame);
            
            
            (int total, int numberOfRolls) Determine_frame_state() {
                var total = pins;
                var numberOfRolls = 1;
                if (_es.ReplayContext(gameId).events.LastOrDefault() is Rolled previous) {
                    total += previous.Pins;
                    numberOfRolls++;
                }
                _es.Record(new Rolled {ContextId = gameId, Pins = pins});
                return (total, numberOfRolls);
            }

            void Check_bonus_applicability((int total, int numberOfRolls) frame)
            {
                if (pins == 10) {
                    _es.Record(new FrameCompleted {ContextId = gameId, Total = frame.total});
                    _es.Record(new StrikeBonusEarned{ContextId = gameId});
                }
                else {
                    if (frame.numberOfRolls == 2)
                        _es.Record(new FrameCompleted {ContextId = gameId, Total = frame.total});
                    if (frame.total == 10)
                        _es.Record(new SpareBonusEarned{ContextId = gameId});
                }
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
                        score += Collect_bonus_rolls(1).Sum();
                        break;
                    case StrikeBonusEarned _:
                        score += Collect_bonus_rolls(2).Sum();
                        break;
                }
                
                
                IEnumerable<int> Collect_bonus_rolls(int count) {
                    for (var j = i+1; j < events.Length; j++) {
                        if (count <= 0) break;
                        if (events[j] is Rolled bonusRoll) {
                            yield return bonusRoll.Pins;
                            count--;
                        }
                    }
                }
            }
            return score;
        }
    }
}