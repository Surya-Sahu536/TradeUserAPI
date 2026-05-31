using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeUserAPI.Data;
using TradeUserAPI.Models;

namespace UserAPI.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly TradeDbContext _db;
    public WalletController(TradeDbContext db) => _db = db;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetWallet()
    {
        var user = await _db.Users
            .Include(u => u.Transactions)
            .FirstOrDefaultAsync(u => u.Id == UserId);

        if (user == null) return Unauthorized();

        var totalDeposited = _db.WalletTransactions
            .Where(w => w.UserId == UserId && w.Type == "DEPOSIT")
            .Sum(w => (decimal?)w.Amount) ?? 0;

        var totalWithdrawn = _db.WalletTransactions
            .Where(w => w.UserId == UserId && w.Type == "WITHDRAW")
            .Sum(w => (decimal?)w.Amount) ?? 0;

        var history = await _db.WalletTransactions
            .Where(w => w.UserId == UserId)
            .OrderByDescending(w => w.CreatedAt)
            .Take(20)
            .ToListAsync();

        return Ok(new
        {
            user.FullName,
            user.Email,
            user.WalletBalance,
            totalDeposited,
            totalWithdrawn,
            history
        });
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] WalletRequestDto dto)
    {
        if (dto.Amount <= 0 || dto.Amount > 1000000)
            return BadRequest(new { message = "Amount must be between ₹1 and ₹10,00,000." });

        var user = await _db.Users.FindAsync(UserId);
        if (user == null) return Unauthorized();

        user.WalletBalance += dto.Amount;

        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = UserId,
            Type = "DEPOSIT",
            Amount = dto.Amount,
            Note = dto.Note ?? "Manual deposit",
            BalanceAfter = user.WalletBalance
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = $"₹{dto.Amount:N2} added to wallet.", newBalance = user.WalletBalance });
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WalletRequestDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than ₹0." });

        var user = await _db.Users.FindAsync(UserId);
        if (user == null) return Unauthorized();

        if (user.WalletBalance < dto.Amount)
            return BadRequest(new { message = $"Insufficient balance. Available: ₹{user.WalletBalance:N2}." });

        user.WalletBalance -= dto.Amount;

        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = UserId,
            Type = "WITHDRAW",
            Amount = dto.Amount,
            Note = dto.Note ?? "Manual withdrawal",
            BalanceAfter = user.WalletBalance
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = $"₹{dto.Amount:N2} withdrawn from wallet.", newBalance = user.WalletBalance });
    }
}

public record WalletRequestDto(decimal Amount, string? Note);
