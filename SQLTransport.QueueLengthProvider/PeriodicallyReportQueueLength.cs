using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;

namespace SQLTransport.QueueLengthProvider
{
    class PeriodicallyReportQueueLength : FeatureStartupTask
    {
        readonly MsmqNativeQueueLengthReporter reporter;

        TimeSpan delayBetweenReports = TimeSpan.FromSeconds(1);
        CancellationTokenSource cancellationTokenSource;
        Task task;

        static readonly ILog Log = LogManager.GetLogger<PeriodicallyReportQueueLength>();

        public PeriodicallyReportQueueLength(MsmqNativeQueueLengthReporter reporter) => this.reporter = reporter;

        protected override Task OnStart(IMessageSession messageSession)
        {
            cancellationTokenSource = new CancellationTokenSource();

            task = Task.Run(async () =>
            {
                try
                {
                    reporter.Warmup();
                }
                catch (Exception ex)
                {
                    Log.Error("Error warming up reporter.", ex);
                    return;
                }

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(delayBetweenReports, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        // private token, reporting is being stopped, don't log the exception because the stack trace of Task.Delay is not interesting
                        break;
                    }

                    try
                    {
                        reporter.ReportNativeQueueLength();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Error reporting MSMQ native queue length", ex);
                    }
                }
            },
                CancellationToken.None);

            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession messageSession)
        {
            cancellationTokenSource.Cancel();

            return task;
        }
    }
}
