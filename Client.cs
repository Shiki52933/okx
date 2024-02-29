using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace okx;


public class Client
{
    public Client(string apiKey, string apiSecret, string passphrase)
    {
        this.ApiKey = apiKey;
        this.ApiSecret = apiSecret;
        this.Passphrase = passphrase;
    }

    public void requestIndexkLine(
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
        addParam(request, param);
        addAuthenticationHeader(request);
        
        var response = sendRequest(request).Result;
        var content = response.Content.ReadAsStringAsync().Result;
        Console.WriteLine(content);
    }

    public Task<HttpResponseMessage> sendRequest(HttpRequestMessage request, bool byProxy = true)
    {
        HttpClient? httpClient = null;
        if (byProxy)
        {
            var proxy = new WebProxy("http://127.0.0.1:7890"); // 这里的地址和端口应该替换为你的 Clash for Windows 的代理服务器的地址和端口
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

    public void addParam(HttpRequestMessage request, Dictionary<string, string> param)
    {
        var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
        foreach (var item in param)
        {
            if (!string.IsNullOrEmpty(item.Value))
                query[item.Key] = item.Value;
        }
        request.RequestUri = new Uri(request.RequestUri.AbsoluteUri.Split('?')[0] + "?" + query);
    }

    public void addAuthenticationHeader(HttpRequestMessage request)
    {
        request.Headers.Add("OK-ACCESS-KEY", this.ApiKey);

        // HMAC SHA256方法加密，通过Base-64编码
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var beforeHash = timestamp + request.Method + request.RequestUri.PathAndQuery + request.Content;
        var hash = Convert.ToBase64String(new HMACSHA256(Encoding.UTF8.GetBytes(this.ApiSecret)).ComputeHash(Encoding.UTF8.GetBytes(beforeHash)));
        request.Headers.Add("OK-ACCESS-SIGN", hash);

        request.Headers.Add("OK-ACCESS-TIMESTAMP", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        request.Headers.Add("OK-ACCESS-PASSPHRASE", this.Passphrase);
        // request.Headers.Add("Content-Type", "application/json");
        request.Headers.Add("'x-simulated-trading'", "0");
    }

    const string prefix = "https://www.okx.com";
    public string ApiKey { get; }
    public string ApiSecret { get; }
    public string Passphrase { get; }
}