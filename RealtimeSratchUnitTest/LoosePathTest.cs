#nullable enable
using Xunit;
using Xunit.Abstractions;
using NeeLaboratory.IO;

namespace RealtimeSearchUnitTest
{
    public class LoosePathTest
    {
        private readonly ITestOutputHelper _output;

        public LoosePathTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(@"Bar.txt", @"C:\Foo\Bar.txt")]
        [InlineData(@"Foo", @"C:\Foo\")]
        [InlineData(@"C:", @"C:\")]
        [InlineData(@"C:", @"C:")]
        [InlineData(@"Bar.txt", @"\\HOST\Foo\Bar.txt")]
        [InlineData(@"Foo", @"\\HOST\Foo\")]
        [InlineData(@"\\HOST", @"\\HOST\")]
        [InlineData(@"\\HOST", @"\\HOST")]
        public void GetFileNameTest(string answer, string source)
        {
            Assert.Equal(answer, LoosePath.GetFileName(source));
        }

        [Theory]
        [InlineData(@"C:\Foo", @"C:\Foo\Bar.txt")]
        [InlineData(@"C:\Foo", @"C:\Foo\Bar\")]
        [InlineData(@"C:\", @"C:\Foo\")]
        [InlineData(@"C:", @"C:\Foo\", false)]
        [InlineData(@"", @"C:\")]
        [InlineData(@"", @"C:")]
        [InlineData(@"\\HOST\Foo", @"\\HOST\Foo\Bar.txt")]
        [InlineData(@"\\HOST", @"\\HOST\Foo\")]
        [InlineData(@"", @"\\HOST\")]
        [InlineData(@"", @"\\HOST")]
        public void GetDirectoryName(string answer, string source, bool fixRootDirectory = true)
        {
            Assert.Equal(answer, LoosePath.GetDirectoryName(source, fixRootDirectory));
        }
    }
}
