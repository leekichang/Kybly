﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Text;
using System.Text.Json;
using System.Windows.Xps;
using System.Runtime.Serialization.Json;

namespace AutoHotkeyRemaster.Models
{
    public class HotkeyProfile
    {
        public const int MAX_HOTKEY = 60;

        private string _profileName = null;
        public string ProfileName
        {
            get
            {
                return _profileName ?? $"profile{ProfileNum}";

            }
            set { _profileName = value; }
        }

        public List<Hotkey> Hotkeys { get; private set; } = new List<Hotkey>();

        [JsonIgnore]
        public int ProfileNum { get; set; }
        [JsonIgnore]
        public int HotkeyCount
        {
            get { return Hotkeys.Count; }
            private set { }
        }

        private HotkeyProfile() { }

        public static HotkeyProfile CreateNewProfile(int profileNum, string profileName = null)
        {
            HotkeyProfile profile = new HotkeyProfile();
            profile.ProfileNum = profileNum;
            profile.ProfileName = profileName;

            return profile;
        }

        /// <summary>
        /// Add new hotkey to profile
        /// </summary>
        /// <param name="hotkey"></param>
        /// <returns>
        ///-1 : profile already full <para />
        /// 0 : Hotkey already exists. <para />
        /// otherwise : return the number of hotkeys in the profile
        ///</returns>
        public int AddHotkey(Hotkey hotkey)
        {
            if (HotkeyCount > MAX_HOTKEY)
            {
                return -1;
            }
            else if (HasHotkey(hotkey))
            {
                return 0;
            }

            Hotkeys.Add(hotkey);

            return HotkeyCount;
        }

        public bool HasHotkey(Hotkey hotkeyToCheck)
        {
            foreach (var hotkey in Hotkeys)
            {
                if (hotkeyToCheck == hotkey)
                    return true;
            }

            return false;
        }

        public static HotkeyProfile LoadFromFile(int profileNum)
        {
            string filename = $"profile{profileNum}";
            string path = Environment.CurrentDirectory + "/savefiles/" + filename + ".json";

            if (!File.Exists(path))
            {
                return null;
            }

            string jsonString = File.ReadAllText(path);
            HotkeyProfile profile = JsonSerializer.Deserialize<HotkeyProfile>(jsonString);
            profile.ProfileNum = profileNum;

            return profile;
        }

        public void Save()
        {
            Save($"profile{ProfileNum}");
        }

        public void Save(string filename)
        {
            string saveFolderPath = Path.Combine(Environment.CurrentDirectory, "savefiles");
            string path = Path.Combine(saveFolderPath, filename + ".json");


            if(!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(path, jsonString);
        }

        public void Delete(string filename)
        {
            string path = Environment.CurrentDirectory + "/savefiles/" + filename + ".json";

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

    }
}
