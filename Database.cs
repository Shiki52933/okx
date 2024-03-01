using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace okx;

public class CandlesDbContext : DbContext
{
    private static readonly string? connectionString;
    static CandlesDbContext()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        IConfigurationRoot configuration = builder.Build();
        connectionString = configuration["database:sqlserver"];
    }
    public DbSet<Candle> Candles { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        // 连接到sqlserver
        => options.UseSqlServer(connectionString);
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Candle>()
            .ToTable("Candles")
            .HasKey(c => new { c.InstId, c.Timestamp, c.Period });
    }
}

public class DbManager
{
    public static void SaveCandles(List<Candle> candles)
    {
        if (candles.Count == 0)
        {
            return;
        }

        // 检查：candles中的InstId是否一致、bar是否一致
        var instId = candles[0].InstId;
        var bar = candles[0].Period;
        foreach (var candle in candles)
        {
            if (candle.InstId != instId || candle.Period != bar)
            {
                throw new ArgumentException("InstId or bar not match");
            }
        }

        // 如果candles中存在空缺，抛出异常
        candles.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        for (var i = 1; i < candles.Count; i++)
        {
            if (candles[i].Timestamp - candles[i - 1].Timestamp != Client.Bar2ms(bar))
            {
                Console.WriteLine(candles[i].ToString());
                Console.WriteLine(candles[i - 1].ToString());
                throw new ArgumentException("candles not continuous");
            }
        }

        // 保存到数据库，如果已存在则更新
        // 如果数据库中已存在相同的InstId、Timestamp、Period的数据，并且done为true，则不更新
        // 从数据库读取数据，然后在内存中完成，最后更新到数据库
        using (var db = new CandlesDbContext())
        {
            db.Database.EnsureCreated();
            var dbCandles = db.Candles.Where(c => c.InstId == instId && c.Period == bar).ToList();
            foreach (var candle in candles)
            {
                var dbCandle = dbCandles.Find(c => c.Timestamp == candle.Timestamp);
                if (dbCandle == null)
                {
                    db.Candles.Add(candle);
                }
                else if (!dbCandle.Done)
                {
                    db.Entry(dbCandle).CurrentValues.SetValues(candle);
                }
            }
            db.SaveChanges();
        }
    }
    public static List<Candle> ReadCandles(
        string instId,
        DateTime after,
        DateTime before,
        string bar)
    {
        using (var db = new CandlesDbContext())
        {
            var afterMs = ((DateTimeOffset)after).ToUnixTimeMilliseconds();
            var beforeMs = ((DateTimeOffset)before).ToUnixTimeMilliseconds();
            var candles = db.Candles
                .Where(c => c.InstId == instId && c.Period == bar && c.Timestamp < afterMs && c.Timestamp >= beforeMs)
                .OrderBy(c => c.Timestamp)
                .ToList();
            return candles;
        }
    }

    public static List<Candle> ReadCandles(
        string instId,
        string after,
        string before,
        string bar)
    {
        using (var db = new CandlesDbContext())
        {
            var afterMs = long.Parse(after);
            var beforeMs = long.Parse(before);
            var candles = db.Candles
                .Where(c => c.InstId == instId && c.Period == bar && c.Timestamp < afterMs && c.Timestamp >= beforeMs)
                .OrderBy(c => c.Timestamp)
                .ToList();
            return candles;
        }
    }

    /// 调用者保证after和before的时间间隔是bar的整数倍
    public static bool ExistCandles(
        string instId,
        string after,
        string before,
        string bar)
    {
        // 如果这个时间段内的每一条数据都存在且done为true，则返回true
        var afterMs = long.Parse(after);
        var beforeMs = long.Parse(before);
        var counts = (afterMs - beforeMs) / Client.Bar2ms(bar);
        using (var db = new CandlesDbContext())
        {
            return db.Candles.Where(c => c.InstId == instId &&
                                    c.Period == bar &&
                                    c.Timestamp < afterMs &&
                                    c.Timestamp >= beforeMs &&
                                    c.Done == true)
                            .Count() == counts;

        }
    }

}