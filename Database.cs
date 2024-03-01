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