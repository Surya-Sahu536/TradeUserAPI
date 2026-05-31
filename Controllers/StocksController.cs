using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeUserAPI.Data;
using TradeUserAPI.Models;

namespace TradeUserAPI.Controllers;

[ApiController]
[Route("api/stocks")]
[Authorize]
public class StocksController : ControllerBase
{
    private readonly TradeDbContext _db;
    public StocksController(TradeDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var stocks = await _db.Stocks.Where(s => s.IsActive).ToListAsync();
        return Ok(stocks.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var stock = await _db.Stocks
            .Include(s => s.PriceHistory.OrderByDescending(p => p.Timestamp).Take(100))
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (stock == null) return NotFound();

        return Ok(new
        {
            stock = ToDto(stock),
            priceHistory = stock.PriceHistory
                .OrderBy(p => p.Timestamp)
                .Select(p => new { p.Price, p.Timestamp })
        });
    }

    private static StockDto ToDto(Stock s)
    {
        var change = s.OpenPrice == 0 ? 0 :
            Math.Round(((s.CurrentPrice - s.OpenPrice) / s.OpenPrice) * 100, 2);

        return new StockDto(
            s.Id, s.Symbol, s.CompanyName, s.Sector,
            s.CurrentPrice, s.OpenPrice, s.DayHigh, s.DayLow,
            s.PreviousClose, s.Volume,
            s.TotalShares, s.AvailableShares,   // ← included
            change, s.LastUpdated);
    }
}