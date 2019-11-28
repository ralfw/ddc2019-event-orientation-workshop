using System.IO;
using eventorientation.tests.message_pump_demo.data;
using eventorientation.tests.message_pump_demo.pipelines;
using Xunit;

namespace eventorientation.tests.message_pump_demo
{
    public class MessagePump_Counter_demo
    {
        [Fact]
        public void Run()
        {
            const string PATH = "msgpump_tests";
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var es = new FilesInFolderEventstore(PATH);
            
            var sut = new MessagePump(es);
            sut.RegisterCommandPipeline(new IncrementPipeline());
            sut.RegisterCommandPipeline(new DecrementPipeline());
            sut.RegisterQueryPipeline(new CounterValuePipeline());

            
            var result = sut.Handle(new Increment {CounterId = "a"});
            Assert.IsType<Success>(result.response);
            result = sut.Handle(new Increment {CounterId = "b"});
            Assert.IsType<Success>(result.response);
            result = sut.Handle(new Increment {CounterId = "a"});
            Assert.IsType<Success>(result.response);

            var qresult = sut.Handle<CounterValue.Result>(new CounterValue {CounterId = "a"});
            Assert.Equal(2, qresult.response.Value);
            
            result = sut.Handle(new Decrement() {CounterId = "x"});
            Assert.IsType<Failure>(result.response);
            
            result = sut.Handle(new Decrement() {CounterId = "a"});
            Assert.IsType<Success>(result.response);
            qresult = sut.Handle<CounterValue.Result>(new CounterValue {CounterId = "a"});
            Assert.Equal(1, qresult.response.Value);
        }
    }
}