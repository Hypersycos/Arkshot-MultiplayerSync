using BepInEx;
using BepInEx.Configuration;
using ExitGames.Client.Photon;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace MultiplayerSync
{
    [BepInPlugin("hypersycos.plugins.arkshot.multiplayersync", "Multiplayer Sync", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public class Tools
        {
            private static Dictionary<string, object> myValues = new();
            private static Dictionary<string, object> hostValues = new();
            private static int entryCount = 0;

            public class SyncedEntry<T>
            {
                private string key;

                private SyncedEntry(string key)
                {
                    this.key = key;
                }

                ~SyncedEntry()
                {
                    myValues.Remove(key);
                }

                public T Value
                {
                    get => (T)hostValues[key];
                    set => myValues[key] = value;
                }

                internal static SyncedEntry<T> NewEntry(T value)
                {
                    myValues.Add(entryCount.ToString(), value);
                    return new SyncedEntry<T>(entryCount++.ToString());
                }
            }
            internal static Hashtable AddProperties(Hashtable roomProperties)
            {
                hostValues = new();
                foreach(KeyValuePair<string, object> pair in myValues)
                {
                    roomProperties.Add(pair.Key, pair.Value);
                    hostValues.Add(pair.Key, pair.Value);
                }
                return roomProperties;
            }

            internal static void SyncProperties(Hashtable roomProperties)
            {
                hostValues = new();
                foreach (var pair in roomProperties)
                {
                    hostValues.Add(pair.Key.ToString(), pair.Value);
                }
            }

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

            public static object GetProperty(Hashtable roomProperties, string property)
            {
                object value = null;
                roomProperties.TryGetValue(property, out value);
                return value;
            }
        }

        [HarmonyPatch(typeof(mainMenu))]
        public class Patches
        {
            static FieldInfo customRoomProperties = AccessTools.Field(typeof(RoomOptions), nameof(RoomOptions.customRoomProperties));
            static MethodInfo addValues = SymbolExtensions.GetMethodInfo(() => Tools.AddProperties);

            [HarmonyTranspiler]
            [HarmonyPatch("createRoom")]
            static IEnumerable<CodeInstruction> injectPropertiesMultiplayer_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instr in instructions)
                {
                    if (instr.StoresField(customRoomProperties))
                    {
                        yield return new CodeInstruction(OpCodes.Call, addValues);
                    }
                    yield return instr;
                }
            }

            [HarmonyTranspiler]
            [HarmonyPatch("Training")]
            static IEnumerable<CodeInstruction> injectPropertiesTraining_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instr in instructions)
                {
                    if (instr.StoresField(customRoomProperties))
                    {
                        yield return new CodeInstruction(OpCodes.Call, addValues);
                    }
                    yield return instr;
                }
            }

            [HarmonyPatch("OnJoinedRoom")]
            [HarmonyPrefix]
            static void onJoinedRoom_Prefix()
            {
                if (!PhotonNetwork.isMasterClient)
                {
                    Tools.SyncProperties(PhotonNetwork.room.customProperties);
                }
            }
        }
    }
}
