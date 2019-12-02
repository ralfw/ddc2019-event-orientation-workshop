namespace eventorientation.tests.bowling_kata_demo
{
    public class Rolled : ContextualEvent {
        public uint Pins;
    }

    public abstract class ScoreRelevantEvent : ContextualEvent {
        public uint Pins;
    }
    
    public class FrameCompleted : ScoreRelevantEvent {}
    
    public class SpareBonusEarned : ContextualEvent {}
    public class SpareBonusAssigned : ScoreRelevantEvent {}

    public class StrikeBonusEarned : ContextualEvent {}
    public class StrikeBonusAssigned : ScoreRelevantEvent {}
}