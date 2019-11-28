using calculator_demo.data;
using calculator_demo.pipelines;
using eventorientation;

namespace calculator_demo
{
    public class MessageHandling
    {
        private readonly MessagePump _mp;
        
        public MessageHandling(string path = "calculator.db") {
            var es = new FilesInFolderEventstore(path);
            _mp=new MessagePump(es);    
            _mp.RegisterCommandPipeline(new ExpandNumberPipeline());
            _mp.RegisterQueryPipeline(new NumberPipeline());
            _mp.RegisterQueryPipeline(new ResultPipeline());
            _mp.RegisterCommandPipeline(new AppendOperatorPipeline());
        }


        public CommandStatus Handle(ExpandNumber cmd)
            => _mp.Handle(cmd).response as CommandStatus;

        public CommandStatus Handle(AppendOperator cmd)
            => _mp.Handle(cmd).response as CommandStatus;

        public Number.Value Handle(Number qry)
            => _mp.Handle(qry).response as Number.Value;
        
        public Result.Value Handle(Result qry)
            => _mp.Handle(qry).response as Result.Value;
    }
}