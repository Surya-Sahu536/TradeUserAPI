using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeUserAPI.Data;

namespace UserAPI.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly TradeDbContext _db;
    public ProfileController(TradeDbContext db) => _db = db;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _db.Users.FindAsync(UserId);
        if (user == null) return Unauthorized();

        var totalTrades    = await _db.Transactions.CountAsync(t => t.UserId == UserId);
        var totalBuys      = await _db.Transactions.CountAsync(t => t.UserId == UserId && t.Type == "BUY");
        var totalSells     = await _db.Transactions.CountAsync(t => t.UserId == UserId && t.Type == "SELL");
        var holdingsCount  = await _db.Holdings.CountAsync(h => h.UserId == UserId);
        var memberSince    = user.CreatedAt;

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.WalletBalance,
            memberSince,
            stats = new { totalTrades, totalBuys, totalSells, holdingsCount }
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _db.Users.FindAsync(UserId);
        if (user == null) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName;

        await _db.SaveChangesAsync();
        return Ok(new { user.FullName, user.Email, message = "Profile updated." });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var user = await _db.Users.FindAsync(UserId);
        if (user == null) return Unauthorized();

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        if (dto.NewPassword.Length < 6)
            return BadRequest(new { message = "New password must be at least 6 characters." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password changed successfully." });
    }
}

public record UpdateProfileDto(string FullName);
public record ChangePasswordDto(string CurrentPassword, string NewPassword);
