using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TradeUserAPI.Data;
using TradeUserAPI.Hubs;

namespace TradeUserAPI.Services;

public class PriceSimulatorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<PriceHub> _hub;
    private readonly MarketEngine _market;

    private readonly Dictionary<int, double> _volatility = new();
    private const double DefaultVolatility = 1.5;

    public PriceSimulatorService(
        IServiceScopeFactory scopeFactory,
        IHubContext<PriceHub> hub,
        MarketEngine market)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _market = market;
    }

    public void SetVolatility(int stockId, double value) =>
        _volatility[stockId] = Math.Clamp(value, 0.1, 10.0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SimulatePrices(); }
            catch (Exception ex) { Console.WriteLine($"[PriceSimulator] {ex.Message}"); }
            await Task.Delay(3000, stoppingToken);
        }
    }

    private async Task SimulatePrices()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
        var stocks = await db.Stocks.Where(s => s.IsActive).ToListAsync();

        foreach (var stock in stocks)
        {
            var vol = _volatility.TryGetValue(stock.Id, out var v) ? v : DefaultVolatility;
            var changeFraction = _market.CalculatePriceChange(
                stock.Id, vol, stock.AvailableShares, stock.TotalShares);

            var newPrice = Math.Round(stock.CurrentPrice * (1 + (decimal)changeFraction), 2);
            newPrice = Math.Max(newPrice, stock.OpenPrice * 0.85m);
            newPrice = Math.Min(newPrice, stock.OpenPrice * 1.15m);
            newPrice = Math.Max(newPrice, 1m);

            stock.PreviousClose = stock.CurrentPrice;
            stock.CurrentPrice = newPrice;
            stock.DayHigh = Math.Max(stock.DayHigh, newPrice);
            stock.DayLow = Math.Min(stock.DayLow, newPrice);
            stock.LastUpdated = DateTime.UtcNow;

            db.PriceHistory.Add(new() { StockId = stock.Id, Price = newPrice });

            var dayChange = stock.OpenPrice == 0 ? 0 :
                Math.Round(((newPrice - stock.OpenPrice) / stock.OpenPrice) * 100, 2);

            await _hub.Clients.All.SendAsync("ReceivePriceUpdate",
                stock.Id, newPrice, dayChange, stock.AvailableShares);
        }

        await db.SaveChangesAsync();

        var old = db.PriceHistory
            .GroupBy(p => p.StockId)
            .SelectMany(g => g.OrderByDescending(p => p.Timestamp).Skip(100));
        db.PriceHistory.RemoveRange(old);
        await db.SaveChangesAsync();
    }
}
