using System;
using System.Collections.Generic;
using System.Linq;
using eventorientation;

namespace eventorientation
{
    class CounterManagement
    {
        private readonly IEventstore _es;
        private readonly MessagePump _mp;
        
        public CounterManagement(IEventstore es) {
            _es = es;
            _mp = new MessagePump(_es);
            _mp.RegisterCommandPipeline(new DecrementPipeline());
        }


        public CommandStatus Handle(Increment cmd) {
            var e = new Incremented{Name=cmd.Name};
            _es.Record(e);
            return new Success();
        }

        public CommandStatus Handle(Decrement cmd)
            => _mp.Handle(cmd).response as CommandStatus;


        public Value.Result Handle(Value query) {
            return new Value.Result{Value = _es.Replay()
                .Where(Filter)
                .Select(Apply).Sum()};


            bool Filter(Event e) =>
                e switch {
                    Incremented i => i.Name == query.Name,
                    Decremented d => d.Name == query.Name
                };
            int Apply(Event e) =>
                e switch {
                    Incremented _ => 1,
                    Decremented _ => -1
                };
        }
    }


    class DecrementPipeline : ICommandPipeline<Decrement, DecrementPipeline.Model>
    {
        public class Model : MessageModel {
            public HashSet<string> RegisteredNames;
        }

        
        public (Model model, Version version) Load(IEventstore es, Decrement command)
        {
            var model = new Model {
                RegisteredNames = new HashSet<string>(
                    es.Replay(typeof(Incremented)).OfType<Incremented>().Select(x => x.Name)
                )
            };
            return (model, null);
        }

        public (CommandStatus commandStatus, Event[] events, Notification[] notifications) Execute(Decrement command, Model model)
        {
            if (model.RegisteredNames.Contains(command.Name) is false)
                return (new Failure("Dat war nix!"), new Event[0], new Notification[0]);
            
            var e = new Decremented{Name=command.Name};
            return (new Success(), new Event[] {e}, new Notification[0]);
        }

        public void Update(IEventstore es, Event[] events, Version version)
        {}
    }
    
    
}