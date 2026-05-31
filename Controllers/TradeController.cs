using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeUserAPI.Data;
using TradeUserAPI.Models;
using TradeUserAPI.Services;

namespace TradeUserAPI.Controllers;

[ApiController]
[Route("api/trade")]
[Authorize]
public class TradeController : ControllerBase
{
    private readonly TradeDbContext _db;
    private readonly MarketEngine _market;

    public TradeController(TradeDbContext db, MarketEngine market)
    {
        _db = db;
        _market = market;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("buy")]
    public async Task<IActionResult> Buy(TradeDto dto)
    {
        if (dto.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0." });

        var user = await _db.Users.FindAsync(UserId);
        var stock = await _db.Stocks.FindAsync(dto.StockId);

        if (user == null || stock == null || !stock.IsActive)
            return BadRequest(new { message = "Invalid user or stock." });

        // ── Check available shares ────────────────────────────────
        if (stock.AvailableShares < dto.Quantity)
            return BadRequest(new
            {
                message = $"Not enough shares available. Only {stock.AvailableShares:N0} shares left for {stock.Symbol}."
            });

        // ── Check wallet balance ──────────────────────────────────
        var totalCost = stock.CurrentPrice * dto.Quantity;
        if (user.WalletBalance < totalCost)
            return BadRequest(new
            {
                message = $"Insufficient balance. Need ₹{totalCost:N2}, available ₹{user.WalletBalance:N2}."
            });

        // ── Deduct wallet ─────────────────────────────────────────
        user.WalletBalance -= totalCost;

        // ── Reduce available shares ───────────────────────────────
        stock.AvailableShares -= dto.Quantity;
        stock.LastUpdated = DateTime.UtcNow;

        // ── Update holding ────────────────────────────────────────
        var holding = await _db.Holdings
            .FirstOrDefaultAsync(h => h.UserId == UserId && h.StockId == dto.StockId);

        if (holding == null)
        {
            holding = new Holding
            {
                UserId = UserId,
                StockId = dto.StockId,
                Quantity = dto.Quantity,
                AverageBuyPrice = stock.CurrentPrice
            };
            _db.Holdings.Add(holding);
        }
        else
        {
            var totalShares = holding.Quantity + dto.Quantity;
            holding.AverageBuyPrice = ((holding.AverageBuyPrice * holding.Quantity)
                                      + (stock.CurrentPrice * dto.Quantity)) / totalShares;
            holding.Quantity = totalShares;
        }

        // ── Record transaction ────────────────────────────────────
        _db.Transactions.Add(new Transaction
        {
            UserId = UserId,
            StockId = dto.StockId,
            Type = "BUY",
            Quantity = dto.Quantity,
            PricePerShare = stock.CurrentPrice,
            TotalAmount = totalCost
        });

        await _db.SaveChangesAsync();

        // Notify market engine so price reacts to this buy
        _market.RecordTrade(dto.StockId, "BUY", dto.Quantity, stock.TotalShares);

        return Ok(new
        {
            message = $"Bought {dto.Quantity} share(s) of {stock.Symbol} at ₹{stock.CurrentPrice:N2}.",
            newBalance = user.WalletBalance,
            availableShares = stock.AvailableShares
        });
    }

    [HttpPost("sell")]
    public async Task<IActionResult> Sell(TradeDto dto)
    {
        if (dto.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0." });

        var user = await _db.Users.FindAsync(UserId);
        var stock = await _db.Stocks.FindAsync(dto.StockId);
        var holding = await _db.Holdings
            .FirstOrDefaultAsync(h => h.UserId == UserId && h.StockId == dto.StockId);

        if (user == null || stock == null)
            return BadRequest(new { message = "Invalid user or stock." });

        if (holding == null || holding.Quantity < dto.Quantity)
            return BadRequest(new
            {
                message = holding == null
                    ? $"You don't own any shares of {stock.Symbol}."
                    : $"You only hold {holding.Quantity} share(s) of {stock.Symbol}."
            });

        var totalValue = stock.CurrentPrice * dto.Quantity;

        // ── Credit wallet ─────────────────────────────────────────
        user.WalletBalance += totalValue;

        // ── Restore available shares ──────────────────────────────
        stock.AvailableShares += dto.Quantity;
        stock.LastUpdated = DateTime.UtcNow;

        // ── Update holding ────────────────────────────────────────
        holding.Quantity -= dto.Quantity;
        if (holding.Quantity == 0)
            _db.Holdings.Remove(holding);

        // ── Record transaction ────────────────────────────────────
        _db.Transactions.Add(new Transaction
        {
            UserId = UserId,
            StockId = dto.StockId,
            Type = "SELL",
            Quantity = dto.Quantity,
            PricePerShare = stock.CurrentPrice,
            TotalAmount = totalValue
        });

        await _db.SaveChangesAsync();

        // Notify market engine so price reacts to this sell
        _market.RecordTrade(dto.StockId, "SELL", dto.Quantity, stock.TotalShares);

        return Ok(new
        {
            message = $"Sold {dto.Quantity} share(s) of {stock.Symbol} at ₹{stock.CurrentPrice:N2}.",
            newBalance = user.WalletBalance,
            availableShares = stock.AvailableShares
        });
    }
}