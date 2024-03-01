using System.Text;
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
// using(var db = new okx.CandlesDbContext())
// {
//     var candles = db.Candles.Where(c => c.InstId == "BTC-USD").ToList();
//     Console.WriteLine("read from database:");
//     foreach (var candle in candles)
//     {
//         Console.WriteLine(candle.ToString());
//     }
// }

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
    bar: "1H");
Helper.PrintAtCenter("test GetKLineAsync");
// foreach (var candle in candles1)
// {
//     Console.WriteLine(candle.ToString());
// }
Helper.PrintAtCenter($"test GetKLineAsync, count: {candles1.Count}");

Helper.PrintAtCenter("test database manager");
DbManager.SaveCandles(candles1);
DbManager.SaveCandles(candles1);
Helper.PrintAtCenter("SaveCandles done");
candles1 = DbManager.ReadCandles("ETH-USD", new DateTime(2024, 1, 1, 3, 0, 0), new DateTime(2023, 12, 31, 23, 0, 0), "1m");
Helper.PrintAtCenter($"ReadCandles done, count: {candles1.Count}");


// 下载btc和eth为期一年的数据
Helper.PrintAtCenter("Download data");
var end = new DateTime(2024, 3, 1);
var start = new DateTime(2023, 1, 1);
var bar = "15m";

foreach(var instId in new string[]{"BTC-USD", "ETH-USD"})
{
    var before = start;
    var after = start;
    var count = 0;
    while (after < end)
    {
        before = after;
        after = after.AddMonths(1);
        var candles = await client.GetKLineAsync(
            instId,
            after: okx.Client.Time2timestamp(after),
            before: okx.Client.Time2timestamp(before),
            bar: bar);
        DbManager.SaveCandles(candles);
        count += candles.Count;
    }
    Console.WriteLine($"download {instId} done, count: {count}");
}

// 读取数据库中btc和eth的15m数据
// 保存为csv格式，放在./python/目录下，供python程序使用
Helper.PrintAtCenter("Save to csv");
foreach(var instId in new string[]{"BTC-USD", "ETH-USD"})
{
    var candles = DbManager.ReadCandles(instId, end, start, bar);
    var csv = new StringBuilder();
    csv.AppendLine("datetime,open,high,low,close,done");
    foreach(var candle in candles)
    {
        csv.AppendLine($"{candle.ReadableTime},{candle.Open},{candle.High},{candle.Low},{candle.Close},{candle.Done}");
    }
    File.WriteAllText($"./python/{instId}_{bar}.csv", csv.ToString());
    Console.WriteLine($"save {instId} done");
}