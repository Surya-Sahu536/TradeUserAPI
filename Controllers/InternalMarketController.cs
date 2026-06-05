using Microsoft.AspNetCore.Mvc;
using TradeUserAPI.Services;

namespace TradeUserAPI.Controllers;

/// <summary>
/// Internal endpoints called only by AdminAPI — not exposed to users.
/// No [Authorize] since AdminAPI calls these server-to-server.
/// </summary>
[ApiController]
[Route("api/internal/market")]
public class InternalMarketController : ControllerBase
{
    private readonly MarketEngine _market;
    private readonly PriceSimulatorService _simulator;

    public InternalMarketController(MarketEngine market, PriceSimulatorService simulator)
    {
        _market = market;
        _simulator = simulator;
    }

    [HttpPost("sentiment")]
    public IActionResult SetSentiment([FromBody] SentimentRequest req)
    {
        _market.SetSentiment(req.Value);
        return Ok(new
        {
            message = $"Market sentiment set to {_market.SentimentLabel}.",
            value = req.Value
        });
    }

    [HttpPost("event")]
    public IActionResult TriggerEvent([FromBody] EventRequest req)
    {
        _market.TriggerEvent(req.StockId, req.ImpactPercent, req.DurationTicks);
        var direction = req.ImpactPercent >= 0 ? "📈" : "📉";
        return Ok(new
        {
            message = $"{direction} Event '{req.EventName}' applied to stock {req.StockId}. " +
                      $"{req.ImpactPercent:+0.##;-0.##}% over {req.DurationTicks} ticks.",
            stockId = req.StockId,
            impact = req.ImpactPercent
        });
    }

    [HttpPost("volatility")]
    public IActionResult SetVolatility([FromBody] VolatilityRequest req)
    {
        _simulator.SetVolatility(req.StockId, req.Value);
        return Ok(new
        {
            message = $"Volatility for stock {req.StockId} set to {req.Value}."
        });
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            sentiment = _market.SentimentLabel,
            sentimentValue = _market.GetSentimentValue()
        });
    }
}

public record SentimentRequest(double Value);
public record EventRequest(int StockId, string EventName, double ImpactPercent, int DurationTicks);
public record VolatilityRequest(int StockId, double Value);
