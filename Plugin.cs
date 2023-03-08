using BepInEx;
using ExitGames.Client.Photon;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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
            static List<string> names;
            static List<object> values;
            public static Hashtable AddProperties(Hashtable roomProperties)
            {
                for (int i = 0; i < names.Count; i++)
                {
                    roomProperties.Add(names[i], values[i]);
                }
                return roomProperties;
            }

            public static void RegisterProperty(BepInPlugin plugin, string property, object value)
            {
                names.Add(plugin.GUID + property);
                values.Add(value);
            }

            public static object GetProperty(BepInPlugin plugin, Hashtable roomProperties, string property)
            {
                object value = null;
                roomProperties.TryGetValue(plugin.GUID + property, out value);
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
            static IEnumerable<CodeInstruction> injectProperties_Transpiler(IEnumerable<CodeInstruction> instructions)
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
        }
    }
}
