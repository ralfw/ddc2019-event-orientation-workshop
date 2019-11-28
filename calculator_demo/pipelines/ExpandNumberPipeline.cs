using System.Linq;
using calculator_demo.data;
using eventorientation;

namespace calculator_demo.pipelines
{
    public class ExpandNumberPipeline : ICommandPipeline<ExpandNumber,ExpandNumberPipeline.Model>
    {
        public class Model : MessageModel {
            public int Number;
        }


        private readonly Model _model = new Model();


        public (Model model, Version version) Load(IEventstore es, ExpandNumber command) => (_model, null);

        
        public (CommandStatus commandStatus, Event[] events, Notification[] notifications) Execute(ExpandNumber command, Model model) {
            var e = new NumberUpdated {Number = model.Number * 10 + int.Parse(command.Digit.ToString())};
            return (new Success(), new Event[] {e}, new Notification[0]);
        }


        public void Update(IEventstore es, Event[] events, Version version) {
            if (events.Any() is false) events = es.Replay(typeof(NumberUpdated));
            var e = events.LastOrDefault(x => x is NumberUpdated) as NumberUpdated;
            if (e == null) return;
            _model.Number = e.Number;
        }
    }
}