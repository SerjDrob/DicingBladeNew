using Microsoft.Extensions.Logging;

namespace DicingBlade.ViewModels
{
    public interface IMainViewModel
    {
        void LogMessage(LogLevel loggerLevel, string message);
    }
}