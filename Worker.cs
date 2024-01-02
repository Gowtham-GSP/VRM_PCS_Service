using VRM_PCS_SERVICE.Interface;

namespace VRM_PCS_SERVICE
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceEngine _ServiceEngine;

        public Worker(ILogger<Worker> logger, IServiceEngine serviceEngine)
        {
            _logger = logger;
            _ServiceEngine = serviceEngine;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ServiceEngine.Start();
            _logger.LogInformation("ServiceEngine Started");
            await Task.CompletedTask;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _ServiceEngine.Start();
            _logger.LogInformation("Service Starting..");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Service");
            _ServiceEngine.Stop();
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _ServiceEngine.Dispose();
            _logger.LogInformation("Service Disposed");
            base.Dispose();
        }
    }
}