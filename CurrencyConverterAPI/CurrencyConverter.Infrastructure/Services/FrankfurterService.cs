using System.Net.Http.Json;
using System.Text.Json;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace CurrencyConverter.Infrastructure.Services
{
    // Infrastructure/Services/FrankfurterService.cs
    public class FrankfurterService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FrankfurterService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

        public FrankfurterService(HttpClient httpClient, IMemoryCache cache, ILogger<FrankfurterService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;

            _retryPolicy = Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));


        }

        public async Task<decimal> ConvertCurrencyAsync(string from, string to, decimal amount)
        {
            var blockedCurrencies = new[] { "TRY", "PLN", "THB", "MXN" };
            if (blockedCurrencies.Contains(from.ToUpper()) || blockedCurrencies.Contains(to.ToUpper()))
            {
                throw new ArgumentException("Conversion with TRY, PLN, THB, or MXN is not allowed.");
            }

            var url = $"https://api.frankfurter.app/latest?amount={amount}&from={from.ToUpper()}&to={to.ToUpper()}";

            var response = await _circuitBreakerPolicy.ExecuteAsync(() =>
                _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(url)));

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("rates", out var ratesElement) &&
                ratesElement.TryGetProperty(to.ToUpper(), out var rateElement))
            {
                return rateElement.GetDecimal();
            }

            throw new InvalidOperationException("Conversion rate not found.");
        }


        public async Task<PaginatedResult<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end, int page, int pageSize)
        {
            var url = $"https://api.frankfurter.app/{start:yyyy-MM-dd}..{end:yyyy-MM-dd}?from={baseCurrency.ToUpper()}";

            var response = await _circuitBreakerPolicy.ExecuteAsync(() =>
                _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(url)));

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            var rates = new List<ExchangeRate>();

            if (json.RootElement.TryGetProperty("rates", out var ratesElement))
            {
                foreach (var day in ratesElement.EnumerateObject())
                {
                    var date = DateTime.Parse(day.Name);
                    foreach (var currency in day.Value.EnumerateObject())
                    {
                        rates.Add(new ExchangeRate
                        {
                            BaseCurrency = baseCurrency,
                            TargetCurrency = currency.Name,
                            Rate = currency.Value.GetDecimal(),
                            Date = date
                        });
                    }
                }
            }

            var totalCount = rates.Count;
            var pagedData = rates
                .OrderByDescending(r => r.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginatedResult<ExchangeRate>(
                        totalItems: totalCount,
                        pageSize: pageSize,
                        items: pagedData
                    );
        }


        public async Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency)
        {
            if (_cache.TryGetValue($"latest-{baseCurrency}", out ExchangeRate cachedRate))
                return cachedRate;

            var response = await _retryPolicy.ExecuteAsync(() =>
                _httpClient.GetFromJsonAsync<ExchangeRate>($"https://api.frankfurter.app/latest?base={baseCurrency}")
            );

            _cache.Set($"latest-{baseCurrency}", response, TimeSpan.FromMinutes(30));
            return response;
        }

        Task<Braintree.PaginatedResult<ExchangeRate>> IExchangeRateService.GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end, int page, int pageSize)
        {
            throw new NotImplementedException();
        }
    }

}
