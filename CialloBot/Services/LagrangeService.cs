using CialloBot.Models;
using CialloBot.Utils;

using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Text;

using BotEventArg = Lagrange.Core.Event.EventArg;
using BotLogLevel = Lagrange.Core.Event.EventArg.LogLevel;

namespace CialloBot.Services;

public class LagrangeService : IDisposable
{
    private readonly ILogger<LagrangeService> logger;
    private readonly ILogger<BotContext> botLogger;
    private readonly ILagrangePersistentService persistentService;
    private readonly IOptions<LagrangeSettingModel> setting;
    private readonly BotConfig botConfig = new BotConfig()
    {
        AutoReconnect = true,
        GetOptimumServer = true
    };
    private BotContext? botContext;

    public LagrangeService(ILogger<LagrangeService> logger,
        ILogger<BotContext> botLogger,
        ILagrangePersistentService persistentService,
        IOptions<LagrangeSettingModel> setting)
    {
        this.logger = logger;
        this.botLogger = botLogger;
        this.persistentService = persistentService;
        this.setting = setting;
    }

    public BotContext BotContext => botContext!;

    public void Dispose()
    {
        botContext?.Dispose();
    }

    public async Task<bool> Login()
    {
        if (this.botContext != null)
            return false;

        (var device, var keystore) = persistentService.ReadSaved();
        if (device is null || keystore is null)
        {
            device ??= BotDeviceInfo.GenerateInfo();
            keystore = new BotKeystore() { Uin = setting.Value.Uin };
            if (setting.Value.Password is not null)
                keystore.PasswordMd5 = Encoding.ASCII.GetBytes(setting.Value.Password).Md5();
            else
                logger.LogWarning("Password is null");
        }
        botContext = BotFactory.Create(botConfig, device, keystore);

        botContext.Invoker.OnBotLogEvent += OnBotLog;
        botContext.Invoker.OnBotCaptchaEvent += OnBotCaptcha;

        if (await botContext.LoginByPassword())
        {
            persistentService.Save(botContext.UpdateDeviceInfo(), botContext.UpdateKeystore());
            logger.LogInformation("Login with password succeed");
            return true;
        }
        logger.LogWarning("Login with password failed, try QrCode");
        var qrCode = await botContext.FetchQrCode();
        if (!qrCode.HasValue)
        {
            logger.LogError("Login failed, reason: qrCode fetch failed");
            return false;
        }

        var codePath = "loginQrCode.png";
        logger.LogInformation($"Login qrcode url is {qrCode.Value.Url}");
        if (!SaveImage(qrCode.Value.QrCode, codePath))
            logger.LogError($"Couldn't save qrcode image to {codePath}");
        else
            logger.LogInformation($"The qrCode have been saved to {codePath}");

        logger.LogInformation("Waiting login response ...");
        await botContext.LoginByQrCode();
        logger.LogInformation("Login succeed");

        persistentService.Save(botContext.UpdateDeviceInfo(), botContext.UpdateKeystore());
        return true;
    }

    private bool SaveImage(byte[] imageData, string path)
    {
        if (File.Exists(path))
            File.Delete(path);
        try
        {
            var fs = File.Create(path);
            fs.Write(imageData);
            return true;
        }
        catch { }
        return false;
    }

    private void OnBotLog(BotContext _, BotEventArg.BotLogEvent @event)
    {
        botLogger.Log(@event.Level switch
        {
            BotLogLevel.Debug => LogLevel.Trace,
            BotLogLevel.Verbose => LogLevel.Debug,
            BotLogLevel.Information => LogLevel.Information,
            BotLogLevel.Warning => LogLevel.Warning,
            BotLogLevel.Fatal => LogLevel.Error,
            _ => LogLevel.Error
        }, @event.ToString());
    }

    private void OnBotCaptcha(BotContext bot, BotEventArg.BotCaptchaEvent @event)
    {
        logger.LogWarning($"Captcha: {@event.Url}");
        logger.LogWarning("Please input ticket and randomString:");

        var ticket = Console.ReadLine();
        var randomString = Console.ReadLine();

        if (ticket != null && randomString != null)
            bot.SubmitCaptcha(ticket, randomString);
    }
}
