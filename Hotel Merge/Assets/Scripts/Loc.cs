using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    /// <summary>
    /// Languages. The game is written in Turkish; Assets/Resources/Localization/Strings.txt maps every Turkish text
    /// (first column, the key) to the other languages (one column each, header row = language codes).
    /// Add a column to add a language. English is the default.
    ///   Loc.T("Kabul Et")            → "Accept"
    ///   Loc.F("Oda {0}", 3)          → "Room 3"
    /// </summary>
    public static class Loc
    {
        public const string Source = "tr";
        public const string Default = "en";
        const string PrefKey = "hotelmerge_lang";

        static Dictionary<string, Dictionary<string, string>> tables;
        static string language;
        static readonly HashSet<string> missing = new HashSet<string>();

        /// <summary>Language codes in the table, e.g. "en", "tr".</summary>
        public static readonly List<string> Languages = new List<string>();

        /// <summary>Name of each language in itself (for the settings panel).</summary>
        public static string DisplayName(string code) => code switch
        {
            "en" => "English",
            "tr" => "Türkçe",
            "de" => "Deutsch",
            "es" => "Español",
            "fr" => "Français",
            _ => code.ToUpperInvariant(),
        };

        public static string Language
        {
            get
            {
                if (language == null) language = PlayerPrefs.GetString(PrefKey, Default);
                return language;
            }
            set
            {
                language = value;
                PlayerPrefs.SetString(PrefKey, value);
                PlayerPrefs.Save();
#if UNITY_WEBGL && !UNITY_EDITOR
                HotelMerge_SaveLanguage(value); // the web loader page reads it on the next visit
#endif
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void HotelMerge_SaveLanguage(string code);
#endif

        public static string T(string text)
        {
            // While the editor builds the scene/prefabs, texts stay Turkish (they are the keys).
            if (string.IsNullOrEmpty(text) || Language == Source || !Application.isPlaying) return text;
            Load();
            if (tables.TryGetValue(Language, out var table) && table.TryGetValue(text, out var t) && t.Length > 0) return t;
            if (Application.isEditor && missing.Add(text)) Debug.LogWarning($"Loc: '{text}' için {Language} çevirisi yok (Strings.txt).");
            return text;
        }

        public static string F(string template, params object[] args) => string.Format(T(template), args);

        static void Load()
        {
            if (tables != null) return;
            tables = new Dictionary<string, Dictionary<string, string>>();
            Languages.Clear();
            Languages.Add(Source);
            var asset = Resources.Load<TextAsset>("Localization/Strings");
            if (!asset) return;
            var lines = asset.text.Replace("\r", "").Split('\n');
            var header = lines[0].Split('\t');
            for (int c = 1; c < header.Length; c++)
            {
                tables[header[c].Trim()] = new Dictionary<string, string>();
                Languages.Add(header[c].Trim());
            }
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0 || lines[i].StartsWith("#")) continue;
                var cols = lines[i].Split('\t');
                string key = Unescape(cols[0]);
                for (int c = 1; c < header.Length && c < cols.Length; c++) tables[header[c].Trim()][key] = Unescape(cols[c]);
            }
        }

        static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\t", "\t");

        /// <summary>All languages the table offers (loads it if needed).</summary>
        public static List<string> Available()
        {
            Load();
            return Languages;
        }
    }

    /// <summary>A fixed list of Turkish texts that reads translated: Areas.Name[i] stays the same at the call site.</summary>
    public class LocList
    {
        readonly string[] source;
        public LocList(params string[] texts) => source = texts;
        public string this[int i] => Loc.T(source[i]);
        public int Length => source.Length;
    }
}
