using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MultiplayerSync
{
    [BepInPlugin("hypersycos.plugins.arkshot.multiplayersync", "Multiplayer Sync", "1.0.1")]
    [BepInProcess("Arkshot.exe")]
    public class MultiplayerSync : BaseUnityPlugin
    {
        internal static MultiplayerSync Instance;
        internal static ManualLogSource logger => Instance.Logger;
        internal static Dictionary<string, object> myValues = new();
        internal static Dictionary<string, object> hostValues = new();
        internal static Dictionary<string, object> defaultValues = new();

        /// <summary>
        /// Fired when a client joins a lobby. /NOT/ fired when the host creates a lobby.
        /// </summary>
        public static event Action OnJoin;

        private static Dictionary<string, KeyValuePair<string, Func<bool>>> requirements = new();

        private static List<string> missingRequirements = null;
        private void Awake()
        {
            // Plugin startup logic
            PhotonPeer.RegisterType(typeof(List<string>), 255, Tools.SerializeStringList, Tools.DeserializeStringList);
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(Patches));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        /// <summary>
        /// Registers a plugin as a hard requirement. Clients using MultiplayerSync which join and do not have the plugin 
        /// loaded will automatically leave with an error message.
        /// </summary>
        /// <param name="plugin">The required plugin</param>
        public static void RegisterRequirement(BaseUnityPlugin plugin)
        {
            requirements.Add(plugin.Info.Metadata.GUID, new KeyValuePair<string, Func<bool>>(plugin.Info.Metadata.Name, null));
        }

        /// <summary>
        /// Registers a plugin as a conditional requirement. Clients using MultiplayerSync which join and do not have the plugin 
        /// loaded will automatically leave with an error message.
        /// </summary>
        /// <param name="plugin">The required plugin</param>
        /// <param name="condition">If this function returns true when the lobby is created, the plugin will be added as a requirement.</param>
        public static void RegisterConditionalRequirement(BaseUnityPlugin plugin, Func<bool> condition)
        {
            requirements.Add(plugin.Info.Metadata.GUID, new KeyValuePair<string, Func<bool>>(plugin.Info.Metadata.Name, condition));
        }

        /// <summary>
        /// Returns true if the <c>BoxedValue</c> of all config entries in config are equal to their <c>DefaultValue</c>.
        /// Useful for conditional requirements where plugins have default settings equal to vanilla - but make sure to NOT the result.
        /// </summary>
        /// <param name="config">The ConfigFile of the plugin to check</param>
        /// <param name="ignored">A list of keys which should not be checked</param>
        /// <returns>Whether all config values are equal to their default values</returns>
        public static bool AllAreDefault(ConfigFile config, List<string> ignored = null)
        {
            if (ignored == null)
            {
                ignored = new();
            }
            foreach(ConfigDefinition key in config.Keys)
            {
                if (ignored.Contains(key.Key)) continue;

                if (config[key].BoxedValue.Equals(config[key].DefaultValue))
                {
                    return false;
                }
            }
            return true;
        }

        private static class Tools
        {
            public static byte[] SerializeStringList(object objectToSerialize)
            {
                List<string> toSerialize = (List<string>)objectToSerialize;
                List<byte> bytes = new List<byte>();
                foreach(string value in toSerialize)
                {
                    byte[] encoded = Encoding.ASCII.GetBytes(value);
                    bytes.Add((byte)encoded.Length);
                    foreach (byte b in Encoding.ASCII.GetBytes(value))
                    {
                        bytes.Add(b);
                    }
                }
                return bytes.ToArray();
            }

            public static object DeserializeStringList(byte[] bytes)
            {
                List<string> result = new List<string>();
                int i = 0;
                while (i < bytes.Length)
                {
                    byte length = bytes[i++];
                    byte[] encoded = new byte[length];
                    for(int j = 0; j < length; j++)
                    {
                        encoded[j] = bytes[i++];
                    }
                    result.Add(Encoding.ASCII.GetString(encoded));
                }
                return result;
            }
            public static Hashtable AddProperties(Hashtable roomProperties)
            {
                hostValues = new();
                foreach (KeyValuePair<string, object> pair in myValues)
                {
                    roomProperties.Add(pair.Key, pair.Value);
                    hostValues.Add(pair.Key, pair.Value);
                }
                List<string> requiredGUIDs = new();
                List<string> requiredNames = new();
                foreach (KeyValuePair<string, KeyValuePair<string, Func<bool>>> set in requirements)
                {
                    string GUID = set.Key;
                    string Name = set.Value.Key;
                    Func<bool> cond = set.Value.Value;
                    if (cond == null || cond())
                    {
                        requiredGUIDs.Add(GUID);
                        requiredNames.Add(Name);
                    }
                }
                roomProperties.Add("requiredGUIDs", requiredGUIDs);
                roomProperties.Add("requiredNames", requiredNames);
                return roomProperties;
            }

            public static void SyncProperties(Hashtable roomProperties)
            {
                hostValues = new();
                List<string> defaultKeys = new() { "curScn", "jn", "gm" };
                foreach (var pair in roomProperties)
                {
                    if (!defaultKeys.Contains(pair.Key.ToString()))
                    {
                        hostValues.Add(pair.Key.ToString(), pair.Value);
                    }
                }
            }
        }

        private static class Patches
        {
            static FieldInfo customRoomProperties = AccessTools.Field(typeof(RoomOptions), nameof(RoomOptions.customRoomProperties));

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(mainMenu), "createRoom")]
            static IEnumerable<CodeInstruction> injectPropertiesMultiplayer_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instr in instructions)
                {
                    if (instr.StoresField(customRoomProperties))
                    {
                        yield return CodeInstruction.Call(typeof(Tools), nameof(Tools.AddProperties));
                    }
                    yield return instr;
                }
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(mainMenu), "Training")]
            static IEnumerable<CodeInstruction> injectPropertiesTraining_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instr in instructions)
                {
                    if (instr.StoresField(customRoomProperties))
                    {
                        yield return CodeInstruction.Call(typeof(Tools), nameof(Tools.AddProperties));
                    }
                    yield return instr;
                }
            }

            [HarmonyPatch(typeof(mainMenu), "Start")]
            [HarmonyPostfix]
            static void onMainMenuStart_Postfix(mainMenu __instance)
            {
                if (missingRequirements != null)
                {
                    MethodInfo MakePopup = typeof(mainMenu).GetMethod("MakePopup", BindingFlags.NonPublic | BindingFlags.Instance);
                    string title = "Missing Required Mod";
                    string body = "Missing: ";
                    for(int i = 0; i < missingRequirements.Count - 1; i++)
                    {
                        body += missingRequirements + "; ";
                    }
                    body += missingRequirements[missingRequirements.Count - 1];
                    MakePopup.Invoke(__instance, new[] { title, body });
                    missingRequirements = null;
                }
            }

            [HarmonyPatch(typeof(menuScript), "Start")]
            [HarmonyPrefix]
            static bool onJoinedRoom_Prefix(menuScript __instance)
            {
                if (!PhotonNetwork.isMasterClient)
                {
                    Tools.SyncProperties(PhotonNetwork.room.customProperties);

                    missingRequirements = new();
                    List<string> requiredGUIDs = (List<string>)hostValues["requiredGUIDs"];
                    List<string> requiredNames = (List<string>)hostValues["requiredNames"];

                    for (int i = 0; i < requiredGUIDs.Count; i++)
                    {
                        string requiredGUID = requiredGUIDs[i];
                        if(!requiredGUIDs.Contains(requiredGUID))
                        {
                            missingRequirements.Add(requiredNames[i]);
                        }
                    }
                    if (missingRequirements.Count > 0)
                    {
                        __instance.QuitToMenu();
                        return false;
                    }
                    missingRequirements = null;

                    OnJoin?.Invoke();
                }
                return true;
            }

            [HarmonyPatch(typeof(chatScript), "OnPhotonPlayerDisconnected")]
            [HarmonyPrefix]
            static bool onPlayerEarlyDisconnect_Prefix(PhotonPlayer otherPlayer)
            {
                if (!otherPlayer.customProperties.ContainsKey("Red"))
                {
                    return false;
                }
                return true;
            }
        }
    }
}
