namespace CryptoApi.Models
{
    public class ScrapingConfig
    {
        public string BaseUrl { get; set; }
        public List<string> CurrencySymbols { get; set; }
        public int IntervalMinutes { get; set; }
    }
}
