namespace CryptoApi.Models
{
    public class CryptoData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Change_24h { get; set; }
        public string MarketCap { get; set; } = string.Empty;
        public string Volume { get; set; } = string.Empty;
        public string TotalSupply { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
