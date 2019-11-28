using System.Collections.Generic;
using System.Linq;
using eventorientation.tests.message_pump_demo.data;

namespace eventorientation.tests.message_pump_demo.pipelines
{
    public class DecrementPipeline : ICommandPipeline<Decrement, DecrementPipeline.DecrementModel>
    {
        public class DecrementModel : MessageModel {
            public bool CounterExists;
        }


        private readonly HashSet<string> _counterIds = new HashSet<string>();
            
            
        public (DecrementModel model, Version version) Load(IEventstore es, Decrement command) {
            var model = new DecrementModel {
                CounterExists = _counterIds.Contains(command.CounterId),
            };
            return (model, es.Version(command.CounterId));
        }

        public (CommandStatus commandStatus, Event[] events, Notification[] notifications) Execute(Decrement command, DecrementModel model) {
            if(model.CounterExists is false) return (new Failure("Counter not found! Increment first."), new Event[0], new Notification[0]);

            return (new Success(),
                new Event[] {new Decremented {CounterId = command.CounterId}},
                new Notification[0]);
        }

        public void Update(IEventstore es, Event[] events, Version version) {
            if (events.Any() is false) events = es.Replay(typeof(Incremented)).ToArray();
            events.Where(e => e is Incremented).ToList()
                .ForEach(e => _counterIds.Add(((Incremented)e).CounterId));
        }
    }
}