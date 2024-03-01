using System.Net;
using System.Configuration;
using System.Text.Json;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace okx;


public partial class Client
{
    public Client(
        string apiKey,
        string apiSecret,
        string passphrase,
        string flag,
        string proxy)
    {
        this.ApiKey = apiKey;
        this.ApiSecret = apiSecret;
        this.Passphrase = passphrase;
        this.Flag = flag;

        this.proxy = proxy;
    }

    /// <summary>
    /// 获取K线数据
    /// 获取的时间范围: [before, after]
    /// </summary>
    public async Task<List<Candle>> GetKLineAsync(
        string instId,
        string after = "",
        string before = "",
        string bar = "1m")
    {
        var requestSpeed = 10; // 10次/2秒
        var delayMs = 2000;
        // 如果before不为空，after为空，需要填充after
        if (!string.IsNullOrEmpty(before) && string.IsNullOrEmpty(after))
        {
            after = Time2timestamp(DateTime.Now);
        }

        List<KLineResponse?> klineResponses = [];

        // 如有必要，分割时间段
        var onceLimit = 100;
        if (!string.IsNullOrEmpty(before) && !string.IsNullOrEmpty(after))
        {
            var ms = Bar2ms(bar);
            var beforeMs = long.Parse(before);
            var afterMs = long.Parse(after);
            var diff = afterMs - beforeMs;

            var count = (diff % ms) > 0 ? diff / ms + 1 : diff / ms;
            var times = (count % onceLimit) > 0 ? count / onceLimit + 1 : count / onceLimit;
            for (var i = 0; i < times; i++)
            {
                var newBefore = (beforeMs + ms * (onceLimit * i - 1)).ToString();
                var newAfter = Math.Min(beforeMs + ms * onceLimit * (i + 1), afterMs).ToString();
                var klineResponse = await RequestHistoryIndexKLineAsync(instId, newAfter, newBefore, bar, onceLimit);
                klineResponses.Add(klineResponse);

                // 控制请求速度
                if (i % requestSpeed == 9)
                {
                    await Task.Delay(delayMs);
                }
            }
        }
        else
        {
            var klineResponse = await RequestIndexKLineAsync(instId, after, before, bar, onceLimit);
            klineResponses.Add(klineResponse);
        }

        return klineResponses.Where(k => k != null)
                             .Select(k => k!.ToCandles())
                             .SelectMany(k => k)
                             .OrderBy(c => c.Timestamp)
                             .Distinct()
                             .ToList();
    }

    public async Task<KLineResponse?> RequestIndexKLineAsync(
        string instId,
        string after = "",
        string before = "",
        string bar = "1m",
        int limit = 100)
    {
        var url = "/api/v5/market/index-candles";
        var param = new Dictionary<string, string>
        {
            {"instId", instId},
            {"after", after},
            {"before", before},
            {"bar", bar},
            {"limit", limit.ToString()}
        };

        var request = new HttpRequestMessage(HttpMethod.Get, prefix + url);
        AddParam(request, param);
        AddAuthenticationHeader(request);

        var response = await SendRequestAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        // 转换为KLineResponse对象
        var klineResponse = JsonSerializer.Deserialize<KLineResponse>(content);
        klineResponse!.instId = instId;
        klineResponse!.bar = bar;
        return klineResponse;
    }

    public async Task<KLineResponse?> RequestHistoryIndexKLineAsync(
        string instId,
        string after = "",
        string before = "",
        string bar = "1m",
        int limit = 100)
    {
        var url = "/api/v5/market/history-index-candles";
        var param = new Dictionary<string, string>
        {
            {"instId", instId},
            {"after", after},
            {"before", before},
            {"bar", bar},
            {"limit", limit.ToString()}
        };

        var request = new HttpRequestMessage(HttpMethod.Get, prefix + url);
        AddParam(request, param);
        AddAuthenticationHeader(request);

        var response = await SendRequestAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var klineResponse = JsonSerializer.Deserialize<KLineResponse>(content);
        klineResponse!.instId = instId;
        klineResponse!.bar = bar;
        return klineResponse;
    }

    public Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, bool byProxy = true)
    {
        HttpClient? httpClient = null;
        if (byProxy)
        {
            var proxy = new WebProxy(this.proxy);
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true,
            };
            httpClient = new HttpClient(httpClientHandler);
        }
        else
        {
            httpClient = new HttpClient();
        }
        return httpClient.SendAsync(request);
    }

    public static void AddParam(HttpRequestMessage request, Dictionary<string, string> param)
    {
        var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
        foreach (var item in param)
        {
            if (!string.IsNullOrEmpty(item.Value))
                query[item.Key] = item.Value;
        }
        request.RequestUri = new Uri(request.RequestUri.AbsoluteUri.Split('?')[0] + "?" + query);
    }

    public void AddAuthenticationHeader(HttpRequestMessage request)
    {
        request.Headers.Add("OK-ACCESS-KEY", this.ApiKey);

        // HMAC SHA256方法加密，通过Base-64编码
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var beforeHash = timestamp + request.Method + request.RequestUri!.PathAndQuery + request.Content;
        var hash = Convert.ToBase64String(
            new HMACSHA256(Encoding.UTF8.GetBytes(this.ApiSecret))
                .ComputeHash(Encoding.UTF8.GetBytes(beforeHash))
            );
        request.Headers.Add("OK-ACCESS-SIGN", hash);

        request.Headers.Add("OK-ACCESS-TIMESTAMP", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        request.Headers.Add("OK-ACCESS-PASSPHRASE", this.Passphrase);
        // request.Headers.Add("Content-Type", "application/json");
        request.Headers.Add("x-simulated-trading", this.Flag);
    }

    public static string Time2timestamp(DateTime time)
    {
        return ((DateTimeOffset)time).ToUnixTimeMilliseconds().ToString();
    }

    public static long Bar2ms(string bar)
    {
        var unit = bar[^1];
        var num = int.Parse(bar[0..^1]);
        return unit switch
        {
            'm' => num * 60 * 1000,
            'h' => num * 60 * 60 * 1000,
            'd' => num * 24 * 60 * 60 * 1000,
            'w' => num * 7 * 24 * 60 * 60 * 1000,
            'M' => num * 30 * 24 * 60 * 60 * 1000,
            _ => throw new ArgumentException("bar unit not supported")
        };
    }

    const string prefix = "https://www.okx.com";
    public string ApiKey { get; }
    public string ApiSecret { get; }
    public string Passphrase { get; }
    public string Flag { get; private set; }

    private string proxy;
}