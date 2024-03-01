using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using okx;


var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

IConfigurationRoot configuration = builder.Build();

var apiKey = configuration["okx:api_key"];
var apiSecretKey = configuration["okx:secret_key"];
var passphrase = configuration["okx:passphrase"];
var flag = configuration["okx:flag"];
var proxy = configuration["network:proxy"];

var client = new okx.Client(apiKey!, apiSecretKey!, passphrase!, flag!, proxy!);
var response = await client.RequestIndexKLineAsync(
    "BTC-USD", 
    limit: 10
    );
Console.WriteLine("read from http response:");
foreach (var candle in response!.ToCandles())
{
    Console.WriteLine(candle.ToString());
}
// using(var db = new okx.CandlesDbContext())
// {
//     db.Database.EnsureCreated();
//     db.Candles.AddRange(response.ToCandles());
//     db.SaveChanges();
// }
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

var candles1 = await client.GetKLineAsync(
    "ETH-USD",
    after: okx.Client.Time2timestamp(new DateTime(2024, 1, 1, 3, 0, 0)),
    before: okx.Client.Time2timestamp(new DateTime(2023, 12, 31, 23, 0, 0)),
    bar: "1m");
Helper.PrintAtCenter("test GetKLineAsync");
foreach (var candle in candles1)
{
    Console.WriteLine(candle.ToString());
}
Helper.PrintAtCenter($"test GetKLineAsync, count: {candles1.Count}");

// candles1中漏了两条，找出来，如果时间戳+60000后的时间戳在candles1中找不到，下一条就是漏掉的
for(int i=0; i<candles1.Count-1; i++)
{
    if (candles1[i].Timestamp + 60000 != candles1[i+1].Timestamp)
    {
        Console.WriteLine(candles1[i].ToString());
        Console.WriteLine(candles1[i+1].ToString());
    }
}


