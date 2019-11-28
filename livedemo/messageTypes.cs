using eventorientation;

namespace eventorientation
{
    class Increment : Command
    {
        public string Name;
    }

    class Decrement : Command
    {
        public string Name;
    }

    class Value : Query
    {
        public class Result : QueryResult
        {
            public int Value;
        }

        public string Name;
    }
}