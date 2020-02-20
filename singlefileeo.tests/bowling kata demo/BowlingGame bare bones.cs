
using Xunit;

namespace eventorientation.tests.bowling_kata_demo
{
    using System.Linq;
    using System.Collections.Generic;
    
    
    class BowlingGameBareBones
    {
        #region Event definitions
        abstract class Event {}
        
        class Rolled : Event {
            public uint Pins;
        }

        abstract class ScoreRelevantEvent : Event {
            public uint Pins;
        }
    
        class FrameCompleted : ScoreRelevantEvent {}
    
        class SpareBonusEarned : Event {}
        class SpareBonusAssigned : ScoreRelevantEvent {}

        class StrikeBonusEarned : Event {}
        class StrikeBonusAssigned : ScoreRelevantEvent {}
        #endregion


        // Poor man's event store
        private readonly List<Event> _events = new List<Event>();


        #region Command context model
        class RollContextModel
        {
            public RollContextModel(IEnumerable<Event> events) {
                events.ToList().ForEach(Apply);
            }

            private void Apply(Event e) {
                switch (e) {
                    case Rolled re:
                        TotalPinsInCurrentFrame += re.Pins;
                        NumberOfRollsInCurrentFrame++;
                        break;
                    case FrameCompleted _:
                        TotalPinsInCurrentFrame = 0;
                        NumberOfRollsInCurrentFrame = 0;
                        FrameNumber++;
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

            public uint FrameNumber { get; private set; }
            public uint TotalPinsInCurrentFrame { get; set; }
            public uint NumberOfRollsInCurrentFrame { get; set; }
            public bool SpareIsMissingBonus { get; private set; }
            public uint StrikesMissingBonus { get; set; }
        }
        #endregion

        #region Command handler
        public void Roll(uint pins) {
            var model = Record_roll();
            Assign_bonusses_to_previous_frames();
            Check_for_bonus_earned();
            Close_frame_if_needed();


            RollContextModel Record_roll() {
                _events.Add(new Rolled {Pins = pins});
                return new RollContextModel(_events);
            }

            void Assign_bonusses_to_previous_frames() {
                if (model.SpareIsMissingBonus)
                    _events.Add(new SpareBonusAssigned{Pins = pins});
                if (model.StrikesMissingBonus == 1)
                    _events.Add(new StrikeBonusAssigned{Pins = pins});
                else while(model.StrikesMissingBonus-- > 1)
                    _events.Add(new StrikeBonusAssigned{Pins = pins});
            }

            void Check_for_bonus_earned() {
                if (model.FrameNumber >= 10) return;
                if (pins == 10)
                    _events.Add(new StrikeBonusEarned());
                else if (model.TotalPinsInCurrentFrame == 10)
                    _events.Add(new SpareBonusEarned());
            }
            
            void Close_frame_if_needed() {
                if (model.FrameNumber >= 10) return;
                if (pins == 10 || model.NumberOfRollsInCurrentFrame == 2)
                    _events.Add(new FrameCompleted {Pins = model.TotalPinsInCurrentFrame});
            }

        }
        #endregion

        #region Query handler
        public uint Score()
            => (uint)_events.OfType<ScoreRelevantEvent>()
                            .Select(x => (int)x.Pins).Sum();
        #endregion
    }
    
    
    public class DemoBareBones
    {
        [Fact]
        public void Acceptance_scenario() {
            var sut = new BowlingGameBareBones();
            var game = new[] {1, 4, 4, 5, 6, 4, 5, 5, 10, 0, 1, 7, 3, 6, 4, 10, 2, 8, 6};
            
            foreach(var pins in game)
                sut.Roll((uint)pins);
            Assert.Equal(133, (int)sut.Score());
        }
        
        
        [Fact]
        public void All_strikes() {
            var sut = new BowlingGameBareBones();
            Roll_many(sut, 12, 10);
            Assert.Equal(300, (int)sut.Score());
        }
        
        
        [Fact]
        public void All_1() {
            var sut = new BowlingGameBareBones();
            Roll_many(sut, 20, 1);
            Assert.Equal(20, (int)sut.Score());
        }
        

        void Roll_many(BowlingGameBareBones bg, int n, uint pins) =>
            Enumerable.Range(0, n).ToList().ForEach(_ => bg.Roll(pins));
    }
}