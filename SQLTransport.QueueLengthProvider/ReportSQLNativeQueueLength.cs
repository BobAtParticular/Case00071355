using System.Text;
using NServiceBus.Features;
using NServiceBus.Transport;

namespace SQLTransport.QueueLengthProvider
{
    class ReportSQLNativeQueueLength : Feature
    {
        public ReportSQLNativeQueueLength()
        {
            EnableByDefault();
            //DependsOn("NServiceBus.Metrics.ServiceControl.ReportingFeature");
            Prerequisite(ctx => ctx.Settings.Get<TransportDefinition>().GetType().Name == "SqlServerTransport", "SQL server transport not configured");
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton<SQLNativeQueueLengthReporter>();
            context.Services.AddSingleton<PeriodicallyReportQueueLength>();

            context.RegisterStartupTask(b => new PeriodicallyReportQueueLength(b.GetRequiredService<SQLNativeQueueLengthReporter>()));
        }
    }
}
