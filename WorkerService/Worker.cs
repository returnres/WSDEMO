using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private HubConnection _connection;
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7247/hub")
               .WithAutomaticReconnect(new[]
    {
    TimeSpan.Zero,
    TimeSpan.FromSeconds(2),
    TimeSpan.FromSeconds(10),
    TimeSpan.FromSeconds(30)
    })
                .Build();

            // ricezione messaggi
            _connection.On<string>("ReceiveMessage", message =>
            {
                _logger.LogInformation($"Messaggio ricevuto: {message}");
            });

            _connection.Reconnecting += error =>
            {
                _logger.LogInformation(" Riconnessione in corso..");
                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                _logger.LogInformation("Riconnesso!");
                return Task.CompletedTask;
            };

            _connection.Closed += async error =>
            {
                _logger.LogInformation("Connessione chiusa, retry manuale...");
                await Task.Delay(5000);
                await _connection.StartAsync();
            };

            await _connection.StartAsync(stoppingToken);
            _logger.LogInformation("start");
            _logger.LogInformation("Connected to SignalR");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_connection != null)
            {
                _logger.LogInformation("stop");
                await _connection.StopAsync();
            }

        }
    }

}