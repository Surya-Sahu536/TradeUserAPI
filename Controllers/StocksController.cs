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
        // Query 1: get stock without price history
        var stock = await _db.Stocks
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (stock == null) return NotFound();

        // Query 2: get price history separately — fast with index
        var priceHistory = await _db.PriceHistory
            .Where(p => p.StockId == id)
            .OrderByDescending(p => p.Timestamp)
            .Take(100)
            .OrderBy(p => p.Timestamp)   // re-order ascending for chart
            .Select(p => new { p.Price, p.Timestamp })
            .ToListAsync();

        var change = stock.OpenPrice == 0 ? 0 :
            Math.Round(((stock.CurrentPrice - stock.OpenPrice) / stock.OpenPrice) * 100, 2);

        return Ok(new
        {
            stock = new
            {
                stock.Id,
                stock.Symbol,
                stock.CompanyName,
                stock.Sector,
                stock.CurrentPrice,
                stock.OpenPrice,
                stock.DayHigh,
                stock.DayLow,
                stock.PreviousClose,
                stock.Volume,
                stock.TotalShares,
                stock.AvailableShares,
                ChangePercent = change,
                stock.LastUpdated
            },
            priceHistory
        });
    }

    // UserAPI/Controllers/AdminStocksController.cs (internal endpoint)
    [HttpPost("{id}/issue-shares")]
    public async Task<IActionResult> IssueShares(int id, [FromBody] IssueSharesDto dto)
    {
        var stock = await _db.Stocks.FindAsync(id);
        if (stock == null) return NotFound();

        if (dto.AdditionalShares <= 0)
            return BadRequest(new { message = "Additional shares must be greater than 0." });

        stock.TotalShares += dto.AdditionalShares;
        stock.AvailableShares += dto.AdditionalShares;
        stock.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Issued {dto.AdditionalShares:N0} new shares for {stock.Symbol}.",
            totalShares = stock.TotalShares,
            availableShares = stock.AvailableShares
        });
    }

    public record IssueSharesDto(long AdditionalShares, string Reason);

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