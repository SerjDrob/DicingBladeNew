using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DicingBlade;
internal static class AppPaths
{
    private static readonly string _directoryPrefix;
    private static string SettingsFolder => Path.Combine(_directoryPrefix, "AppSettings");

    static AppPaths()
    {
        var workingDirectory = Environment.CurrentDirectory;
#if DEBUGGING
            _directoryPrefix = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
#else
        _directoryPrefix = string.Empty;
#endif
    }

    public static string TempFolder => Path.Combine(_directoryPrefix, "TempFiles");
    public static string TechnologyFolder => Path.Combine(_directoryPrefix, "TechnologyFiles");


    public static string AxesConfigs => Path.Combine(SettingsFolder, "AxesConfigs.json");
    public static string DefaultLaserParams => Path.Combine(SettingsFolder, "DefaultLaserParams.json");
    public static string DefaultProcessFilter => Path.Combine(SettingsFolder, "DefaultProcessFilter.json");
    public static string MachineConfigs => Path.Combine(SettingsFolder, "MachineConfigs.json");
    public static string PureDeformation => Path.Combine(SettingsFolder, "PureDeformation.json");
    public static string TeachingDeformation => Path.Combine(SettingsFolder, "TeachingDeformation.json");
    public static string Applog => Path.Combine(TempFolder, "app.log");
}
