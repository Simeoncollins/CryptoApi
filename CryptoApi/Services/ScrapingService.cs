using CryptoApi.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CryptoApi.Services
{
    public class ScrapingService
    {
        private readonly ScrapingConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public ScrapingService(
            IOptions<ScrapingConfig> config,
            IHttpClientFactory httpClientFactory)
        {
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<CryptoData>> ScrapeAllCurrenciesAsync()
        {
            var results = new List<CryptoData>();
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            foreach (var symbol in _config.CurrencySymbols)
            {
                try
                {
                    var url = $"{_config.BaseUrl}{symbol}";
                    var html = await httpClient.GetStringAsync(url);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    results.Add(ParseCurrencyData(doc, symbol));
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scraping {symbol}: {ex.Message}");
                }
            }

            return results;
        }

        private CryptoData ParseCurrencyData(HtmlDocument doc, string symbol)
        {
            List<string> otherValues = ExtractSpecificSpanValues(doc);
            return new CryptoData
            {
                Name = ExtractName(doc),
                Symbol = doc.DocumentNode.SelectSingleNode("//span[@data-role='coin-symbol']")?.InnerText.Trim() ?? "N/A",
                ImageUrl = ExtractImageSrc(doc),
                Price = ParseDecimal(doc.DocumentNode.SelectSingleNode("//span[@data-test='text-cdp-price-display']")?.InnerText),
                Change_24h = ExtractPercentageChange(doc),
                MarketCap = otherValues.Count > 0 ? otherValues.ElementAt(0) : "N/A",
                Volume = otherValues.Count > 1 ? otherValues.ElementAt(1) : "N/A",
                TotalSupply = otherValues.Count > 2 ? otherValues.ElementAt(2) : "N/A"
            };
        }

        private string ExtractName(HtmlDocument doc)
        {
            try
            {
                // Locate the <p> element with the class containing 'change-text'
                var nameNode = doc.DocumentNode
                    .SelectSingleNode("//span[@data-role='coin-name']");

                if (nameNode == null)
                    return "N/A";

                // Remove <span> child element if it exists
                var spanNode = nameNode.SelectSingleNode(".//span");
                if (spanNode != null)
                {
                    spanNode.Remove();
                }

                // Now get the cleaned inner text
                return nameNode.InnerText.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting percentage change: {ex.Message}");
                return "N/A";
            }
        }

        private decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            var cleanValue = value.Replace("$", "").Replace("%", "").Replace(",", "").Trim();
            return decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0;
        }

        private List<string> ExtractSpecificSpanValues(HtmlDocument doc)
        {
            var values = new List<string>();

            try
            {
                // Select all <div> elements with the exact class name
                var nodes = doc.DocumentNode.SelectNodes("//div[@class='BasePopover_base__T5yOf popover-base']/span");

                if (nodes == null || nodes.Count < 5)
                    return values; // Not enough elements to get 1st, 2nd, and 5th

                // Get 1st, 2nd, and 5th elements (0-based index)
                values.Add(nodes[0].InnerText.Trim());
                values.Add(nodes[1].InnerText.Trim());
                values.Add(nodes[4].InnerText.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting span values: {ex.Message}");
            }

            return values;
        }

        private string ExtractImageSrc(HtmlDocument doc)
        {
            try
            {
                // Locate the <div> with data-role="coin-logo"
                var divNode = doc.DocumentNode
                    .SelectSingleNode("//div[@data-role='coin-logo']");

                if (divNode == null)
                    return "N/A";

                // Find the <img> tag inside it
                var imgNode = divNode.SelectSingleNode(".//img");

                if (imgNode == null)
                    return "N/A";

                // Get the 'src' attribute value
                var src = imgNode.GetAttributeValue("src", "N/A");
                return src;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting image src: {ex.Message}");
                return "N/A";
            }
        }


        private decimal ExtractPercentageChange(HtmlDocument doc)
        {
            try
            {
                // Locate the <p> element with the class containing 'change-text'
                var percentageNodes = doc.DocumentNode
                    .SelectNodes("//p[contains(@class, 'change-text')]");

                if (percentageNodes == null)
                    return 0;
                var percentageNode = percentageNodes[1];
                if (percentageNode == null)
                    return 0;
                // Remove <svg> child element if it exists
                var svgNode = percentageNode.SelectSingleNode(".//svg");
                if (svgNode != null)
                {
                    svgNode.Remove();
                }

                // Now get the cleaned inner text
                return ParseDecimal(percentageNode.InnerText.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting percentage change: {ex.Message}");
                return 0;
            }
        }

    }
}
