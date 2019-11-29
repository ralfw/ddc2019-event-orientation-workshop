using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Newtonsoft.Json; // NuGet package dependency

namespace eventorientation
{
    public interface IEventstore : IDisposable {
        event Action<Version, long, Event[]> OnRecorded;
        
        (Version version, long finalEventNumber) Record(params Event[] events);
        (Version version, long finalEventNumber) Record(Version expectedVersion, params Event[] events);
        
        Version Version(string id);
        long Length { get; }
        
        Event[] Replay(params Type[] eventTypes);
        Event[] Replay(long firstEventNumber, params Type[] eventTypes);

        (Version version, ContextualEvent[] events) ReplayContext(string contextId);
        (Version version, ContextualEvent[] events) ReplayContext(long firstEventNumber, string contextId);

        (Version[] versions, Event[] events) ReplayWithVersion(Func<Event,string> mapToVersionId, params Type[] eventTypes);
        (Version[] versions, Event[] events) ReplayWithVersion(long firstEventNumber, Func<Event,string> mapToVersionId, params Type[] eventTypes);
    }
    
    
    public abstract class Event {
        public string EventId { get; set; }

        protected Event() { EventId = Guid.NewGuid().ToString(); }
    }

    public abstract class ContextualEvent : Event {
        public string ContextId;
    }


    public class Version {
        public string Id { get; }
        public string Number { get; }

        internal Version(string id, string number) {
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Id of version must not be empty/null!");
            Id = id;
            Number = number;
        }

        public Version(string id) : this(id, "*") {}
    }
    
    
    public class FilesInFolderEventstore : IEventstore
    {
        private const string DEFAUL_PATH = "eventstore.db";
        

        public event Action<Version, long, Event[]> OnRecorded = (v, f, e) => { };
        
        
        private readonly Lock _lock;
        private readonly FilesInFolderEventRepository _repo;
        private readonly Versions _vers;
        
        public FilesInFolderEventstore() : this(DEFAUL_PATH) {}
        public FilesInFolderEventstore(string path) {
            _repo = new FilesInFolderEventRepository(Path.Combine(path, "events"));
            _vers = new Versions(Path.Combine(path, "versions"));
            _lock = new Lock();
        }


        public (Version version, long finalEventNumber) Record(params Event[] events) => Record(null, events);
        public (Version version, long finalEventNumber) Record(Version expectedVersion, params Event[] events) {
            Version newVersion = null;
            long finalEventNumber = -1;
            _lock.TryWrite(() => {
                newVersion = _vers.Update(expectedVersion);
                Store_events();
            });
            OnRecorded(newVersion, finalEventNumber, events);
            return (newVersion, finalEventNumber);


            void Store_events() {
                var n = _repo.Count;
                events.ToList().ForEach(e => _repo.Store(n++, e));
                finalEventNumber = _repo.Count;
            }
        }

        
        public Version Version(string id) => _vers[id];

        public long Length => _repo.Count;


        public Event[] Replay(params Type[] eventTypes) => Replay(-1, eventTypes);
        public Event[] Replay(long firstEventNumber, params Type[] eventTypes)
            => _lock.TryRead(() => {
                    var allEvents = AllEvents(firstEventNumber);
                    return Filter_by_type(allEvents, eventTypes).ToArray();
                });


        public (Version version, ContextualEvent[] events) ReplayContext(string contextId) => ReplayContext(-1, contextId);

        public (Version version, ContextualEvent[] events) ReplayContext(long firstEventNumber, string contextId) {
            return _lock.TryRead<(Version,ContextualEvent[])>(() => {
                var contextEvents = AllEvents(firstEventNumber).Where(e => e is ContextualEvent)
                                                               .OfType<ContextualEvent>()
                                                               .Where(e => e.ContextId == contextId)
                                                               .ToArray();
                return (_vers[contextId], contextEvents);
            });
        }

        
        public (Version[] versions, Event[] events) ReplayWithVersion(Func<Event, string> mapToVersionId, params Type[] eventTypes)
            => ReplayWithVersion(-1, mapToVersionId, eventTypes);
        public (Version[] versions, Event[] events) ReplayWithVersion(long firstEventNumber, Func<Event, string> mapToVersionId, params Type[] eventTypes) {
            return _lock.TryRead(() => {
                var allEvents = AllEvents(firstEventNumber);
                var filteredEvents = Filter_by_type(allEvents, eventTypes).ToArray();
                var versions = Retrieve_versions(filteredEvents).ToArray();
                return (versions, filteredEvents);
            });


            IEnumerable<Version> Retrieve_versions(IEnumerable<Event> events) {
                var idsRetrieved = new HashSet<string>();
                foreach (var e in events) {
                    var versionId = mapToVersionId(e);
                    if (string.IsNullOrEmpty(versionId)) continue;
                    if (idsRetrieved.Contains(versionId)) continue;
                    yield return _vers[versionId];
                    idsRetrieved.Add(versionId);
                }
            }
        }

        
        private IEnumerable<Event> AllEvents(long firstEventNumber) {
            var n = _repo.Count;
            for (var i = firstEventNumber < 0 ? 0 : firstEventNumber; i < n; i++)
                yield return _repo.Load(i);
        }

        private IEnumerable<Event> Filter_by_type(IEnumerable<Event> events, Type[] eventTypes) {
            if (eventTypes.Length <= 0) return events;
                
            var eventTypes_ = new HashSet<Type>(eventTypes);
            return events.Where(e => eventTypes_.Contains(e.GetType()));
        }
        
        
        public void Dispose() {
            _repo.Dispose();
        }
    }
    
    
    internal static class EventSerialization {
        public static string Serialize(this Event e) {
            var eventName = e.GetType().AssemblyQualifiedName;
            var data = JsonConvert.SerializeObject(e);
            var parts = new[]{eventName, data};
            return string.Join("\n", parts);
        }

        public static Event Deserialize(this string e) {
            var lines = e.Split('\n');
            var eventName = lines.First();
            var data = string.Join("\n", lines.Skip(1));
            return (Event)JsonConvert.DeserializeObject(data, Type.GetType(eventName));
        }
    }


    internal class Versions : IDisposable
    {
        private readonly string _path;

        public Versions(string path) {
            _path = path;
            if (Directory.Exists(_path) is false)
                Directory.CreateDirectory(_path);
        }


        public Version this[string id] {
            get {
                var filename = VersionFilename(id);
                if (File.Exists(filename) is false) 
                    return new Version(id);
                
                return new Version(
                            id,
                            File.ReadAllText(filename));
            }
        }


        public Version Update(Version expectedVersion) {
            if (expectedVersion == null) return null;

            var filename = VersionFilename(expectedVersion.Id);
            
            if (File.Exists(filename) is false) {
                var newVersion = new Version(expectedVersion.Id, "1");
                File.WriteAllText(filename, newVersion.Number);
                return newVersion;
            }

            var currentNumber = File.ReadAllText(filename);
            if (expectedVersion.Number != "*" && expectedVersion.Number != currentNumber) throw new InvalidOperationException($"Expected version '{expectedVersion.Number}' for {expectedVersion.Id} does not match current '{currentNumber}'!");
            
            currentNumber = (int.Parse(currentNumber) + 1).ToString();
            var updatedVersion = new Version(expectedVersion.Id, currentNumber);
            File.WriteAllText(filename, updatedVersion.Number);
            return updatedVersion;
        }


        private string VersionFilename(string id) => Path.Combine(_path, id + ".txt");
        
        public void Dispose() {}
    }
    
    
    internal class FilesInFolderEventRepository : IDisposable
    {
        private readonly string _path;

        public FilesInFolderEventRepository(string path) {
            _path = path;
            if (Directory.Exists(_path) is false)
                Directory.CreateDirectory(_path);
        }
        
        
        public void Store(long index, Event e) {
            var text = EventSerialization.Serialize(e);
            Store(index, text);
        }

        private void Store(long index, string text) {
            if (index < 0) throw new InvalidOperationException("Event index must be >= 0!");
            
            var filepath = FilepathFor(index);
            if (File.Exists(filepath)) throw new InvalidOperationException($"Event with index {index} has already been stored and cannot be overwritten!");
            
            File.WriteAllText(filepath, text);
        }

        
        public Event Load(long index) {
            var text = File.ReadAllText(FilepathFor(index));
            return EventSerialization.Deserialize(text);
        }


        public long Count => Directory.GetFiles(_path).Length;

        
        private string FilepathFor(long index) => System.IO.Path.Combine(_path, $"{index:x16}.txt");

        
        public void Dispose() { }
    }
    
    
    internal class Lock
    {
        private const int LOCK_ACQUISITION_TIMEOUT_MSEC = 5000;
        private readonly ReaderWriterLock _lock;

        public Lock() {
            _lock = new ReaderWriterLock();
        }
        
        
        public void TryWrite(Action f) {
            _lock.AcquireWriterLock(LOCK_ACQUISITION_TIMEOUT_MSEC);
            try {
                f();
            }
            finally  {
                _lock.ReleaseWriterLock();
            }
        }
        
        public T TryRead<T>(Func<T> f) {
            _lock.AcquireReaderLock(LOCK_ACQUISITION_TIMEOUT_MSEC);
            try {
                return f();
            }
            finally  {
                _lock.ReleaseReaderLock();
            }
        }
    }
}