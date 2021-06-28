using BepInEx;
using HarmonyLib;
using ModConfigEnforcer;
using System;
using System.Reflection;
using UnityEngine;

namespace RequirementCheck
{
    [BepInPlugin("aedenthorn.RequirementCheck", "Requirement Check", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigVariable<bool> modEnabled;
        public static ConfigVariable<bool> isDebug;
        public static ConfigVariable<int> nexusID;
        public static ConfigVariable<string> mineKeyReqList;
        public static ConfigVariable<string> mineItemReqList;
        public static ConfigVariable<string> craftKeyReqList;
        public static ConfigVariable<string> learnKeyReqList;
        public static ConfigVariable<string> pickupKeyReqList;
        public static ConfigVariable<string> equipKeyReqList;
        public static ConfigVariable<string> skillKeyReqList;
        public static ConfigVariable<string> teleportKeyReqList;
        public static ConfigVariable<string> teleportItemReqList;
        public static ConfigVariable<string> tameKeyReqList;
        public static ConfigVariable<string> tameItemReqList;
        
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            ConfigManager.RegisterMod(Info.Metadata.Name, Config);
            modEnabled = ConfigManager.RegisterModConfigVariable<bool>(Info.Metadata.Name, "Enabled", true, "General", "Enable this mod.", false);
            isDebug = ConfigManager.RegisterModConfigVariable<bool>(Info.Metadata.Name, "IsDebug", false, "General", "Enable this mod.", true);
            nexusID = ConfigManager.RegisterModConfigVariable<int>(Info.Metadata.Name, "NexusID", 1340, "General", "Nexus mod ID for updates", true);

            teleportKeyReqList = ConfigManager.RegisterModConfigVariable<string>(Info.Metadata.Name, "TeleportKeyReqList", "", "Lists", "Comma-separated list of global key requirements for teleportation", false);
            teleportItemReqList = ConfigManager.RegisterModConfigVariable<string>(Info.Metadata.Name, "TeleportItemReqList", "", "Lists", "Comma-separated list of carried item requirements for teleportation", false);
            skillKeyReqList = ConfigManager.RegisterModConfigVariable<string>(Info.Metadata.Name, "SkillKeyReqList", "", "Lists", "Comma-separated list of item:key global key requirements for upgrading a skill", false);
            equipKeyReqList = ConfigManager.RegisterModConfigVariable<string>(Info.Metadata.Name, "EquipKeyReqList", "", "Lists", "Comma-separated list of item:key global key requirements for equipping up an item", false);
            pickupKeyReqList = ConfigManager.RegisterModConfigVariable<string>(Info.Metadata.Name, "PickupKeyReqList", "", "Lists", "Comma-separated list of item:key global key requirements for picking up an item", false);
            craftKeyReqList = ConfigManager.RegisterModConfigVariable<string>(Info.Metadata.Name, "CraftKeyReqList", "", "Lists", "Comma-separated list of item:key global key requirements for crafting an item", false);
            learnKeyReqList = ConfigManager.RegisterModConfigVariable<string>(Info.Metadata.Name, "LearnKeyReqList", "", "Lists", "Comma-separated list of item:key global key requirement for learning a recipe", false);
            mineKeyReqList = ConfigManager.RegisterModConfigVariable<string>(Info.Metadata.Name, "MineKeyReqList", "", "Lists", "Comma-separated list of node:key mining global key requirements", false);
            mineItemReqList = ConfigManager.RegisterModConfigVariable<string>(Info.Metadata.Name, "MineItemReqList", "", "Lists", "Comma-separated list of node:item mining held item requirements", false);

            Dbgl($"mod awake");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        [HarmonyPatch(typeof(Player), "TeleportTo")]
        static class TeleportTo_Patch
        {
            static bool Prefix(Player __instance)
            {

                if (!modEnabled.Value)
                    return true;

                Dbgl("Checking teleport");

                return CheckKeyReq(teleportKeyReqList.Value) && CheckItemReq(teleportItemReqList.Value, __instance.GetInventory());
            }
        }

        [HarmonyPatch(typeof(Inventory), "CanAddItem", new Type[] { typeof(GameObject), typeof(int) })]
        static class CanPickup_Patch1
        {
            static void Postfix(GameObject prefab, ref bool __result)
            {

                if (!modEnabled.Value || prefab == null || !__result)
                    return;

                Dbgl("Checking add item 1");

                __result =  CheckNameKeyReq(prefab.name, pickupKeyReqList.Value);
            }
        }
         
        [HarmonyPatch(typeof(Inventory), "CanAddItem", new Type[] { typeof(ItemDrop.ItemData), typeof(int) })]
        static class CanPickup_Patch2
        {
            static void Postfix(ItemDrop.ItemData item, ref bool __result)
            {

                if (!modEnabled.Value || item.m_dropPrefab == null || !__result)
                    return;
                
                Dbgl("Checking add item 2");

                __result =  CheckNameKeyReq(item.m_dropPrefab.name, pickupKeyReqList.Value);
            }
        }

        [HarmonyPatch(typeof(Humanoid), "Pickup")]
        static class Pickup_Patch
        {
            static bool Prefix(GameObject go)
            {

                if (!modEnabled.Value)
                    return true;

                Dbgl("Checking pickup");

                return CheckNameKeyReq(go.name, pickupKeyReqList.Value);
            }
        }

        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        static class EquipItem_Patch
        {
            static bool Prefix(Humanoid __instance, ItemDrop.ItemData item)
            {
                if (!modEnabled.Value || !(__instance is Player))
                    return true;

                Dbgl("Checking equip");

                GameObject prefab = item.m_dropPrefab;
                if (!prefab)
                {
                    Dbgl($"no prefab for {item.m_shared.m_name}");
                    prefab = ObjectDB.instance.m_items.Find(o => o.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == item.m_shared.m_name);
                    if (!prefab)
                        return true;
                }

                return CheckNameKeyReq(item.m_dropPrefab.name, equipKeyReqList.Value);
            }
        }

        [HarmonyPatch(typeof(Player), "AddKnownRecipe")]
        static class AddKnownRecipe_Patch
        {
            static bool Prefix(Recipe recipe)
            {
                if (!modEnabled.Value || recipe.m_item.gameObject == null)
                    return true;

                Dbgl("Checking add known recipe");

                return CheckNameKeyReq(recipe.m_item.gameObject.name, learnKeyReqList.Value);
            }
        }
                
        [HarmonyPatch(typeof(Player), "AddKnownPiece")]
        static class AddKnownPiece_Patch
        {
            static bool Prefix(Piece piece)
            {
                if (!modEnabled.Value || piece.gameObject == null)
                    return true;

                Dbgl("Checking add known piece");

                return CheckNameKeyReq(piece.gameObject.name, learnKeyReqList.Value);
            }
        }

        [HarmonyPatch(typeof(Player), "AddKnownItem")]
        static class AddKnownItem_Patch
        {
            static bool Prefix(ItemDrop.ItemData item)
            {
                if (!modEnabled.Value || item.m_dropPrefab == null)
                    return true;

                Dbgl("Checking add known item");

                return CheckNameKeyReq(item.m_dropPrefab.name, learnKeyReqList.Value);
            }
        }

        [HarmonyPatch(typeof(Skills), "CheatRaiseSkill")]
        static class CheatRaiseSkill_Patch
        {
            static bool Prefix(string name)
            {

                if (!modEnabled.Value || !Enum.TryParse(name, true, out Skills.SkillType skillType))
                    return true;

                return CheckNameKeyReq(Enum.GetName(typeof(Skills.SkillType), skillType), skillKeyReqList.Value);
            }
        }

        [HarmonyPatch(typeof(Skills), "RaiseSkill")]
        static class RaiseSkill_Patch
        {
            static bool Prefix(Skills.SkillType skillType)
            {
                if (!modEnabled.Value || skillType == Skills.SkillType.None)
                    return true;

                Dbgl("Checking raise skill");

                return CheckNameKeyReq(Enum.GetName(typeof(Skills.SkillType), skillType), skillKeyReqList.Value);
            }
        }

        [HarmonyPatch(typeof(Destructible), "RPC_Damage")]
        static class Destructible_RPC_Damage_Patch
        {
            static bool Prefix(Destructible __instance, HitData hit, ZNetView ___m_nview, bool ___m_destroyed)
            {

                if (!modEnabled.Value)
                    return true;

                if (!___m_nview.IsValid() || !___m_nview.IsOwner() || ___m_destroyed)
                {
                    return false;
                }
                return CheckCanDamage(__instance.gameObject, hit);
            }
        }

        [HarmonyPatch(typeof(MineRock), "RPC_Hit")]
        static class MineRock_RPC_Hit_Patch
        {
            static bool Prefix(MineRock __instance, HitData hit, ZNetView ___m_nview)
            {

                if (!modEnabled.Value)
                    return true;
                if (!___m_nview.IsValid() || !___m_nview.IsOwner())
                {
                    return false;
                }
                return CheckCanDamage(__instance.gameObject, hit);
            }
        }

        [HarmonyPatch(typeof(MineRock5), "RPC_Damage")]
        static class MineRock5_RPC_Damage_Patch
        {
            static bool Prefix(MineRock5 __instance, HitData hit, ZNetView ___m_nview)
            {

                if (!modEnabled.Value)
                    return true;
                if (!___m_nview.IsValid() || !___m_nview.IsOwner())
                {
                    return false;
                }
                return CheckCanDamage(__instance.gameObject, hit);
            }
        }


        private static bool CheckCanDamage(GameObject gameObject, HitData hit)
        {
            Dbgl("Checking can damage");
            
            if (!CheckNameKeyReq(gameObject.name, mineKeyReqList.Value))
                return false;


            Character c = hit.GetAttacker();
            if (c is Player)
            {
                Inventory inv = (c as Player).GetInventory();
                return CheckNameItemReq(gameObject.name, mineItemReqList.Value, inv);
            }

            return true;
        }
        private static bool CheckNameItemReq(string name, string nameKeyList, Inventory inv)
        {
            if (nameKeyList.Length > 0 && inv != null)
            {
                foreach (string nameItemString in nameKeyList.Split(','))
                {
                    string[] nameItem = nameItemString.Split(':');
                    if (nameItem.Length != 2)
                        continue;

                    if (name.StartsWith(nameItem[0]) && !inv.GetAllItems().Exists(i => i.m_dropPrefab.name == nameItem[1]))
                    {
                        Dbgl($"item req failed: {nameItemString}");

                        return false;
                    }
                    else
                    {
                        Dbgl($"item req passed: {nameItemString}");
                    }
                }
            }
            return true;
        }

        private static bool CheckNameKeyReq(string name, string nameKeyList)
        {
            if (nameKeyList.Length > 0 && ZoneSystem.instance)
            {
                foreach (string nameKeyString in nameKeyList.Split(','))
                {
                    string[] nameKey = nameKeyString.Split(':');
                    if (nameKey.Length != 2)
                        continue;
                    if (name.StartsWith(nameKey[0]) && !ZoneSystem.instance.GetGlobalKey(nameKey[1]))
                    {
                        Dbgl($"key req failed: {nameKeyString}");
                        return false;
                    }
                    else
                    {
                        Dbgl($"key req passed: {nameKeyString}");
                    }
                }
            }
            return true;
        }


        private static bool CheckItemReq(string keyList, Inventory inv)
        {
            if (keyList.Length > 0 && inv != null)
            {
                foreach (string item in keyList.Split(','))
                {
                    if (!inv.GetAllItems().Exists(i => i.m_dropPrefab.name == item))
                    {
                        Dbgl($"item req failed: {item}");

                        return false;
                    }
                    else
                    {
                        Dbgl($"item req passed: {item}");
                    }
                }
            }
            return true;
        }

        private static bool CheckKeyReq(string keyList)
        {
            if (keyList.Length > 0)
            {
                foreach (string name in keyList.Split(','))
                {
                    if (!ZoneSystem.instance.GetGlobalKey(name))
                    {
                        Dbgl($"key req failed: {name}");
                        return false;
                    }
                    else
                    {
                        Dbgl($"key req passed: {name}");
                    }
                }
            }
            return true;
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
                return true;
            }
        }
    }
}
