using CurrencyConverter.Domain;

namespace CurrencyConverter.Application.Interfaces
{
    public interface IExchangeRateService
    {
        Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency);
        Task<decimal> ConvertCurrencyAsync(string from, string to, decimal amount);
        Task<Braintree.PaginatedResult<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end, int page, int pageSize);
    }
}
