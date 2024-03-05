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
            if (!File.Exists(filePath)) return default(T);
            var obj = JsonConvert.DeserializeObject(File.ReadAllText(filePath), typeof(T));
            return (T?)obj;
        }

        internal static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> en)
        {
            return new ObservableCollection<T>(en);
        }

        internal static void AddToSubscriptions(this IDisposable subscription, IEnumerable<IDisposable> subscriptions) => subscriptions?.Append(subscription);
    } 

}
