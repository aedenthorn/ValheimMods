using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BuildingRepair
{
    [BepInPlugin("aedenthorn.BuildingRepair", "Building Repair", "0.5.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> allowRepairOther;
        public static ConfigEntry<bool> requireCraftingStation;
        public static ConfigEntry<float> repairRadius;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> repairMessage;
        public static int destroyMask = LayerMask.GetMask(new string[]
        {
            "Default",
            "static_solid",
            "Default_small",
            "piece",
            "piece_nonsolid",
            "terrain",
            "vehicle"
        });

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1277, "Nexus mod ID for updates");
            repairRadius = Config.Bind<float>("General", "RepairRadius", 20, "Radius of repair");
            allowRepairOther = Config.Bind<bool>("General", "AllowRepairOther", false, "Aloow repairing other player's pieces");
            requireCraftingStation = Config.Bind<bool>("General", "RequireCraftingStation", true, "Require a nearby crafting station to repair corresponding pieces (this is a vanilla requirement)");
            hotKey = Config.Bind<string>("General", "HotKey", "'", "Hotkey to initiate repair");
            repairMessage = Config.Bind<string>("General", "RepairMessage", "Repaired {0} pieces.", "Repair message text.");

            if (!modEnabled.Value)
                return;

            //if (debugEnabled.Value)
            //    Player.m_debugMode = true;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Update()
        {
            if (!AedenthornUtils.IgnoreKeyPresses(true) && AedenthornUtils.CheckKeyDown(hotKey.Value)) 
            {
                int count = repairPieces(repairRadius.Value);
                Dbgl($"Repaired {count} pieces.");
            }
        }

        public static int repairPieces(float radius)
        {
            Player player = Player.m_localPlayer;
            if (!player)
                return 0;
            int count = 0;
            Collider[] array = Physics.OverlapSphere(player.transform.position, radius, destroyMask);
            for (int i = 0; i < array.Length; i++)
            {
                Piece piece = array[i].GetComponentInParent<Piece>();
                if (piece)
                {
                    if (!allowRepairOther.Value && !piece.IsCreator())
                    {
                        continue;
                    }
                    if (requireCraftingStation.Value && !Traverse.Create(player).Method("CheckCanRemovePiece", new object[] { piece }).GetValue<bool>())
                    {
                        continue;
                    }
                    ZNetView component = piece.GetComponent<ZNetView>();
                    if (component == null)
                    {
                        continue;
                    }
                    WearNTear wnt = piece.GetComponent<WearNTear>();
                    if (!wnt || !wnt.Repair())
                        continue;
                    count++;
                }
            }
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(repairMessage.Value, count));
            return count;
        }
        [HarmonyPatch(typeof(Terminal), "InputText")]
        public static class InputText_Patch
        {
            public static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    __instance.AddString(text);
                    context.Config.Reload();
                    context.Config.Save();
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} repair"))
                {
                    __instance.AddString(text);
                    int count = repairPieces(repairRadius.Value);
                    __instance.AddString($"{context.Info.Metadata.Name} repaired {count} pieces");
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} repair "))
                {
                    __instance.AddString(text);
                    if (int.TryParse(text.Split(' ')[2], out int radius))
                    {
                        int count = repairPieces(radius);
                        __instance.AddString($"{context.Info.Metadata.Name} repaired {count} pieces");
                    }
                    else
                        __instance.AddString($"{context.Info.Metadata.Name} syntax error");
                    return false;
                }
                return true;
            }
        }

    }
}