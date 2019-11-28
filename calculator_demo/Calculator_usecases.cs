using System;
using System.IO;
using calculator_demo.data;
using eventorientation;
using Xunit;

namespace calculator_demo
{
    public class Calculator_usecases
    {
        const string PATH = "calc.test";
        
        [Fact]
        public void Number_expansion()
        {
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var sut = new MessageHandling(PATH);

            sut.Handle(new ExpandNumber('3'));
            sut.Handle(new ExpandNumber('1'));
            sut.Handle(new ExpandNumber('4'));

            var rNumber = sut.Handle(new Number());
            Assert.Equal(314, rNumber.Number);
        }
        
        
        [Fact]
        public void Simple_calculation()
        {
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var sut = new MessageHandling(PATH);

            sut.Handle(new ExpandNumber('2'));
            
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Addition});
            var rNumber = sut.Handle(new Number());
            Assert.Equal(0, rNumber.Number);
            var rResult = sut.Handle(new Result());
            Assert.Equal(2, rResult.Number);
            
            sut.Handle(new ExpandNumber('3'));
            
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Multiplication});
            rNumber = sut.Handle(new Number());
            Assert.Equal(0, rNumber.Number);
            rResult = sut.Handle(new Result());
            Assert.Equal(5, rResult.Number);
        }
        
        
        [Fact]
        public void Calculation()
        {
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var sut = new MessageHandling(PATH);

            // 23 + 17 * 50
            
            sut.Handle(new ExpandNumber('2'));
            sut.Handle(new ExpandNumber('3'));
            
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Addition});
            
            sut.Handle(new ExpandNumber('1'));
            sut.Handle(new ExpandNumber('7'));
            
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Multiplication});
            
            sut.Handle(new ExpandNumber('5'));
            sut.Handle(new ExpandNumber('0'));
            
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Multiplication});
            
            var rResult = sut.Handle(new Result());
            Assert.Equal(2000, rResult.Number);
            var rNumber = sut.Handle(new Number());
            Assert.Equal(0, rNumber.Number);
        }
        
        
        [Fact]
        public void Equals_followed_by_op()
        {
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var sut = new MessageHandling(PATH);

            // 2+3=(5)-1=(4)
            sut.Handle(new ExpandNumber('2'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Addition});
            sut.Handle(new ExpandNumber('3'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Equals});
            var rResult = sut.Handle(new Result());
            Assert.Equal(5, rResult.Number);
            
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Subtraction});
            sut.Handle(new ExpandNumber('1'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Equals});
            rResult = sut.Handle(new Result());
            Assert.Equal(4, rResult.Number);
        }
        
        
        [Fact]
        public void Equals_followed_by_number()
        {
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var sut = new MessageHandling(PATH);

            // 2+3=(5)4*5=(20)
            sut.Handle(new ExpandNumber('2'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Addition});
            sut.Handle(new ExpandNumber('3'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Equals});
            var rResult = sut.Handle(new Result());
            Assert.Equal(5, rResult.Number);
            
            sut.Handle(new ExpandNumber('4'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Multiplication});
            sut.Handle(new ExpandNumber('5'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Equals});
            
            rResult = sut.Handle(new Result());
            Assert.Equal(20, rResult.Number);
        }
        
        
        [Fact]
        public void Op_replacement()
        {
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var sut = new MessageHandling(PATH);

            // 2+*3=(6)
            sut.Handle(new ExpandNumber('2'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Addition});
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Multiplication});
            sut.Handle(new ExpandNumber('3'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Equals});
            var rResult = sut.Handle(new Result());
            Assert.Equal(6, rResult.Number);
        }
        
        
        [Fact]
        public void Div_by_zero()
        {
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var sut = new MessageHandling(PATH);

            // 6/0 2=(3)
            sut.Handle(new ExpandNumber('6'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Division});
            sut.Handle(new ExpandNumber('0'));
            var cResult = sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Equals});
            Assert.IsType<Failure>(cResult);
            
            sut.Handle(new ExpandNumber('2'));
            sut.Handle(new AppendOperator {Op = AppendOperator.Operators.Equals});
            var rResult = sut.Handle(new Result());
            Assert.Equal(3, rResult.Number);
        }
    }
}