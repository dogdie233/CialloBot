namespace CialloBot.Models;

public class LagrangeSettingModel
{
    public uint Uin { get; set; }
    public string? Password { get; set; }
    public string DeviceInfoPath { get; set; } = "device.json";
    public string KeystorePath { get; set; } = "keystore.bin";
    public string? SignerUrl { get; set; } = "https://sign.lagrangecore.org/api/sign/25765";
}
