using System;
using eventorientation;

namespace calculator_demo.data
{
    public class ExpandNumber : Command
    {
        public char Digit { get; }

        public ExpandNumber(char digit) {
            if ("0123456789".IndexOf(digit) < 0) throw new InvalidOperationException($"Invalid digit '{digit}'!");
            Digit = digit;
        }
    }

    
    public class AppendOperator : Command
    {
        public enum Operators {
            Addition,
            Subtraction,
            Multiplication,
            Division,
            Equals
        }

        public Operators Op;
    }

    
    public class Number : Query
    {
        public class Value : QueryResult {
            public int Number;
        }
    }
    
    
    public class Result : Query
    {
        public class Value : QueryResult {
            public int Number;
        }
    }
}