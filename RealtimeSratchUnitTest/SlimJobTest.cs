#nullable enable
using Xunit;
using System.Threading.Tasks;
using System.Threading;
using NeeLaboratory.Threading.Jobs;
using System;
using System.Windows.Navigation;
using Xunit.Abstractions;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Windows.Threading;
using System.Diagnostics;

namespace RealtimeSearchUnitTest
{
    public class SlimJobTest
    {
        private readonly ITestOutputHelper _output;

        public SlimJobTest(ITestOutputHelper output)
        {
            _output = output;
        }


        [Fact]
        public async Task BasicTest()
        {
            var dispatcher = new SlimJobEngine("test");

            int a = 0;
            var job = dispatcher.InvokeAsync(() => { a = 1; }, CancellationToken.None);
            await job;
            Assert.Equal(SlimJobStates.Completed, job.State);
            Assert.Null(job.Exception);
            Assert.Equal(1, a);
        }

        [Fact]
        public async Task SumTest()
        {
            var dispatcher = new SlimJobEngine("test");

            int a = 0;
            var job1 = dispatcher.InvokeAsync(() => { Thread.Sleep(100); a++; }, CancellationToken.None);
            var job2 = dispatcher.InvokeAsync(() => { Thread.Sleep(100); a++; }, CancellationToken.None);
            var job3 = dispatcher.InvokeAsync(() => { Thread.Sleep(100); a++; }, CancellationToken.None);
            Assert.Equal(0, a);
            await job3;
            Assert.Equal(SlimJobStates.Completed, job1.State);
            Assert.Equal(SlimJobStates.Completed, job2.State);
            Assert.Equal(SlimJobStates.Completed, job3.State);
            Assert.Null(job1.Exception);
            Assert.Null(job2.Exception);
            Assert.Null(job3.Exception);
            Assert.Equal(3, a);
        }


        [Fact]
        public async Task HeavyJobTest()
        {
            using var dispatcher = new SlimJobEngine("test");

            int a = 0;
            var job = dispatcher.InvokeAsync(() => { Thread.Sleep(1000); a = 1; }, CancellationToken.None);
            Assert.Equal(SlimJobStates.Pending, job.State);
            await Task.Delay(100);
            Assert.Equal(SlimJobStates.Executing, job.State);
            await job;
            Assert.Equal(SlimJobStates.Completed, job.State);
            Assert.Null(job.Exception);
            Assert.Equal(1, a);
        }

        [Fact]
        public async Task AbortTest()
        {
            using var dispatcher = new SlimJobEngine("test");

            int a = 0;
            _ = dispatcher.InvokeAsync(() => { Thread.Sleep(100); }, CancellationToken.None);
            var job = dispatcher.InvokeAsync(() => { Thread.Sleep(1000); a = 1; }, CancellationToken.None);
            var result = job.Abort();
            Assert.True(result);
            await job;
            Assert.Equal(SlimJobStates.Aborted, job.State);
            Assert.Null(job.Exception);
            Assert.Equal(0, a);
        }

        [Fact]
        public async Task ExceptionTest()
        {
            using var dispatcher = new SlimJobEngine("test");

            int a = 0;
            var job = dispatcher.InvokeAsync(() => { a = 1; throw new ApplicationException(); }, CancellationToken.None);
            await job;
            Assert.Equal(SlimJobStates.Aborted, job.State);
            Assert.True(job.Exception is ApplicationException);
            Assert.Equal(1, a);
        }

        [Fact(Timeout = 3000)]
        public async Task BasicFunc()
        {
            var dispatcher = new SlimJobEngine("test");

            var job1 = dispatcher.InvokeAsync(() => { return 1; }, CancellationToken.None);
            var job2 = dispatcher.InvokeAsync(() => { return 2; }, CancellationToken.None);

            //await Task.Delay(100);

            //_output.WriteLine($"job1...");
            var a = await job1;
            Assert.Equal(SlimJobStates.Completed, job1.State);
            Assert.Null(job1.Exception);
            Assert.Equal(1, a);

            //_output.WriteLine($"job2...");
            var b = await job2;
            Assert.Equal(SlimJobStates.Completed, job2.State);
            Assert.Null(job2.Exception);
            Assert.Equal(2, b);
        }

        [Fact]
        public async Task MemoryLeakTest()
        {
            var dispatcher = new SlimJobEngine("test");
            int count = 0;
            SlimJobOperation? job = null;
            for (int i = 0; i < 1000; i++)
            {
                job = dispatcher.InvokeAsync(() => { count++; }, CancellationToken.None);
                _ = dispatcher.InvokeAsync(() => { return 1; }, CancellationToken.None);
            }
            Assert.NotNull(job);
            await job!;
            Assert.Equal(1000, count);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();

            _output.WriteLine($"GC.Complete");
        }
    }
}
