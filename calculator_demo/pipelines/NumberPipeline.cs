using System.Linq;
using calculator_demo.data;
using eventorientation;

namespace calculator_demo.pipelines
{
    public class NumberPipeline : IQueryPipeline<Number,NumberPipeline.Model,Number.Value>
    {
        public class Model : MessageModel {
            public int Number;
        }


        private readonly Model _model = new Model();


        (Model model, Version version) IQueryPipeline<Number, Model, Number.Value>.Load(IEventstore es, Number query)
            => (_model, null);


        public Number.Value Project(Number query, Model model)
            => new Number.Value {Number = model.Number};


        public void Update(IEventstore es, Event[] events, Version version) {
            if (events.Any() is false) events = es.Replay(typeof(NumberUpdated));
            var e = events.LastOrDefault(x => x is NumberUpdated) as NumberUpdated;
            if (e == null) return;
            _model.Number = e.Number;
        }
    }
}