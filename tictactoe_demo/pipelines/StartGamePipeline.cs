using System;
using eventorientation;
using tictactoe_demo.data;
using tictactoe_demo.domain;
using Version = eventorientation.Version;

namespace tictactoe_demo.pipelines
{
    public class StartGamePipeline : ICommandPipeline<StartGame, StartGamePipeline.Model>
    {
        public class Model : MessageModel
        {}

        public (Model model, Version version) Load(IEventstore es, StartGame command)
            => (new Model(), null);

        
        public (CommandStatus commandStatus, Event[] events, Notification[] notifications) Execute(StartGame command, Model model)
        {
            var gameId = Guid.NewGuid().ToString();
            return (
                new Success<string>(gameId),
                new Event[] {
                    new GameStarted{GameId = gameId, NamePlayerX = command.NamePlayerX, NamePlayerO = command.NamePlayerO},
                    new CurrentPlayerChanged{GameId = gameId, Current = Referee.InitialPlayer}
                },
                new Notification[0]
            );
        }

        
        public void Update(IEventstore es, Event[] events, Version version)
        {}
    }
}