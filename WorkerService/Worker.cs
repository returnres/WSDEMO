using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;


namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private string _clientId = "pippo";
        private HubConnection _connection;

        //  Coda interna
        private readonly Channel<Guid> _channel;

        //  Limite concorrenza HTTP
        private readonly SemaphoreSlim _semaphore = new(5);

        public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            // Channel non limitato (puoi cambiarlo dopo)
            _channel = Channel.CreateUnbounded<Guid>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Setup SignalR
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

          

            // Ricezione messaggi → scrivo in coda
            _connection.On<Guid>("ReceiveMessage", async message =>
            {
                _logger.LogInformation($"Messaggio ricevuto: {message}");

                await _channel.Writer.WriteAsync(message, stoppingToken);
            });

            // Ricezione messaggi → scrivo in coda
            _connection.On<string>("TestMessage",  message =>
            {
                _logger.LogInformation($"TestMessage ricevuto: {message}");
            });

            _connection.Reconnecting += error =>
            {
                _logger.LogWarning("Riconnessione in corso...");
                return Task.CompletedTask;
            };

            _connection.Reconnected += async (connectionId) =>
            {
                _logger.LogInformation("Riconnesso!");
                await _connection.InvokeAsync("Register", _clientId);
            };

            _connection.Closed += async error =>
            {
                _logger.LogWarning("Connessione chiusa, retry manuale...");
                await Task.Delay(5000);
                await _connection.StartAsync();
            };

            await _connection.StartAsync(stoppingToken);

            //Registrazione nel gruppo 
            await _connection.InvokeAsync("Register", _clientId);

            _logger.LogInformation($"Registrato come {_clientId}");

            _logger.LogInformation("Connesso a SignalR");

            // Avvio consumer della coda
            var consumerTask = ProcessQueue(stoppingToken);

            await Task.WhenAll(consumerTask);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping worker...");

            if (_connection != null)
            {
                await _connection.StopAsync();
            }

            _channel.Writer.Complete();

            await base.StopAsync(cancellationToken);
        }

        private async Task ProcessQueue(CancellationToken stoppingToken)
        {
            await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                _ = Task.Run(() => ProcessMessage(message, stoppingToken), stoppingToken);
            }
        }

        private async Task ProcessMessage(Guid message, CancellationToken stoppingToken)
        {
           //aspetta slot liberi (in ram)
            await _semaphore.WaitAsync(stoppingToken);

            //after webapi notified me call webapi 
            try
            {
                var client = _httpClientFactory.CreateClient("api");

                var payload = new
                {
                    message = message
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/test", content, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"OK → {message}");
                }
                else
                {
                    _logger.LogError($"Errore API ({response.StatusCode}) → {message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore processing → {message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}