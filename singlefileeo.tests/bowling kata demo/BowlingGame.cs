using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace eventorientation.tests.bowling_kata_demo
{
    class BowlingGame
    {
        private readonly IEventstore _es;

        public BowlingGame(IEventstore es) { _es = es; }


        class RollContextModel
        {
            public RollContextModel(IEventstore es, string gameId) {
                es.ReplayContext(gameId).events.ToList().ForEach(Apply);
            }

            private void Apply(ContextualEvent e) {
                switch (e) {
                    case Rolled re:
                        TotalPinsInCurrentFrame += re.Pins;
                        NumberOfRollsInCurrentFrame++;
                        break;
                    case FrameCompleted _:
                        TotalPinsInCurrentFrame = 0;
                        NumberOfRollsInCurrentFrame = 0;
                        break;
                    
                    case SpareBonusEarned _:
                        SpareIsMissingBonus = true;
                        break;
                    case SpareBonusAssigned _:
                        SpareIsMissingBonus = false;
                        break;
                    
                    case StrikeBonusEarned _: 
                        StrikesMissingBonus += 2;
                        break;
                    case StrikeBonusAssigned _:
                        StrikesMissingBonus -= 1;
                        break;
                }
            }

            public uint TotalPinsInCurrentFrame { get; set; }
            public uint NumberOfRollsInCurrentFrame { get; set; }
            public bool SpareIsMissingBonus { get; private set; }
            public uint StrikesMissingBonus { get; set; }
        }
        
        
        public void Roll(string gameId, uint pins) {
            var model = Record_roll();
            Assign_bonusses_to_previous_frames();
            Close_frame_if_needed();
            Check_for_bonus_earned();


            RollContextModel Record_roll() {
                _es.Record(new Rolled {ContextId = gameId, Pins = pins});
                return new RollContextModel(_es, gameId);
            }

            void Assign_bonusses_to_previous_frames() {
                if (model.SpareIsMissingBonus)
                    _es.Record(new SpareBonusAssigned{ContextId = gameId, Pins = pins});
                if (model.StrikesMissingBonus == 1)
                    _es.Record(new StrikeBonusAssigned{ContextId = gameId, Pins = pins});
                else while(model.StrikesMissingBonus-- > 1)
                    _es.Record(new StrikeBonusAssigned{ContextId = gameId, Pins = pins});
            }

            void Close_frame_if_needed() {
                if (pins == 10 || model.NumberOfRollsInCurrentFrame == 2)
                    _es.Record(new FrameCompleted {ContextId = gameId, Pins = model.TotalPinsInCurrentFrame});
            }

            void Check_for_bonus_earned() {
                if (pins == 10)
                    _es.Record(new StrikeBonusEarned{ContextId = gameId});
                else if (model.TotalPinsInCurrentFrame == 10)
                    _es.Record(new SpareBonusEarned{ContextId = gameId});
            }
        }

        
        public uint Score(string gameId)
            => (uint)_es.ReplayContext(gameId)
                            .events
                            .OfType<ScoreRelevantEvent>()
                            .Select(x => (int)x.Pins).Sum();
    }
}