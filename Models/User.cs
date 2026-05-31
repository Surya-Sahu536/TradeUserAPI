namespace TradeUserAPI.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public decimal WalletBalance { get; set; } = 10000m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public class Stock
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal DayHigh { get; set; }
    public decimal DayLow { get; set; }
    public decimal PreviousClose { get; set; }
    public long Volume { get; set; }

    // ── Share inventory ──────────────────────────────────────────
    // TotalShares: fixed number of shares issued (set by admin)
    // AvailableShares: decreases when users buy, increases when they sell
    public long TotalShares { get; set; } = 1_000_000;
    public long AvailableShares { get; set; } = 1_000_000;

    public bool IsActive { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public ICollection<PriceHistory> PriceHistory { get; set; } = new List<PriceHistory>();
}

public class PriceHistory
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public Stock Stock { get; set; } = null!;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Holding
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int StockId { get; set; }
    public Stock Stock { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal AverageBuyPrice { get; set; }
}

public class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int StockId { get; set; }
    public Stock Stock { get; set; } = null!;
    public string Type { get; set; } = string.Empty; // BUY | SELL
    public int Quantity { get; set; }
    public decimal PricePerShare { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class WalletTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Type { get; set; } = string.Empty; // DEPOSIT | WITHDRAW
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── DTOs ─────────────────────────────────────────────────────────
public record RegisterDto(string FullName, string Email, string Password);
public record LoginDto(string Email, string Password);
public record TradeDto(int StockId, int Quantity);
public record WalletRequestDto(decimal Amount, string? Note);
public record UpdateProfileDto(string FullName);
public record ChangePasswordDto(string CurrentPassword, string NewPassword);

public record StockDto(
    int Id, string Symbol, string CompanyName, string Sector,
    decimal CurrentPrice, decimal OpenPrice, decimal DayHigh, decimal DayLow,
    decimal PreviousClose, long Volume,
    long TotalShares, long AvailableShares,   // ← included in API response
    decimal ChangePercent, DateTime LastUpdated);

public record HoldingDto(int StockId, string Symbol, string CompanyName,
    int Quantity, decimal AverageBuyPrice, decimal CurrentPrice,
    decimal CurrentValue, decimal ProfitLoss, decimal ProfitLossPercent);

public record TransactionDto(int Id, string Symbol, string CompanyName,
    string Type, int Quantity, decimal PricePerShare, decimal TotalAmount, DateTime CreatedAt);

public record PortfolioDto(decimal WalletBalance, decimal TotalInvested,
    decimal CurrentValue, decimal TotalPnL, decimal TotalPnLPercent,
    IEnumerable<HoldingDto> Holdings);
