using System.Linq;
using calculator_demo.data;
using eventorientation;

namespace calculator_demo.pipelines
{
    public class ResultPipeline : IQueryPipeline<Result,ResultPipeline.Model,Result.Value>
    {
        public class Model : MessageModel {
            public int Number;
        }


        private readonly Model _model = new Model();
        

        (Model model, Version version) IQueryPipeline<Result, Model, Result.Value>.Load(IEventstore es, Result query)
            => (_model, null);


        public Result.Value Project(Result query, Model model)
            => new Result.Value {Number = model.Number};


        public void Update(IEventstore es, Event[] events, Version version) {
            if (events.Any() is false) events = es.Replay(typeof(ResultCalculated));
            var e = events.LastOrDefault(x => x is ResultCalculated) as ResultCalculated;
            if (e == null) return;
            _model.Number = e.Number;
        }
    }
}