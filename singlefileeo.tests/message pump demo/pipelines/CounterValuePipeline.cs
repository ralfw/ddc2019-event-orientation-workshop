using System.Collections.Generic;
using System.Linq;
using eventorientation.tests.message_pump_demo.data;

namespace eventorientation.tests.message_pump_demo.pipelines
{
    public class CounterValuePipeline : IQueryPipeline<CounterValue, CounterValuePipeline.CounterValueModel, CounterValue.Result>
    {
        public class CounterValueModel : MessageModel {
            public int Value;
        }

        public (CounterValueModel model, Version version) Load(IEventstore es, CounterValue query) {
            var counters = es.Replay(typeof(Incremented), typeof(Decremented)).Aggregate(
                new Dictionary<string, int>(),
                (dict, e) => {
                    switch (e) {
                        case Incremented i:
                            if (dict.ContainsKey(i.CounterId) is false) dict[i.CounterId] = 0;
                            dict[i.CounterId] += 1;
                            break;
                        case Decremented d:
                            dict[d.CounterId] -= 1;
                            break;
                    }
                    return dict;
                });
                
            var model = new CounterValueModel();
            counters.TryGetValue(query.CounterId, out model.Value);
            return (model, null);
        }

            
        public CounterValue.Result Project(CounterValue query, CounterValueModel model)
            => new CounterValue.Result{ CounterId=query.CounterId, Value = model.Value };

            
        public void Update(IEventstore es, Event[] events, Version version) {}
    }
}