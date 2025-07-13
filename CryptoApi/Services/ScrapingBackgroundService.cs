using CryptoApi.Data;
using CryptoApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoApi.Services
{
    public class ScrapingBackgroundService : BackgroundService
    {
        private readonly ILogger<ScrapingBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ScrapingConfig _config;

        public ScrapingBackgroundService(
            ILogger<ScrapingBackgroundService> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<ScrapingConfig> config)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scraping Background Service started");

            // Initial database setup
            await InitializeDatabaseAsync(stoppingToken);

            // Periodic scraping
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scrapingService = scope.ServiceProvider.GetRequiredService<ScrapingService>();
                    var dataStorage = scope.ServiceProvider.GetRequiredService<SqliteDataStorage>();

                    _logger.LogInformation($"Starting scraping cycle for {_config.CurrencySymbols.Count} currencies");
                    var scrapedData = await scrapingService.ScrapeAllCurrenciesAsync();
                    await dataStorage.UpdateData(scrapedData);
                    _logger.LogInformation($"Scraped {scrapedData.Count} currencies");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Scraping error");
                }

                await Task.Delay(TimeSpan.FromMinutes(_config.IntervalMinutes), stoppingToken);
            }
        }

        private async Task InitializeDatabaseAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dataStorage = scope.ServiceProvider.GetRequiredService<SqliteDataStorage>();

            // Apply pending migrations
            await dbContext.Database.MigrateAsync(stoppingToken);

            // Initial data population
            if (!await dbContext.CryptoData.AnyAsync(stoppingToken))
            {
                _logger.LogInformation("Initial database population started");
                var scrapingService = scope.ServiceProvider.GetRequiredService<ScrapingService>();
                var initialData = await scrapingService.ScrapeAllCurrenciesAsync();
                await dataStorage.UpdateData(initialData);
                _logger.LogInformation($"Initialized database with {initialData.Count} currencies");
            }
        }
    }
}