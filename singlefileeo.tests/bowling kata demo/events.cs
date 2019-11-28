namespace eventorientation.tests.bowling_kata_demo
{
    public class Rolled : Event {
        public int Pins;
    }

    public class FrameFinished : Event {
        public int Total;
    }
    
    public class SpareAchieved : Event {}
    public class StrikeAchieved : Event {}
}