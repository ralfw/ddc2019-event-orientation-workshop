using eventorientation;

namespace calculator_demo.data
{
    public class NumberUpdated : Event {
        public int Number;
    }

    public class ResultCalculated : Event {
        public int Number;
    }
    
    public abstract class OpAppended : Event{}
    public class AdditionAppended : OpAppended{}
    public class MultiplicationAppended : OpAppended {}
    public class SubtractionAppended : OpAppended {}
    public class DivisionAppended : OpAppended {}
    public class EqualsAppended : OpAppended {}
}