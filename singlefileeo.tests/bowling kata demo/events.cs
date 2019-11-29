namespace eventorientation.tests.bowling_kata_demo
{
    public class Rolled : ContextualEvent {
        public int Pins;
    }

    public class FrameCompleted : ContextualEvent {
        public int Total;
    }
    
    public class SpareBonusEarned : ContextualEvent {}
    public class StrikeBonusEarned : ContextualEvent {}
}