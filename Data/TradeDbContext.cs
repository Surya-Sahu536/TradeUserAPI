using Microsoft.EntityFrameworkCore;
using TradeUserAPI.Models;
using TradeUserAPI.Models;

namespace TradeUserAPI.Data;

public class TradeDbContext : DbContext
{
    public TradeDbContext(DbContextOptions<TradeDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<PriceHistory> PriceHistory { get; set; }
    public DbSet<Holding> Holdings { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Stock>().HasIndex(s => s.Symbol).IsUnique();
        mb.Entity<User>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<Holding>().HasIndex(h => new { h.UserId, h.StockId }).IsUnique();

        mb.Entity<Stock>().Property(s => s.CurrentPrice).HasColumnType("decimal(18,4)");
        mb.Entity<Stock>().Property(s => s.OpenPrice).HasColumnType("decimal(18,4)");
        mb.Entity<Stock>().Property(s => s.DayHigh).HasColumnType("decimal(18,4)");
        mb.Entity<Stock>().Property(s => s.DayLow).HasColumnType("decimal(18,4)");
        mb.Entity<Stock>().Property(s => s.PreviousClose).HasColumnType("decimal(18,4)");
        mb.Entity<User>().Property(u => u.WalletBalance).HasColumnType("decimal(18,2)");
        mb.Entity<Holding>().Property(h => h.AverageBuyPrice).HasColumnType("decimal(18,4)");
        mb.Entity<Transaction>().Property(t => t.PricePerShare).HasColumnType("decimal(18,4)");
        mb.Entity<Transaction>().Property(t => t.TotalAmount).HasColumnType("decimal(18,2)");
        mb.Entity<PriceHistory>().Property(p => p.Price).HasColumnType("decimal(18,4)");
        mb.Entity<WalletTransaction>().Property(w => w.Amount).HasColumnType("decimal(18,2)");
        mb.Entity<WalletTransaction>().Property(w => w.BalanceAfter).HasColumnType("decimal(18,2)");
    }
}

public static class DbSeeder
{
    public static void Seed(TradeDbContext db)
    {
        try { if (db.Stocks.Any()) return; }
        catch { return; }

        var stocks = new List<Stock>
        {
            new() { Symbol="RELIANCE",   CompanyName="Reliance Industries Ltd",      Sector="Energy",         CurrentPrice=2850.00m, OpenPrice=2830m, DayHigh=2870m, DayLow=2810m, PreviousClose=2820m, Volume=1200000, TotalShares=500000, AvailableShares=500000 },
            new() { Symbol="TCS",        CompanyName="Tata Consultancy Services",    Sector="IT",             CurrentPrice=3920.50m, OpenPrice=3900m, DayHigh=3945m, DayLow=3880m, PreviousClose=3905m, Volume=850000,  TotalShares=300000, AvailableShares=300000 },
            new() { Symbol="INFY",       CompanyName="Infosys Ltd",                  Sector="IT",             CurrentPrice=1452.75m, OpenPrice=1440m, DayHigh=1465m, DayLow=1430m, PreviousClose=1445m, Volume=1100000, TotalShares=800000, AvailableShares=800000 },
            new() { Symbol="HDFCBANK",   CompanyName="HDFC Bank Ltd",                Sector="Banking",        CurrentPrice=1680.00m, OpenPrice=1670m, DayHigh=1695m, DayLow=1660m, PreviousClose=1672m, Volume=2000000, TotalShares=600000, AvailableShares=600000 },
            new() { Symbol="WIPRO",      CompanyName="Wipro Ltd",                    Sector="IT",             CurrentPrice=478.30m,  OpenPrice=475m,  DayHigh=482m,  DayLow=470m,  PreviousClose=472m,  Volume=950000,  TotalShares=1000000,AvailableShares=1000000},
            new() { Symbol="ICICIBANK",  CompanyName="ICICI Bank Ltd",               Sector="Banking",        CurrentPrice=1120.00m, OpenPrice=1110m, DayHigh=1130m, DayLow=1100m, PreviousClose=1115m, Volume=1800000, TotalShares=700000, AvailableShares=700000 },
            new() { Symbol="TATAMOTORS", CompanyName="Tata Motors Ltd",              Sector="Automotive",     CurrentPrice=952.40m,  OpenPrice=945m,  DayHigh=960m,  DayLow=938m,  PreviousClose=948m,  Volume=3200000, TotalShares=900000, AvailableShares=900000 },
            new() { Symbol="SBIN",       CompanyName="State Bank of India",          Sector="Banking",        CurrentPrice=748.90m,  OpenPrice=742m,  DayHigh=755m,  DayLow=738m,  PreviousClose=745m,  Volume=4500000, TotalShares=2000000,AvailableShares=2000000},
            new() { Symbol="BAJFINANCE", CompanyName="Bajaj Finance Ltd",            Sector="Finance",        CurrentPrice=6820.00m, OpenPrice=6780m, DayHigh=6870m, DayLow=6740m, PreviousClose=6760m, Volume=620000,  TotalShares=200000, AvailableShares=200000 },
            new() { Symbol="HCLTECH",    CompanyName="HCL Technologies Ltd",         Sector="IT",             CurrentPrice=1340.00m, OpenPrice=1325m, DayHigh=1355m, DayLow=1310m, PreviousClose=1330m, Volume=780000,  TotalShares=400000, AvailableShares=400000 },
        };

        db.Stocks.AddRange(stocks);
        db.SaveChanges();
    }
}
