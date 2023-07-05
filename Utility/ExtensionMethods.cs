using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DicingBlade.Utility
{
    internal static class ExtensionMethods
    {
        //internal static void CopyToSettings(this MachineSettingsViewModel machineSettings)
        //{
        //    Settings.Default.XAcc = machineSettings.XAcc;
        //    //Settings.Default.XLoad
        //    Settings.Default.XVelHigh = machineSettings.XVelHigh;
        //    Settings.Default.XVelLow = machineSettings.XVelLow;
        //    Settings.Default.XVelService = machineSettings.XVelService;
        //    Settings.Default.YAcc = machineSettings.YAcc;
        //    //Settings.Default.YLoad
        //    Settings.Default.YVelHigh = machineSettings.YVelHigh;
        //    Settings.Default.YVelLow = machineSettings.YVelLow;
        //    Settings.Default.YVelService = machineSettings.YVelService;
        //    Settings.Default.ZAcc = machineSettings.ZAcc;
        //    //Settings.Default.ZObjective
        //    Settings.Default.ZVelHigh = machineSettings.ZVelHigh;
        //    Settings.Default.ZVelLow = machineSettings.ZVelLow;
        //    Settings.Default.ZVelService = machineSettings.ZVelService;
        //    Settings.Default.XOffset = machineSettings.XOffset;
        //    Settings.Default.YOffset = machineSettings.YOffset;
        //    Settings.Default.XLoad = machineSettings.XLoad;
        //    Settings.Default.YLoad = machineSettings.YLoad;
        //    Settings.Default.ZeroFocusPoint = machineSettings.ZCamera;
        //    Settings.Default.ZeroPiercePoint = machineSettings.ZLaser;
        //    Settings.Default.XLeftPoint = machineSettings.XLeftPoint;
        //    Settings.Default.YLeftPoint = machineSettings.YLeftPoint;
        //    Settings.Default.XRightPoint = machineSettings.XRightPoint;
        //    Settings.Default.YRightPoint = machineSettings.YRightPoint;
        //}
        //internal static void CopyFromSettings(this MachineSettingsViewModel machineSettings)
        //{
        //    machineSettings.XAcc = Settings.Default.XAcc;
        //    //Settings.Default.XLoad
        //    machineSettings.XVelHigh = Settings.Default.XVelHigh;
        //    machineSettings.XVelLow = Settings.Default.XVelLow;
        //    machineSettings.XVelService = Settings.Default.XVelService;
        //    machineSettings.YAcc = Settings.Default.YAcc;
        //    //Settings.Default.YLoad
        //    machineSettings.YVelHigh = Settings.Default.YVelHigh;
        //    machineSettings.YVelLow = Settings.Default.YVelLow;
        //    machineSettings.YVelService = Settings.Default.YVelService;
        //    machineSettings.ZAcc = Settings.Default.ZAcc;
        //    //Settings.Default.ZObjective
        //    machineSettings.ZVelHigh = Settings.Default.ZVelHigh;
        //    machineSettings.ZVelLow = Settings.Default.ZVelLow;
        //    machineSettings.ZVelService = Settings.Default.ZVelService;
        //    machineSettings.XOffset = Settings.Default.XOffset;
        //    machineSettings.YOffset = Settings.Default.YOffset;
        //    machineSettings.XLoad = Settings.Default.XLoad;
        //    machineSettings.YLoad = Settings.Default.YLoad;
        //    machineSettings.ZCamera = Settings.Default.ZeroFocusPoint;
        //    machineSettings.ZLaser = Settings.Default.ZeroPiercePoint;
        //    machineSettings.XLeftPoint = Settings.Default.XLeftPoint;
        //    machineSettings.YLeftPoint = Settings.Default.YLeftPoint;
        //    machineSettings.XRightPoint = Settings.Default.XRightPoint;
        //    machineSettings.YRightPoint = Settings.Default.YRightPoint;
        //}
        internal static void SerializeObject(this object obj, string filePath)
        {
            var json = JsonConvert.SerializeObject(obj);
            using var writer = new StreamWriter(filePath, false);
            var l = new TextWriterTraceListener(writer);
            l.WriteLine(json);
            l.Flush();
        }
        internal static T? DeserilizeObject<T>(string filePath)
        {
            var obj = JsonConvert.DeserializeObject(File.ReadAllText(filePath), typeof(T));
            return (T?)obj;
        }

        internal static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> en)
        {
            return new ObservableCollection<T>(en);
        }

        internal static void SetObject<T>(this ref T obj, T @value) where T:struct
        {
            obj = value;
        }
    }

}
