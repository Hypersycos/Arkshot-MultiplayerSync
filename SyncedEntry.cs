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
            MultiplayerSync.myValues.Remove(key);
        }

        /// <summary>
        /// Gets the current host value, or the default value if that fails
        /// </summary>
        public T Value
        {
            get => SyncedEntries.GetValue<T>(key);
        }

        /// <summary>
        /// Gets and sets the value which will be used when this client hosts
        /// </summary>
        public T MyHostValue
        {
            get => (T)MultiplayerSync.myValues[key];
            set => MultiplayerSync.myValues[key] = value;
        }

        internal static SyncedEntry<T> NewEntry(T value, string key, T defaultValue)
        {
            string id = key;
            MultiplayerSync.myValues.Add(id, value);
            MultiplayerSync.defaultValues.Add(id, defaultValue);
            return new SyncedEntry<T>(id);
        }

        /// <summary>
        /// Returns the <see cref="CodeInstruction" langword="CodeInstructions"/> required to call <see cref="SyncedEntries.GetValue"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CodeInstruction> GetValueIL()
        {
            yield return new CodeInstruction(OpCodes.Ldstr, key);
            yield return CodeInstruction.Call(typeof(SyncedEntries), "GetValue", generics: new[] { typeof(T) });
        }
    }

    public class SyncedEntries
    { 
        /// <summary>
        /// Register a synced value for your plugin
        /// </summary>
        /// <typeparam name="T">The value's type</typeparam>
        /// <param name="value">The current value</param>
        /// <param name="defaultValue">The default value (used as a client if not found in host's values)</param>
        /// <param name="key">The key to use, must be unique to your plugin</param>
        /// <param name="plugin">The plugin to own the entry, almost always <c>this</c></param>
        /// <returns></returns>
        public static SyncedEntry<T> RegisterSyncedValue<T>(T value, T defaultValue, string key, BaseUnityPlugin plugin)
        {
            return SyncedEntry<T>.NewEntry(value, plugin.Info.Metadata.GUID+"."+key, defaultValue);
        }

        /// <summary>
        /// Creates a <see cref="SyncedEntry{T}"/> linked to a <see cref="ConfigEntry{T}"/>. Used by <see cref="SyncedConfigEntry{T}"/>
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ConfigEntry{T}"/></typeparam>
        /// <param name="configEntry">The <see cref="ConfigEntry{T}"/> to link to</param>
        /// <returns>A new <see cref="SyncedEntry{T}"/> linked to <c>configEntry</c></returns>
        /// <seealso cref="SyncedConfigEntry{T}"/>
        public static SyncedEntry<T> RegisterSyncedConfig<T>(ConfigEntry<T> configEntry)
        {
            var GUIDField = configEntry.ConfigFile.GetType().GetField("_ownerMetadata", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            string GUID = ((BepInPlugin)GUIDField.GetValue(configEntry.ConfigFile)).GUID;
            string key = GUID + "." + configEntry.Definition.Key;
            T defaultValue = (T)configEntry.DefaultValue;

            SyncedEntry<T> toReturn = SyncedEntry<T>.NewEntry(configEntry.Value, key, defaultValue);
            configEntry.SettingChanged += (_, _) => toReturn.MyHostValue = configEntry.Value;
            return toReturn;
        }

        /// <summary>
        /// Gets the current host value for <c>property</c>, or the default value if that fails.
        /// Will throw an exception if the key doesn't exist, it's much safer to use <see cref="SyncedEntry{T}.Value"/>
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="property">The key of the value - this is "GUID.key"</param>
        /// <returns>The current host value, or the default value if not found</returns>
        /// <seealso cref="SyncedEntry{T}.Value"/>
        public static T GetValue<T>(string property)
        {
            object value = null;
            if (MultiplayerSync.hostValues.TryGetValue(property, out value))
            {
                return (T)value;
            }
            else
            {
                return (T)MultiplayerSync.defaultValues[property];
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
