using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MultiplayerSync
{
    [BepInPlugin("hypersycos.plugins.arkshot.multiplayersync", "Multiplayer Sync", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static ManualLogSource logger => Instance.Logger;
        internal static Dictionary<string, object> myValues = new();
        internal static Dictionary<string, object> hostValues = new();
        internal static Dictionary<string, object> defaultValues = new();
        public static event Action OnJoin;
        private void Awake()
        {
            // Plugin startup logic
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(Patches));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static class Tools
        {
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
                foreach (var pair in roomProperties)
                {
                    hostValues.Add(pair.Key.ToString(), pair.Value);
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

            [HarmonyPatch(typeof(menuScript), "Join")]
            [HarmonyPrefix]
            static void onJoinedRoom_Prefix()
            {
                if (!PhotonNetwork.isMasterClient)
                {
                    Tools.SyncProperties(PhotonNetwork.room.customProperties);
                    OnJoin?.Invoke();
                }
            }
        }
    }
}
