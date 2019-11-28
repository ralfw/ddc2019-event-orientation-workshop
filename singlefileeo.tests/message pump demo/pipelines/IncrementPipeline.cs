using eventorientation.tests.message_pump_demo.data;

namespace eventorientation.tests.message_pump_demo.pipelines
{
    public class IncrementPipeline : ICommandPipeline<Increment, IncrementPipeline.IncrementModel>
    {
        public class IncrementModel : MessageModel {}

            
        public (IncrementModel model, Version version) Load(IEventstore es, Increment command)
            => (new IncrementModel(), null);

        public (CommandStatus commandStatus, Event[] events, Notification[] notifications) Execute(Increment command, IncrementModel model)
            => (new Success(), 
                new Event[] {new Incremented {CounterId = command.CounterId}}, 
                new Notification[0]);

        public void Update(IEventstore es, Event[] events, Version version) {}
    }
}