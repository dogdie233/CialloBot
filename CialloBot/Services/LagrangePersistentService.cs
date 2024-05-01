using CialloBot.Models;
using CialloBot.Utils;

using Lagrange.Core.Common;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Text.Json;

namespace CialloBot.Services;

public interface ILagrangePersistentService
{
    public (BotDeviceInfo? deviceInfo, BotKeystore? keystore) ReadSaved();
    public void ClearSaved();
    public void Save(BotDeviceInfo? info, BotKeystore? keystore);
}

public class LagrangePersistentService(IOptions<LagrangeSettingModel> option, ILogger<LagrangePersistentService> logger) : ILagrangePersistentService
{
    private bool validated = false;
    private bool valid = false;
    private static readonly JsonSerializerOptions serializerOptions;

    static LagrangePersistentService()
    {
        serializerOptions = new();
        serializerOptions.Converters.Add(new ByteArrayJsonConverter());
    }

    private void ValidatePath()
    {
        var defaultSetting = new LagrangeSettingModel();
        if (option.Value.DeviceInfoPath is null)
        {
            option.Value.DeviceInfoPath = defaultSetting.DeviceInfoPath;
            logger.LogError($"{nameof(LagrangeSettingModel.DeviceInfoPath)} couldn't be null, reset to {option.Value.DeviceInfoPath}");
        }
        if (option.Value.KeystorePath is null)
        {
            option.Value.KeystorePath = defaultSetting.KeystorePath;
            logger.LogError($"{nameof(LagrangeSettingModel.KeystorePath)} couldn't be null, reset to {option.Value.KeystorePath}");
        }

        valid = true;
        if (!Check(option.Value.DeviceInfoPath))
        {
            option.Value.DeviceInfoPath = defaultSetting.DeviceInfoPath;
            if (!Check(option.Value.DeviceInfoPath))
                valid = false;
        }
        if (!Check(option.Value.KeystorePath))
        {
            option.Value.KeystorePath = defaultSetting.KeystorePath;
            if (!Check(option.Value.KeystorePath))
                valid = false;
        }
        if (!valid)
            logger.LogWarning("Persistent Data path is incorrect, login cache ability have been disabled.");

        bool Check(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                var exist = fileInfo.Exists;
                using (var file = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                }

                if (!exist)
                    fileInfo.Delete();

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Have no read+write permission for file {path}");
            }
            return false;
        }
    }

    private void EnsureValidate()
    {
        if (validated)
            return;
        
        ValidatePath();
        validated = true;
    }

    public void ClearSaved()
    {
        EnsureValidate();
        if (!valid)
            return;
        if (File.Exists(option.Value.DeviceInfoPath))
            File.Delete(option.Value.DeviceInfoPath);
        if (File.Exists(option.Value.KeystorePath))
            File.Delete(option.Value.KeystorePath);
    }

    public (BotDeviceInfo? deviceInfo, BotKeystore? keystore) ReadSaved()
    {
        EnsureValidate();
        (BotDeviceInfo? deviceInfo, BotKeystore? keystore) result = default;
        if (!valid)
            return result;

        if (File.Exists(option.Value.DeviceInfoPath))
        {
            using (var deviceStream = File.OpenRead(option.Value.DeviceInfoPath))
                result.deviceInfo = JsonSerializer.Deserialize<BotDeviceInfo>(deviceStream, serializerOptions);
        }
        if (File.Exists(option.Value.KeystorePath))
        {
            using (var keystoreStream = File.OpenRead(option.Value.KeystorePath))
                result.keystore = JsonSerializer.Deserialize<BotKeystore>(keystoreStream, serializerOptions);
        }
        return result;
    }

    public void Save(BotDeviceInfo? info, BotKeystore? keystore)
    {
        EnsureValidate();
        if (!valid)
            return;
        Save(option.Value.DeviceInfoPath, info);
        Save(option.Value.KeystorePath, keystore);

        static void Save<T>(string path, T obj)
        {
            if (obj is null)
                return;

            using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
            fs.SetLength(0);
            fs.Seek(0, SeekOrigin.Begin);
            JsonSerializer.Serialize(fs, obj, serializerOptions);
        }
    }
}