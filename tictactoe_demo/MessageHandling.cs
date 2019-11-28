using eventorientation;
using tictactoe_demo.data;
using tictactoe_demo.pipelines;

namespace tictactoe_demo
{
    public class MessageHandling : IMessageHandling
    {
        private readonly MessagePump _ms;
        
        public MessageHandling(IEventstore es) {
            _ms = new MessagePump(es);
            _ms.RegisterCommandPipeline(new StartGamePipeline());
            _ms.RegisterCommandPipeline(new PlaceTokenPipeline());
            _ms.RegisterQueryPipeline(new GamePipeline());
        }

        public CommandStatus Handle(StartGame cmd)
            => _ms.Handle(cmd).response as CommandStatus;

        public CommandStatus Handle(PlaceToken cmd)
            => _ms.Handle(cmd).response as CommandStatus;

        public GameView.Result Handle(GameView qry)
            => _ms.Handle(qry).response as GameView.Result;
    }
}