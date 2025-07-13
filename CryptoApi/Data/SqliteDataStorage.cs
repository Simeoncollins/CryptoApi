using CryptoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoApi.Data
{
    public class SqliteDataStorage
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SqliteDataStorage> _logger;

        public SqliteDataStorage(AppDbContext context, ILogger<SqliteDataStorage> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task UpdateData(List<CryptoData> newData)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Clear existing data
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM CryptoData");

                // Add new data
                await _context.CryptoData.AddRangeAsync(newData);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation($"Updated database with {newData.Count} records");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database update failed");
                throw;
            }
        }

        public IEnumerable<CryptoData> GetAllData()
        {
            return _context.CryptoData.AsNoTracking().ToList();
        }
    }
}
