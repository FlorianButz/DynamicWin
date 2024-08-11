using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.Utils
{
    internal class SaveManager
    {
        private static Dictionary<string, object> data = new Dictionary<string, object>();
        public static Dictionary<string, object> SaveData { get { return data; } set => data = value; }

        public static string SavePath = Environment.SpecialFolder.ApplicationData + @"\DynamicWin\";
        static string fileName = "Settings.dws";

        public static void LoadData()
        {
            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);

            var fullPath = Path.Combine(SavePath, fileName);

            if (!File.Exists(fullPath))
            {
                var fs = File.Create(fullPath);
                fs.Close();
                File.WriteAllText(fullPath, JsonConvert.SerializeObject(new Dictionary<string, object>()));
            }

            var json = File.ReadAllText(fullPath);
            data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        public static void SaveAll()
        {
            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);

            var fullPath = Path.Combine(SavePath, fileName);
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);

            if (!File.Exists(fullPath))
                File.Create(fullPath);

            File.WriteAllText(fullPath, json);
        }

        public static void Add(string key, object value)
        {
            if (!Contains(key))
                data.Add(key, value);
            else
                data[key] = value;
        }

        public static void Remove(string key)
        {
            if (Contains(key))
                data.Remove(key);
        }

        public static object Get(string key)
        {
            if (Contains(key))
                return data[key];
            else
                return default;
        }

        public static bool Contains(string key)
        {
            return data.ContainsKey(key);
        }
    }
}
