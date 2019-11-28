namespace eventorientation.tests.message_pump_demo.data
{
    public class Increment : Command
    {
        public string CounterId;
    }

    public class Decrement : Command
    {
        public string CounterId;
    }

    public class CounterValue : Query
    {
        public string CounterId;


        public class Result : QueryResult
        {
            public string CounterId;
            public int Value;
        }
    }
}