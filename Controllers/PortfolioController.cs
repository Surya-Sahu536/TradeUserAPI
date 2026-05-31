using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeUserAPI.Data;
using TradeUserAPI.Models;

namespace TradeUserAPI.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly TradeDbContext _db;
    public PortfolioController(TradeDbContext db) => _db = db;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetPortfolio()
    {
        var user = await _db.Users.FindAsync(UserId);
        if (user == null) return Unauthorized();

        var holdings = await _db.Holdings
            .Include(h => h.Stock)
            .Where(h => h.UserId == UserId)
            .ToListAsync();

        var holdingDtos = holdings.Select(h =>
        {
            var currentValue = h.Stock.CurrentPrice * h.Quantity;
            var invested = h.AverageBuyPrice * h.Quantity;
            var pnl = currentValue - invested;
            var pnlPct = invested == 0 ? 0 : Math.Round((pnl / invested) * 100, 2);
            return new HoldingDto(h.StockId, h.Stock.Symbol, h.Stock.CompanyName,
                h.Quantity, h.AverageBuyPrice, h.Stock.CurrentPrice,
                Math.Round(currentValue, 2), Math.Round(pnl, 2), pnlPct);
        }).ToList();

        var totalInvested = holdingDtos.Sum(h => h.AverageBuyPrice * h.Quantity);
        var currentValue = holdingDtos.Sum(h => h.CurrentValue);
        var totalPnl = currentValue - totalInvested;
        var totalPnlPct = totalInvested == 0 ? 0 : Math.Round((totalPnl / totalInvested) * 100, 2);

        return Ok(new PortfolioDto(
            user.WalletBalance,
            Math.Round(totalInvested, 2),
            Math.Round(currentValue, 2),
            Math.Round(totalPnl, 2),
            totalPnlPct,
            holdingDtos
        ));
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
    {
        var txs = await _db.Transactions
            .Include(t => t.Stock)
            .Where(t => t.UserId == UserId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .Select(t => new TransactionDto(t.Id, t.Stock.Symbol, t.Stock.CompanyName,
                t.Type, t.Quantity, t.PricePerShare, t.TotalAmount, t.CreatedAt))
            .ToListAsync();

        return Ok(txs);
    }
}
