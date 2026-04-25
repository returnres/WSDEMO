using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;


namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private HubConnection _connection;
        private readonly IHttpClientFactory _httpClientFactory;
        public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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

            // ricezione notifica da server
            _connection.On<string>("ReceiveMessage", async message =>
            {
                _logger.LogInformation($"Messaggio ricevuto: {message}");
                try
                {
                    var client = _httpClientFactory.CreateClient("MyApi");

                    var payload = new
                    {
                        message = message
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("api/test", content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Chiamata API OK");
                    }
                    else
                    {
                        _logger.LogError($"Errore API: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante chiamata API");
                }
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