using Microsoft.Extensions.Logging;

namespace CialloBot.Services;

public class CialloService(ILogger<CialloService> logger)
{
    public void Print(string message)
    {
        logger.LogInformation("Ciallo～(∠·ω< )⌒☆ >>> " + message);
    }
}
