using System;
using System.Collections.Generic;
using System.Linq;

namespace eventorientation
{
    public abstract class MessageModel {}
    public sealed class EmptyMessageModel : MessageModel {}


    public delegate (TMessageModel model, Version version) LoadDelegate<in TIncoming, TMessageModel>(IEventstore es, TIncoming message)
        where TIncoming : IIncoming
        where TMessageModel : MessageModel;

    public delegate void UpdateDelegate(IEventstore es, Event[] events, Version version);
    
    public delegate TQueryResult 
                    HandleQueryDelegate<in TQuery, in TMessageModel, out TQueryResult>(TQuery query, TMessageModel model)
        where TQuery : Query
        where TMessageModel : MessageModel
        where TQueryResult : QueryResult;

    public delegate (CommandStatus commandStatus, Event[] events, Notification[] notifications)
                    HandleCommandDelegate<in TCommand, in TMessageModel>(TCommand command, TMessageModel model)
        where TCommand : Command
        where TMessageModel : MessageModel;

    public delegate Command[] HandleNotificationDelegate<in TNotification, in TMessageModel>(TNotification notification, TMessageModel model)
        where TNotification : Notification
        where TMessageModel : MessageModel;
    
    public delegate (Response response, Notification[] notifications) PipelineDelegate(IIncoming message);



    public interface IQueryPipeline<in TQuery, TMessageModel, out TQueryResult>
        where TQuery : Query
        where TMessageModel : MessageModel
        where TQueryResult : QueryResult
    {
        (TMessageModel model, Version version) Load(IEventstore es, TQuery query);
        
        TQueryResult Project(TQuery query, TMessageModel model);
        
        void Update(IEventstore es, Event[] events, Version version);
    }


    public interface ICommandPipeline<in TCommand, TMessageModel>
        where TCommand : Command
        where TMessageModel : MessageModel
    {
        (TMessageModel model, Version version) Load(IEventstore es, TCommand command);

        (CommandStatus commandStatus, Event[] events, Notification[] notifications) Execute(TCommand command, TMessageModel model);

        void Update(IEventstore es, Event[] events, Version version);
    }
    
    
    
    
    public class MessagePump
    {
        private readonly IEventstore _es;
        private readonly Dictionary<Type, PipelineDelegate> _pipelines;
        private event UpdateDelegate _update; 
        
        
        public MessagePump(IEventstore es) {
            _es = es;
            _pipelines = new Dictionary<Type, PipelineDelegate>();
        }
        
        
        public void RegisterQueryPipeline<TQuery,TMessageModel,TQueryResult>(
            LoadDelegate<TQuery,TMessageModel> load,
            HandleQueryDelegate<TQuery,TMessageModel,TQueryResult> process,
            UpdateDelegate update
        )
            where TQuery : Query
            where TMessageModel : MessageModel
            where TQueryResult : QueryResult
        {
            _pipelines.Add(typeof(TQuery), msg => {
                var (model, _) = load(_es, msg as TQuery);
                var result = process(msg as TQuery, model);
                return (result, new Notification[0]);
            });
            _update += update;
        }

        public void RegisterQueryPipeline<TQuery, TMessageModel, TQueryResult>(IQueryPipeline<TQuery, TMessageModel, TQueryResult> pipeline)
            where TQuery : Query
            where TMessageModel : MessageModel
            where TQueryResult : QueryResult
            => RegisterQueryPipeline<TQuery,TMessageModel,TQueryResult>(pipeline.Load, pipeline.Project, pipeline.Update);
        

        public void RegisterCommandPipeline<TCommand, TMessageModel>(
            LoadDelegate<TCommand, TMessageModel> load,
            HandleCommandDelegate<TCommand, TMessageModel> process,
            UpdateDelegate update
        )
            where TCommand : Command
            where TMessageModel : MessageModel
        {
            _pipelines.Add(typeof(TCommand), msg => {
                var (model, version) = load(_es, msg as TCommand);
                var (commandStatus, events, notifications) = process(msg as TCommand, model);
                var (newVersion, _) = _es.Record(version, events);
                if (events.Any()) _update(_es, events, newVersion);
                return (commandStatus, notifications);
            });
            _update += update;
        }


        public void RegisterCommandPipeline<TCommand, TMessageModel>(ICommandPipeline<TCommand, TMessageModel> pipeline)
            where TCommand : Command
            where TMessageModel : MessageModel
            => RegisterCommandPipeline<TCommand, TMessageModel>(pipeline.Load, pipeline.Execute, pipeline.Update);
        

        public void RegisterNotificationPipeline<TNotification, TMessageModel>(
            LoadDelegate<TNotification, TMessageModel> load,
            HandleNotificationDelegate<TNotification, TMessageModel> process
        )
            where TNotification : Notification
            where TMessageModel : MessageModel
        {
            _pipelines.Add(typeof(TNotification), msg => {
                var (model, version) = load(_es, msg as TNotification);
                var commands = process(msg as TNotification, model);
                var allNotifications = commands.SelectMany(cmd => Handle(cmd).notifications);
                return (new NoResponse(), allNotifications.ToArray());
            });
        }


        private bool _hasBeenStarted;
        public void Start() {
            if (_hasBeenStarted) return;
            _update(_es, new Event[0], null);
            _hasBeenStarted = true;
        }

        public (Response response, Notification[] notifications) Handle(IIncoming input) {
            Start();
            return _pipelines[input.GetType()](input);
        }

        public (TResponse response, Notification[] notifications) Handle<TResponse>(IIncoming input)
            where TResponse : Response
        {
            var result = Handle(input);
            return (result.response as TResponse, result.notifications);
        }
    }
}