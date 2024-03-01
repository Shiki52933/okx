using System.Text;

namespace okx;


public class Candle
{
    public string? InstId { get; set; }
    public long Timestamp { get; set; } // unit: ms
    public DateTime ReadableTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).DateTime;
    public string? Period { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public bool Done { get; set; }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("timestamp: ").Append(Timestamp).Append("\t");
        sb.Append("readableTime: ").Append(ReadableTime).Append("\t");
        sb.Append("open: ").Append(Open).Append("\t");
        sb.Append("high: ").Append(High).Append("\t");
        sb.Append("low: ").Append(Low).Append("\t");
        sb.Append("close: ").Append(Close).Append("\t");
        sb.Append("done: ").Append(Done).Append("\t");
        return sb.ToString();
    }
}

public class KLineResponse
{
    /// <summary>
    /// 来自http返回的k线数据，由于解析json，这里不符合c#命名规范
    /// </summary>
    public string? instId { get; set; }
    public string? bar { get; set; }
    public string? code { get; set; }
    public string? msg { get; set; }
    public string[][]? data { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("code: ").Append(code).Append("\n");
        sb.Append("msg: ").Append(msg).Append("\n");
        sb.Append("data: [").Append("\n");
        if (code == "0")
        {
            foreach (var item in data)
            {
                sb.Append("[\n");
                sb.Append("timestamp: ").Append(item[0]).Append("\n");
                sb.Append("open: ").Append(item[1]).Append("\n");
                sb.Append("high: ").Append(item[2]).Append("\n");
                sb.Append("low: ").Append(item[3]).Append("\n");
                sb.Append("close: ").Append(item[4]).Append("\n");
                sb.Append("done: ").Append(item[5]).Append("\n");
                sb.Append("]\n");
            }
        }
        sb.Append("]\n");
        return sb.ToString();
    }
    public List<Candle> ToCandles()
    {
        var candles = new List<Candle>();
        if (code == "0")
        {
            foreach (var item in data)
            {
                var candle = new Candle
                {
                    InstId = this.instId,
                    Timestamp = long.Parse(item[0]),
                    Period = this.bar,
                    Open = decimal.Parse(item[1]),
                    High = decimal.Parse(item[2]),
                    Low = decimal.Parse(item[3]),
                    Close = decimal.Parse(item[4]),
                    Done = item[5]=="1"
                };
                candles.Add(candle);
            }
        }
        return candles;
    }

}

