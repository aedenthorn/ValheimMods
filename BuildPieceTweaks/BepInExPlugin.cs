using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BuildPieceTweaks
{
    [BepInPlugin("aedenthorn.BuildPieceTweaks", "Build Piece Tweaks", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<bool> globalPieceClipEverything;
        public static ConfigEntry<bool> globalAllowedInDungeons;
        public static ConfigEntry<bool> globalRepairPiece;
        public static ConfigEntry<bool> globalCanBeRemoved;
        
        private static Dictionary<string, PieceData> pieceDatas;
        private static string assetPath;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 1201, "Nexus mod ID for updates");

            globalPieceClipEverything = Config.Bind<bool>("Global", "GlobalPieceClipEverything", false, "Global piece clip everything.");
            globalAllowedInDungeons = Config.Bind<bool>("Global", "GlobalAllowedInDungeons", false, "Global allowed in dungeons.");
            globalRepairPiece = Config.Bind<bool>("Global", "GlobalRepairPiece", false, "Global repair piece.");
            globalCanBeRemoved = Config.Bind<bool>("Global", "GlobalCanBeRemoved", false, "Global can be removed.");

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);

            pieceDatas = GetDataFromFiles();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        [HarmonyPatch(typeof(Piece), "Awake")]
        static class Piece_Awake_Patch
        {
            static void Postfix(Piece __instance)
            {
                if (!modEnabled.Value)
                    return;

                //Dbgl($"loading data for {Utils.GetPrefabName(__instance.gameObject)}");

                if (pieceDatas.ContainsKey(Utils.GetPrefabName(__instance.gameObject)))
                {
                    PieceData data = pieceDatas[Utils.GetPrefabName(__instance.gameObject)];
                    __instance.m_category = data.category;
                    __instance.m_comfortGroup = data.comfortGroup;
                    __instance.m_comfort = data.comfort;
                    __instance.m_groundPiece = data.groundPiece;
                    __instance.m_allowAltGroundPlacement = data.allowAltGroundPlacement;
                    __instance.m_groundOnly = data.groundOnly;
                    __instance.m_cultivatedGroundOnly = data.cultivatedGroundOnly;
                    __instance.m_waterPiece = data.waterPiece;
                    __instance.m_clipGround = data.clipGround;
                    __instance.m_clipEverything = data.clipEverything;
                    __instance.m_noInWater = data.noInWater;
                    __instance.m_notOnWood = data.notOnWood;
                    __instance.m_notOnTiltingSurface = data.notOnTiltingSurface;
                    __instance.m_inCeilingOnly = data.inCeilingOnly;
                    __instance.m_notOnFloor = data.notOnFloor;
                    __instance.m_noClipping = data.noClipping;
                    __instance.m_onlyInTeleportArea = data.onlyInTeleportArea;
                    __instance.m_allowedInDungeons = data.allowedInDungeons;
                    __instance.m_spaceRequirement = data.spaceRequirement;
                    __instance.m_repairPiece = data.repairPiece;
                    __instance.m_canBeRemoved = data.canBeRemoved;
                    __instance.m_onlyInBiome = data.onlyInBiome;

                    WearNTear wnt = __instance.gameObject.GetComponent<WearNTear>();
                    if (wnt)
                    {

                        wnt.m_health = data.health;
                        wnt.m_noRoofWear = data.noRoofWear;
                        wnt.m_noSupportWear = data.noSupportWear;
                        wnt.m_materialType = data.materialType;
                        wnt.m_supports = data.supports;
                        wnt.m_comOffset = data.comOffset;
                        wnt.m_hitNoise = data.hitNoise;
                        wnt.m_destroyNoise = data.destroyNoise;
                        wnt.m_autoCreateFragments = data.autoCreateFragments;

                        foreach (string modString in data.damageModifiers)
                        {
                            string[] parts = modString.Split(':');
                            var type = (HitData.DamageType)Enum.Parse(typeof(HitData.DamageType), parts[0]);
                            var mod = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), parts[1]);
                            switch (type)
                            {
                                case HitData.DamageType.Blunt:
                                    wnt.m_damages.m_blunt = mod;
                                    break;
                                case HitData.DamageType.Slash:
                                    wnt.m_damages.m_slash = mod;
                                    break;
                                case HitData.DamageType.Pierce:
                                    wnt.m_damages.m_pierce = mod;
                                    break;
                                case HitData.DamageType.Chop:
                                    wnt.m_damages.m_chop = mod;
                                    break;
                                case HitData.DamageType.Pickaxe:
                                    wnt.m_damages.m_pickaxe = mod;
                                    break;
                                case HitData.DamageType.Fire:
                                    wnt.m_damages.m_fire = mod;
                                    break;
                                case HitData.DamageType.Frost:
                                    wnt.m_damages.m_frost = mod;
                                    break;
                                case HitData.DamageType.Lightning:
                                    wnt.m_damages.m_lightning = mod;
                                    break;
                                case HitData.DamageType.Poison:
                                    wnt.m_damages.m_poison = mod;
                                    break;
                                case HitData.DamageType.Spirit:
                                    wnt.m_damages.m_spirit = mod;
                                    break;

                            }
                        }
                    }

                }

                if (globalPieceClipEverything.Value)
                    __instance.m_clipEverything = true;
                if (globalAllowedInDungeons.Value)
                    __instance.m_allowedInDungeons = true;
                if (globalRepairPiece.Value)
                    __instance.m_repairPiece = true;
                if (globalCanBeRemoved.Value)
                    __instance.m_canBeRemoved = true;

            }
        }


        private static Dictionary<string, PieceData> GetDataFromFiles()
        {
            CheckModFolder();

            Dictionary<string, PieceData> datas = new Dictionary<string, PieceData>();

            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                PieceData data = JsonUtility.FromJson<PieceData>(File.ReadAllText(file));
                datas.Add(data.name, data);
            }
            return datas;
        }

        private static void CheckModFolder()
        {
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }
        }

        private static PieceData GetDataByName(string pieceName)
        {
            ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer").GetComponent<ItemDrop>();

            var pieces = Traverse.Create(hammer.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>();

            var go = pieces.FirstOrDefault(p => Utils.GetPrefabName(p) == pieceName);
            
            return GetDataFromItem(go, pieceName);

            Dbgl($"Game object {pieceName} not found!");
            return null;
        }

        private static PieceData GetDataFromItem(GameObject go, string pieceName)
        {
            Piece piece = go?.GetComponent<Piece>();

            if (piece == null)
                return null;

            var pieceData = new PieceData()
            {
                name = pieceName,
                category = piece.m_category,
                comfortGroup = piece.m_comfortGroup,
                comfort = piece.m_comfort,
                groundPiece = piece.m_groundPiece,
                allowAltGroundPlacement = piece.m_allowAltGroundPlacement,
                groundOnly = piece.m_groundOnly,
                cultivatedGroundOnly = piece.m_cultivatedGroundOnly,
                waterPiece = piece.m_waterPiece,
                clipGround = piece.m_clipGround,
                clipEverything = piece.m_clipEverything,
                noInWater = piece.m_noInWater,
                notOnWood = piece.m_notOnWood,
                notOnTiltingSurface = piece.m_notOnTiltingSurface,
                inCeilingOnly = piece.m_inCeilingOnly,
                notOnFloor = piece.m_notOnFloor,
                noClipping = piece.m_noClipping,
                onlyInTeleportArea = piece.m_onlyInTeleportArea,
                allowedInDungeons = piece.m_allowedInDungeons,
                spaceRequirement = piece.m_spaceRequirement,
                repairPiece = piece.m_repairPiece,
                canBeRemoved = piece.m_canBeRemoved,
                onlyInBiome = piece.m_onlyInBiome
            };

            WearNTear wnt = go.GetComponent<WearNTear>();
            if (wnt)
            {
                pieceData.health = wnt.m_health;
                pieceData.noRoofWear = wnt.m_noRoofWear;
                pieceData.noSupportWear = wnt.m_noSupportWear;
                pieceData.supports = wnt.m_supports;
                pieceData.comOffset = wnt.m_comOffset;
                pieceData.hitNoise = wnt.m_hitNoise;
                pieceData.destroyNoise = wnt.m_destroyNoise;
                pieceData.autoCreateFragments = wnt.m_autoCreateFragments;
                pieceData.damageModifiers = new List<string>()
                {
                    Enum.GetName(typeof(HitData.DamageType), HitData.DamageType.Blunt) + ":" + Enum.GetName(typeof(HitData.DamageModifier), wnt.m_damages.m_blunt),
                    Enum.GetName(typeof(HitData.DamageType), HitData.DamageType.Slash) + ":" + Enum.GetName(typeof(HitData.DamageModifier), wnt.m_damages.m_slash),
                    Enum.GetName(typeof(HitData.DamageType), HitData.DamageType.Pierce) + ":" + Enum.GetName(typeof(HitData.DamageModifier), wnt.m_damages.m_pierce),
                    Enum.GetName(typeof(HitData.DamageType), HitData.DamageType.Chop) + ":" + Enum.GetName(typeof(HitData.DamageModifier), wnt.m_damages.m_chop),
                    Enum.GetName(typeof(HitData.DamageType), HitData.DamageType.Pickaxe) + ":" + Enum.GetName(typeof(HitData.DamageModifier), wnt.m_damages.m_pickaxe),
                    Enum.GetName(typeof(HitData.DamageType), HitData.DamageType.Fire) + ":" + Enum.GetName(typeof(HitData.DamageModifier), wnt.m_damages.m_fire),
                    Enum.GetName(typeof(HitData.DamageType), HitData.DamageType.Frost) + ":" + Enum.GetName(typeof(HitData.DamageModifier), wnt.m_damages.m_frost),
                    Enum.GetName(typeof(HitData.DamageType), HitData.DamageType.Lightning) + ":" + Enum.GetName(typeof(HitData.DamageModifier), wnt.m_damages.m_lightning),
                    Enum.GetName(typeof(HitData.DamageType), HitData.DamageType.Poison) + ":" + Enum.GetName(typeof(HitData.DamageModifier), wnt.m_damages.m_poison),
                    Enum.GetName(typeof(HitData.DamageType), HitData.DamageType.Spirit) + ":" + Enum.GetName(typeof(HitData.DamageModifier), wnt.m_damages.m_spirit)
                };
            }

            return pieceData;
        }

        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;

                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reload"))
                {
                    pieceDatas = GetDataFromFiles();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} reloaded piece variables from files" }).GetValue();
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} comfort"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();

                    List<string> output = new List<string>();
                    foreach (Piece.ComfortGroup type in Enum.GetValues(typeof(Piece.ComfortGroup)))
                    {
                        output.Add(Enum.GetName(typeof(Piece.ComfortGroup), type) + " " + (int)type);
                    }
                    Dbgl("\r\n"+string.Join("\r\n", output));

                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped comfort groups" }).GetValue();
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} cats"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();

                    List<string> output = new List<string>();
                    foreach (Piece.PieceCategory type in Enum.GetValues(typeof(Piece.PieceCategory)))
                    {
                        output.Add(Enum.GetName(typeof(Piece.PieceCategory), type) + " " + (int)type);
                    }
                    Dbgl("\r\n" + string.Join("\r\n", output));

                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped piece categories" }).GetValue();
                    return false;
                }
                else if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} damage"))
                {
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();

                    List<string> output = new List<string>();
                    foreach (HitData.DamageModifier type in Enum.GetValues(typeof(HitData.DamageModifier)))
                    {
                        output.Add(Enum.GetName(typeof(HitData.DamageModifier), type));
                    }
                    Dbgl("\r\n" + string.Join("\r\n", output));

                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped damage modifiers" }).GetValue();
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} save "))
                {
                    var t = text.Split(' ');
                    string pieceName = t[t.Length - 1];
                    PieceData pieceData = GetDataByName(pieceName);
                    if (pieceData == null)
                        return false;
                    CheckModFolder();
                    File.WriteAllText(Path.Combine(assetPath, pieceData.name + ".json"), JsonUtility.ToJson(pieceData));
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} saved piece data to {pieceName}.json" }).GetValue();
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()} dump "))
                {
                    var t = text.Split(' ');
                    string pieceName = t[t.Length - 1];
                    PieceData pieceData = GetDataByName(pieceName);
                    if (pieceData == null)
                        return false;
                    Dbgl("\r\n" + JsonUtility.ToJson(pieceData));
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} dumped {pieceName}" }).GetValue();
                    return false;
                }
                else if (text.ToLower().StartsWith($"{typeof(BepInExPlugin).Namespace.ToLower()}"))
                {
                    string output = $"{context.Info.Metadata.Name} reset\r\n"
                    + $"{context.Info.Metadata.Name} reload\r\n"
                    + $"{context.Info.Metadata.Name} dump <PieceName>\r\n"
                    + $"{context.Info.Metadata.Name} save <PieceName>\r\n"
                    + $"{context.Info.Metadata.Name} comfort\r\n"
                    + $"{context.Info.Metadata.Name} cats\r\n"
                    + $"{context.Info.Metadata.Name} damage";

                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { output }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
