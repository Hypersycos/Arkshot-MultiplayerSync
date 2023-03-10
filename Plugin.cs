using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MultiplayerSync
{
    [BepInPlugin("hypersycos.plugins.arkshot.multiplayersync", "Multiplayer Sync", "1.0.0")]
    public class MultiplayerSync : BaseUnityPlugin
    {
        public static MultiplayerSync Instance;
        internal static ManualLogSource logger => Instance.Logger;
        internal static Dictionary<string, object> myValues = new();
        internal static Dictionary<string, object> hostValues = new();
        internal static Dictionary<string, object> defaultValues = new();
        public static event Action OnJoin;

        private static SyncedEntry<List<string>> requirementGUIDs;
        private static SyncedEntry<List<string>> requirementNames;
        private static List<string> missingRequirements = null;
        private void Awake()
        {
            // Plugin startup logic
            requirementGUIDs = SyncedEntries.RegisterSyncedValue(new List<string>(), new List<string>(), "requirementGUIDs", this);
            requirementNames = SyncedEntries.RegisterSyncedValue(new List<string>(), new List<string>(), "requirementNames", this);
            PhotonPeer.RegisterType(typeof(List<string>), 255, Tools.SerializeStringList, Tools.DeserializeStringList);
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(Patches));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public static void RegisterRequirement(BaseUnityPlugin plugin)
        {
            requirementGUIDs.MyHostValue.Add(plugin.Info.Metadata.GUID);
            requirementNames.MyHostValue.Add(plugin.Info.Metadata.Name);
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

        public class Patches
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

                    for(int i = 0; i < requirementGUIDs.Value.Count; i++)
                    {
                        string requiredGUID = requirementGUIDs.Value[i];
                        if(!requirementGUIDs.MyHostValue.Contains(requiredGUID))
                        {
                            missingRequirements.Add(requirementNames.Value[i]);
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
            static bool onPlayerEarlyDisconnect_Prefix(PhotonPlayer player)
            {
                if (!player.customProperties.ContainsKey("Red"))
                {
                    return false;
                }
                return true;
            }
        }
    }
}
