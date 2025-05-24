namespace CurrencyConverter.Domain
{
    public class ExchangeRate
    {
        public string? BaseCurrency { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, decimal>? Rates { get; set; }
        public string TargetCurrency { get; set; }
        public decimal Rate { get; set; }
    }
}
