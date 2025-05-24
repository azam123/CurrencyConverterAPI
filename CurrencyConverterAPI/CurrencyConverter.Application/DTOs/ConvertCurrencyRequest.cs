namespace CurrencyConverter.Application.DTOs
{
    public class ConvertCurrencyRequest
    {
        public string? From { get; set; }
        public string? To { get; set; }
        public decimal Amount { get; set; }
    }
}
