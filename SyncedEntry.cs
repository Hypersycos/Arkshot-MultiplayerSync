using BepInEx;
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
            get => SyncedEntries.GetValue<T>(key);
            set => Plugin.myValues[key] = value;
        }

        public T MyHostValue
        {
            get => (T)Plugin.myValues[key];
            set => Plugin.myValues[key] = value;
        }

        internal static SyncedEntry<T> NewEntry(T value, string key, T defaultValue)
        {
            string id = key;
            Plugin.myValues.Add(id, value);
            Plugin.defaultValues.Add(id, defaultValue);
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
        public static SyncedEntry<T> RegisterSyncedValue<T>(T value, T defaultValue, string key, BaseUnityPlugin plugin)
        {
            return SyncedEntry<T>.NewEntry(value, plugin.Info.Metadata.GUID+"."+key, defaultValue);
        }

        public static SyncedEntry<T> RegisterSyncedConfig<T>(ConfigEntry<T> configEntry)
        {
            var GUIDField = configEntry.ConfigFile.GetType().GetField("_ownerMetadata", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            string GUID = ((BepInPlugin)GUIDField.GetValue(configEntry.ConfigFile)).GUID;
            string key = GUID + "." + configEntry.Definition.Key;
            T defaultValue = (T)configEntry.DefaultValue;

            SyncedEntry<T> toReturn = SyncedEntry<T>.NewEntry(configEntry.Value, key, defaultValue);
            configEntry.SettingChanged += (_, _) => toReturn.Value = configEntry.Value;
            return toReturn;
        }

        public static T GetValue<T>(string property)
        {
            object value = null;
            if (Plugin.hostValues.TryGetValue(property, out value))
            {
                return (T)value;
            }
            else
            {
                return (T)Plugin.defaultValues[property];
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
