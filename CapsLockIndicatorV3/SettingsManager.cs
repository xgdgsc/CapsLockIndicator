﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CapsLockIndicatorV3
{
    public static class SettingsManager
    {
        public static string SettingsFilePath => Environment.ExpandEnvironmentVariables(@"%appdata%\Jonas Kohl\CapsLock Indicator\settings\any\usercfg");
        public static string SettingsFileLocal => Environment.ExpandEnvironmentVariables(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "usercfg"));

        private static Dictionary<string, (Type, object)> Settings;

        public static void Load()
        {
            LoadDefaults();
            LoadUser();
        }

        private static void LoadDefaults()
        {
            var lines = resources.defaultSettings.Split('\n');
            if (Settings == null)
                Settings = new Dictionary<string, (Type, object)>();

            foreach (var ln in lines)
            {
                if (ln.Length < 1) continue;
                var (typeAndKey, value) = ln.Split(new char[] { '=' }, 2, StringSplitOptions.None);
                var (type, key) = typeAndKey.Split(new char[] { ':' }, 2, StringSplitOptions.None);
                var t = GetSettingsType(type);

                Settings[key] = (t, Cast(value, t));
            }
        }

        private static void LoadUser()
        {
            var path = SettingsFileLocal;

            if (!File.Exists(path))
                path = SettingsFilePath;

            if (!File.Exists(SettingsFilePath))
                return;

            var lines = File.ReadAllLines(path);
            if (Settings == null)
                Settings = new Dictionary<string, (Type, object)>();

            foreach (var ln in lines)
            {
                if (ln.Length < 1) continue;
                var (typeAndKey, value) = ln.Split(new char[] { '=' }, 2, StringSplitOptions.None);
                var (type, key) = typeAndKey.Split(new char[] { ':' }, 2, StringSplitOptions.None);
                var t = GetSettingsType(type);

                Settings[key] = (t, Cast(value, t));
            }
        }

        public static object Get(string key)
        {
            var d = Settings[key];
            return Convert.ChangeType(d.Item2, d.Item1);
        }

        public static T Get<T>(string key)
        {
            var d = Settings[key];
            if (typeof(T).IsEnum)
                return (T)Enum.Parse(typeof(T), d.Item2.ToString());
            return (T)d.Item2;
        }

        public static T GetOrDefault<T>(string key)
        {
            return GetOrDefault(key, default(T));
        }

        public static T GetOrDefault<T>(string key, T defaultValue)
        {
            if (Settings.ContainsKey(key))
                try
                {
                    return Get<T>(key);
                }
                catch
                {
                    return defaultValue;
                }
            return defaultValue;
        }

        public static void Save()
        {
            var sb = new StringBuilder();

            foreach (var s in Settings)
                sb.AppendFormat("{0}:{1}={2}\n", GetSettingsTypeIndicator(s.Value.Item1), s.Key, ConvertToString(s.Value.Item2));

            var path = SettingsFileLocal;

            if (!File.Exists(path))
                path = SettingsFilePath;

            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
        }

        private static object Cast(string value, Type type)
        {
            if (type == typeof(Color))
            {
                if (value.Contains(";"))
                {
                    var parts = value.Split(';');
                    var parsedParts = parts.Select(p => byte.Parse(p)).ToArray();
                    if (parts.Length > 3)
                        return Color.FromArgb(parsedParts[0], parsedParts[1], parsedParts[2], parsedParts[3]);
                    else
                        return Color.FromArgb(255, parsedParts[0], parsedParts[1], parsedParts[2]);
                }
                else
                    return Color.FromName(value);
            }
            else if (type == typeof(Font))
                return new FontConverter().ConvertFromString(value);
            return Convert.ChangeType(value, type);
        }

        private static string ConvertToString(object value)
        {
            if (value.GetType() == typeof(Color))
                return string.Join(";", new byte[] { ((Color)value).A, ((Color)value).R, ((Color)value).G, ((Color)value).B });
            else if (value.GetType() == typeof(Font))
                return new FontConverter().ConvertToString((Font)value);
            return value.ToString();
        }

        private static string GetSettingsTypeIndicator(Type type)
        {
            if (type == typeof(bool))
                return "b";
            else if (type == typeof(int))
                return "i";
            else if (type == typeof(string))
                return "s";
            else if (type == typeof(Color))
                return "c";
            else if (type == typeof(Font))
                return "f";
            else
                return "o";
        }

        private static Type GetSettingsType(string type)
        {
            switch (type)
            {
                case "b":
                    return typeof(bool);
                case "i":
                    return typeof(int);
                case "s":
                    return typeof(string);
                case "c":
                    return typeof(Color);
                case "f":
                    return typeof(Font);
                default:
                    return typeof(object);
            }
        }

        public static void Set(string key, object value)
        {
            Settings[key] = (value.GetType(), value);
        }

        public static string GetActualPath()
        {
            var path = SettingsFileLocal;

            if (!File.Exists(path))
                path = SettingsFilePath;

            return path;
        }
    }
}
