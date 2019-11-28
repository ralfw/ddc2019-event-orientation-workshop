using System.IO;
using eventorientation;
using Xunit;

namespace eventorientation
{
    public class Counter_demo
    {
        private const string PATH = "counters.db";
        
        [Fact]
        public void Test1() {
            if(Directory.Exists(PATH)) Directory.Delete(PATH, true);
            var es = new FilesInFolderEventstore(PATH);
            
            var sut = new CounterManagement(es);
            sut.Handle(new Increment {Name = "a"});
            sut.Handle(new Increment {Name = "b"});
            sut.Handle(new Increment {Name = "a"});
            var result = sut.Handle(new Value {Name = "a"});
            Assert.Equal(2, result.Value);
        }
    }
}