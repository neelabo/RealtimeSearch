#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RealtimeSearchUnitTest
{
    public class SandBox
    {
        private readonly ITestOutputHelper _output;

        public SandBox(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CancellationTokenSourceDisposeTestAsync()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            await Task.Delay(100);

            cts.Cancel();
            cts.Dispose();
            await Task.Delay(100);

            Assert.True(token.IsCancellationRequested);
        }
    }
}
