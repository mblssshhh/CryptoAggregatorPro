using System.Globalization;
using System.Text.Json;

namespace CryptoAggregatorPro.Helpers
{
    public static class KuCoinVolumeProvider
    {
        private static readonly HttpClient _http = new();
        private static decimal _lastVolume = 0m;

        public static async Task<decimal> GetVolumeAsync(string symbol)
        {
            try
            {
                var url = $"https://api.kucoin.com/api/v1/market/stats?symbol={symbol}";
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");

                decimal.TryParse(data.GetProperty("volValue").GetString(),
                    NumberStyles.Any, CultureInfo.InvariantCulture,
                    out _lastVolume);

                return _lastVolume;
            }
            catch
            {
                return _lastVolume;
            }
        }
    }

}
