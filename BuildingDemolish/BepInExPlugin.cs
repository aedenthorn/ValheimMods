using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BuildingDemolish
{
    [BepInPlugin("aedenthorn.BuildingDemolish", "Building Demolish", "0.6.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> allowDestroyUncreated;
        public static ConfigEntry<bool> requireCraftingStation;
        public static ConfigEntry<float> destroyRadius;
        public static ConfigEntry<string> hotKey;
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
            nexusID = Config.Bind<int>("General", "NexusID", 516, "Nexus mod ID for updates");
            destroyRadius = Config.Bind<float>("General", "DestroyRadius", 20, "Radius of destruction");
            allowDestroyUncreated = Config.Bind<bool>("General", "AllowDestroyUncreated", false, "Allow destroying buildings not created by any player");
            requireCraftingStation = Config.Bind<bool>("General", "RequireCraftingStation", true, "Require a nearby crafting station to destroy corresponding pieces (this is a vanilla requirement)");
            hotKey = Config.Bind<string>("General", "HotKey", ";", "Hotkey to initiate destruction");

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
                int count = DemolishPieces(destroyRadius.Value);
                Dbgl($"Demolished {count} pieces.");
            }
        }

        public static int DemolishPieces(float radius)
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
                    if (!piece.IsCreator() && (piece.GetCreator() != 0 || !allowDestroyUncreated.Value))
                    {
                        continue;
                    }
                    if (!piece.m_canBeRemoved)
                    {
                        continue;
                    }
                    if (Location.IsInsideNoBuildLocation(piece.transform.position))
                    {
                        continue;
                    }
                    if (!PrivateArea.CheckAccess(piece.transform.position, 0f, true, false))
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
                    if (!piece.CanBeRemoved())
                    {
                        continue;
                    }
                    count++;
                    WearNTear component2 = piece.GetComponent<WearNTear>();
                    if (component2)
                    {
                        component2.Remove();
                    }
                    else
                    {
                        component.ClaimOwnership();
                        piece.DropResources();
                        piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, piece.gameObject.transform, 1f);
                        player.m_removeEffects.Create(piece.transform.position, Quaternion.identity, null, 1f);
                        ZNetScene.instance.Destroy(piece.gameObject);
                    }

                }
            }
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
                    context.Config.Reload();
                    context.Config.Save();

                    __instance.AddString(text);
                    __instance.AddString($"{context.Info.Metadata.Name} config reloaded");
                    return false;
                }
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} demolish"))
                {
                    __instance.AddString(text);
                    int count = DemolishPieces(destroyRadius.Value);
                    __instance.AddString($"{context.Info.Metadata.Name} demolished {count} pieces");
                    return false;
                }
                if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} demolish "))
                {
                    __instance.AddString(text);
                    if (int.TryParse(text.Split(' ')[2], out int radius))
                    {
                        int count = DemolishPieces(radius);
                        __instance.AddString($"{context.Info.Metadata.Name} demolished {count} pieces");
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