namespace TradeUserAPI.Services;

/// <summary>
/// Holds live market state — shared across price simulation ticks.
/// Singleton: one instance for the app lifetime.
/// </summary>
public class MarketEngine
{
    private readonly Random _rng = new();

    // ── Global sentiment ──────────────────────────────────────────────────
    // -1.0 = extreme bear, 0 = neutral, +1.0 = extreme bull
    public double GlobalSentiment { get; private set; } = 0.0;
    public string SentimentLabel => GlobalSentiment switch
    {
        > 0.5  => "Strong Bull 🐂",
        > 0.1  => "Bullish 📈",
        < -0.5 => "Strong Bear 🐻",
        < -0.1 => "Bearish 📉",
        _      => "Neutral ➡️"
    };

    // ── Pending market events per stock ───────────────────────────────────
    // stockId → (impactPercent, ticksRemaining)
    // e.g. +15% earnings beat that fades over 10 ticks
    private readonly Dictionary<int, MarketEvent> _events = new();

    // ── Demand pressure per stock ─────────────────────────────────────────
    // Accumulates from real user trades between simulation ticks
    // stockId → net buy pressure (-ve = selling pressure)
    private readonly Dictionary<int, double> _tradePressure = new();
    private readonly object _lock = new();

    // ── Called by TradeController when a real trade happens ───────────────
    public void RecordTrade(int stockId, string type, int quantity, long totalShares)
    {
        lock (_lock)
        {
            if (!_tradePressure.ContainsKey(stockId))
                _tradePressure[stockId] = 0;

            // Impact proportional to quantity vs total supply
            var impact = (double)quantity / Math.Max(totalShares, 1) * 100.0;
            _tradePressure[stockId] += type == "BUY" ? impact : -impact;
        }
    }

    // ── Called by admin to set global sentiment ───────────────────────────
    public void SetSentiment(double value)
    {
        GlobalSentiment = Math.Clamp(value, -1.0, 1.0);
    }

    // ── Called by admin to trigger a stock event ──────────────────────────
    public void TriggerEvent(int stockId, double impactPercent, int durationTicks = 10)
    {
        lock (_lock)
        {
            _events[stockId] = new MarketEvent(impactPercent, durationTicks);
        }
    }

    public void ClearEvent(int stockId)
    {
        lock (_lock) { _events.Remove(stockId); }
    }

    // ── Main calculation: returns % price change for one tick ─────────────
    public double CalculatePriceChange(int stockId, double volatility, long availableShares, long totalShares)
    {
        double change = 0;

        // 1. Base random noise — scaled by volatility
        change += (_rng.NextDouble() * 2.0 - 1.0) * volatility;

        // 2. Global sentiment nudge (bull market lifts all stocks slightly)
        change += GlobalSentiment * 0.1;

        // 3. Demand pressure from real user trades
        lock (_lock)
        {
            if (_tradePressure.TryGetValue(stockId, out var pressure))
            {
                change += pressure * 0.5;           // trade impact
                _tradePressure[stockId] *= 0.3;     // pressure decays each tick
                if (Math.Abs(_tradePressure[stockId]) < 0.001)
                    _tradePressure.Remove(stockId);
            }
        }

        // 4. Scarcity premium — price rises faster when shares are scarce
        if (totalShares > 0)
        {
            var availPct = (double)availableShares / totalShares;
            if (availPct < 0.1)       change += 0.3;   // very scarce → upward pressure
            else if (availPct < 0.25) change += 0.15;
        }

        // 5. Active market event (earnings beat, crash, etc.)
        lock (_lock)
        {
            if (_events.TryGetValue(stockId, out var evt) && evt.TicksRemaining > 0)
            {
                // Apply fraction of event impact per tick — fades linearly
                change += evt.ImpactPercent / evt.TotalTicks;
                evt.TicksRemaining--;
                if (evt.TicksRemaining <= 0) _events.Remove(stockId);
            }
        }

        return change / 100.0; // return as decimal fraction
    }

    public MarketEvent? GetEvent(int stockId) =>
        _events.TryGetValue(stockId, out var e) ? e : null;

    public double GetSentimentValue() => GlobalSentiment;
}

public class MarketEvent
{
    public double ImpactPercent  { get; set; }
    public int    TicksRemaining { get; set; }
    public int    TotalTicks     { get; set; }

    public MarketEvent(double impactPercent, int durationTicks)
    {
        ImpactPercent  = impactPercent;
        TicksRemaining = durationTicks;
        TotalTicks     = durationTicks;
    }
}
