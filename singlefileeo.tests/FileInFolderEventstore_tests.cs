using System;
using System.IO;
using Xunit;

namespace eventorientation.tests
{
    public class FileInFolderEventstore_tests
    {
        class Increment : Event
        {
            public string Id;
        }

        class Decrement : Event
        {
            public string Id;
        }
        
        
        [Fact]
        public void UseCase()
        {
            const string PATH = "usecase_test";
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var sut = new FilesInFolderEventstore(PATH);

            sut.Record(new Increment {Id = "a"});
            sut.Record(new Increment {Id = "b"});
            sut.Record(new Increment {Id = "a"});
            sut.Record(new Decrement {Id = "b"}); // 0
            sut.Record(new Increment {Id = "a"}); // 3

            var events = sut.Replay(typeof(Decrement));
            Assert.Single(events);
            
            events = sut.Replay(typeof(Increment));
            Assert.Equal(4, events.Length);
            Assert.Equal(5, sut.Length);
        }
        
        
        [Fact]
        public void UseCase_with_versioning()
        {
            const string PATH = "usecase_test";
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var sut = new FilesInFolderEventstore(PATH);

            var resultA = sut.Record(new Version("a"), new Increment {Id = "a"});
            Assert.Equal("1", resultA.version.Number);
            var resultB = sut.Record(new Version("b"), new Increment {Id = "b"});
            resultA = sut.Record(resultA.version, new Increment {Id = "a"});
            resultB = sut.Record(resultB.version, new Decrement {Id = "b"});
            Assert.Equal("2", resultB.version.Number);
            sut.Record(resultA.version, new Increment {Id = "a"});

            var ex = Assert.Throws<InvalidOperationException>(() => sut.Record(resultA.version, new Decrement() {Id = "a"}));
            Assert.StartsWith("Expected version", ex.Message);

            var version = sut.Version(resultA.version.Id);
            Assert.Equal("3", version.Number);
        }
        
        
        [Fact]
        public void UseCase_retrieve_with_versions()
        {
            const string PATH = "usecase_test";
            if (Directory.Exists(PATH)) Directory.Delete(PATH,true);
            var sut = new FilesInFolderEventstore(PATH);

            var resultA = sut.Record(new Version("a"), new Increment {Id = "a"});
            var resultB = sut.Record(new Version("b"), new Increment {Id = "b"});
            resultA = sut.Record(resultA.version, new Increment {Id = "a"});
            resultB = sut.Record(resultB.version, new Decrement {Id = "b"});
            sut.Record(resultA.version, new Increment {Id = "a"});

            var result = sut.ReplayWithVersion(MapToVersionId);
            Assert.Equal("3", result.versions[0].Number);
            Assert.Equal("2", result.versions[1].Number);

            string MapToVersionId(Event e) => e switch {
                Increment i => i.Id,
                Decrement d => d.Id
            };
        }
    }
}