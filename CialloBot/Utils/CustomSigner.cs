using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Lagrange.Core.Utility.Sign;

namespace CialloBot.Utils;

public class CustomSigner : SignProvider
{
    private const string DefaultSignServerUrl = "https://sign.lagrangecore.org/api/sign/30366";
    
    private readonly string _signServer;
    private readonly Timer _timer;

    private readonly HttpClient _client;

    public CustomSigner(string? signServerUrl)
    {
        _signServer = signServerUrl ?? DefaultSignServerUrl;
        _client = new HttpClient();
        
        _timer = new Timer(_ =>
        {
            var reconnect = Available = Test();
            if (reconnect) _timer?.Change(-1, 5000);
        });
    }

    public override byte[]? Sign(string cmd, uint seq, byte[] body, [UnscopedRef] out byte[]? ver, [UnscopedRef] out string? token)
    {
        ver = null;
        token = null;
        if (!WhiteListCommand.Contains(cmd)) return null;
        if (!Available || string.IsNullOrEmpty(_signServer)) return new byte[35]; // Dummy signature
        
        var payload = new JsonObject
        {
            { "cmd", cmd },
            { "seq", seq },
            { "src", body.Hex() },
        };

        try
        {
            var message = _client.PostAsJsonAsync(_signServer, payload).Result;
            string response = message.Content.ReadAsStringAsync().Result;
            var json = JsonSerializer.Deserialize<JsonObject>(response);

            ver = json?["value"]?["extra"]?.ToString().UnHex() ?? Array.Empty<byte>();
            token = Encoding.ASCII.GetString(json?["value"]?["token"]?.ToString().UnHex() ?? Array.Empty<byte>());
            return json?["value"]?["sign"]?.ToString().UnHex() ?? new byte[35];
        }
        catch
        {
            Available = false;
            _timer.Change(0, 5000);
            
            return new byte[35]; // Dummy signature
        }
    }

    public override bool Test()
    {
        try
        {
            var response = _client.GetAsync($"{_signServer}/ping").Result.Content.ReadAsStringAsync().Result;
            if (JsonSerializer.Deserialize<JsonObject>(response)?["code"]?.GetValue<int>() == 0)
            {
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}