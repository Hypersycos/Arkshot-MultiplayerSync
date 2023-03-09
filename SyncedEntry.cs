using BepInEx.Configuration;
using ExitGames.Client.Photon;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MultiplayerSync
{
    public class SyncedEntry<T>
    {
        private string key;

        private SyncedEntry(string key)
        {
            this.key = key;
        }

        ~SyncedEntry()
        {
            Plugin.myValues.Remove(key);
        }

        public T Value
        {
            get => (T)Plugin.hostValues[key];
            set => Plugin.myValues[key] = value;
        }

        internal static SyncedEntry<T> NewEntry(T value)
        {
            string id = Plugin.EntryCount.ToString();
            Plugin.myValues.Add(id, value);
            return new SyncedEntry<T>(id);
        }

        public IEnumerable<CodeInstruction> GetValueIL()
        {
            yield return new CodeInstruction(OpCodes.Ldstr, key);
            yield return CodeInstruction.Call(typeof(SyncedEntries), "GetValue", generics: new[] { typeof(T) });
        }
    }

    public class SyncedEntries
    { 
        public static SyncedEntry<T> RegisterSyncedValue<T>(T value)
        {
            return SyncedEntry<T>.NewEntry(value);
        }

        public static SyncedEntry<T> RegisterSyncedConfig<T>(ConfigEntry<T> configEntry)
        {
            SyncedEntry<T> toReturn = SyncedEntry<T>.NewEntry(configEntry.Value);
            configEntry.SettingChanged += (_, _) => toReturn.Value = configEntry.Value;
            return toReturn;
        }

        public static T GetValue<T>(string property)
        {
            object value = null;
            Plugin.hostValues.TryGetValue(property, out value);
            if (value == null)
            {
                Plugin.logger.LogInfo("Failed to get " + property);
                return (T)Plugin.myValues[property];
            }
            else
            {
                return (T)value;
            }
        }

        internal static T GetProperty<T>(Hashtable roomProperties, string property)
        {
            object value = null;
            roomProperties.TryGetValue(property, out value);
            return (T)value;
        }
    }
}
