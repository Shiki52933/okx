using Microsoft.Extensions.Configuration;


var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

IConfigurationRoot configuration = builder.Build();

var apiKey = configuration["okx:api_key"];
var apiSecretKey = configuration["okx:secret_key"];
var passphrase = configuration["okx:passphrase"];
var flag = configuration["okx:flag"];
var proxy = configuration["network:proxy"];

var client = new okx.Client(apiKey!, apiSecretKey!, passphrase!, proxy!, flag!);
var response = await client.RequestIndexKLineAsync(
    "BTC-USD", 
    limit: 10
    );
Console.WriteLine("read from http response:");
foreach (var candle in response!.ToCandles())
{
    Console.WriteLine(candle.ToString());
}
using(var db = new okx.CandlesDbContext())
{
    db.Database.EnsureCreated();
    db.Candles.AddRange(response.ToCandles());
    db.SaveChanges();
}
// 尝试从数据库中读取数据
using(var db = new okx.CandlesDbContext())
{
    var candles = db.Candles.Where(c => c.InstId == "BTC-USD").ToList();
    Console.WriteLine("read from database:");
    foreach (var candle in candles)
    {
        Console.WriteLine(candle.ToString());
    }
}

response = await client.RequestHistoryIndexKLineAsync(
    "BTC-USD", 
    after: okx.Client.Time2timestamp(new DateTime(2024, 1, 1, 0, 0, 0)),
    limit: 10
    );
foreach (var candle in response!.ToCandles())
{
    Console.WriteLine(candle.ToString());
}
