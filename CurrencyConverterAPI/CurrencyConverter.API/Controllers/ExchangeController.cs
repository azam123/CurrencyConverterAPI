// API/Controllers/ExchangeController.cs
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "User")]
[ApiController]
[Route("api/[controller]")]
public class ExchangeController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<ExchangeController> _logger;

    public ExchangeController(IExchangeRateService exchangeRateService, ILogger<ExchangeController> logger)
    {
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] string baseCurrency)
    {
        if (new[] { "TRY", "PLN", "THB", "MXN" }.Contains(baseCurrency.ToUpper()))
            return BadRequest("Currency not supported.");

        var result = await _exchangeRateService.GetLatestRatesAsync(baseCurrency);
        return Ok(result);
    }

    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] ConvertCurrencyRequest request)
    {
        if (new[] { "TRY", "PLN", "THB", "MXN" }.Any(c => c == request.From.ToUpper() || c == request.To.ToUpper()))
            return BadRequest("Currency not supported.");

        var converted = await _exchangeRateService.ConvertCurrencyAsync(request.From, request.To, request.Amount);
        return Ok(new { converted });
    }
}
