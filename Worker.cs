using FlipkartProductTrackerBackend.Controllers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;

namespace FlipkartProductTrackerBackend
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly StockCheckerService _stockController;
        private readonly string path = @"C:\";
        private readonly string fileName = "service_output.txt";
        private SoundPlayer inStock = new SoundPlayer(@"C:\StockAvailable.wav");
        private SoundPlayer noStock = new SoundPlayer(@"C:\NoStock.wav");

        public Worker(ILogger<Worker> logger, StockCheckerService stockController)
        {
            _logger = logger;
            _stockController = stockController;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                File.Create(Path.Combine(path, fileName));
            }
            else
            {
                if (File.Exists(Path.Combine(path, fileName)))
                    File.Delete(Path.Combine(path, fileName));
                else
                    File.Create(Path.Combine(path, fileName));
            }

            _logger.LogInformation($"Spinning up worker at: {DateTimeOffset.Now}");
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Updating stock details at: {DateTimeOffset.Now}, please wait...");

                bool isProductInStock = await _stockController.GetProductStockStatus();

                _logger.LogInformation("Checking PS5 stock now.");

                if (!isProductInStock)
                {
                    _logger.LogError("Product is not in stock. Rechecking in 30 seconds.");
                    noStock.Play();
                }
                else
                {
                    _logger.LogWarning("Product is in stock! Place order now.");
                    inStock.Play();
                }

                await Task.Delay(30000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}