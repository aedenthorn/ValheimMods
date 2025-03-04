using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DeleteThis
{
    [BepInPlugin("aedenthorn.DeleteThis", "Delete This", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<string> modKey;
        public static ConfigEntry<string> layerMaskString;
        public static ConfigEntry<string> checkKey;
        public static ConfigEntry<string> deleteKey;
        public static ConfigEntry<string> checkMessage;
        public static ConfigEntry<string> deletedMessage;
        public static ConfigEntry<int> maxDeleteDistance;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            layerMaskString = Config.Bind<string>("General", "LayerMask", "item,piece,piece_nonsolid,Default,static_solid,Default_small,character,character_net,vehicle", "List of types of things to delete, comma-separated");
            modKey = Config.Bind<string>("General", "ModKey", "left alt", "Modifier key required to allow check and deletion");
            checkKey = Config.Bind<string>("General", "CheckKey", "home", "Key used to check what will be deleted");
            deleteKey = Config.Bind<string>("General", "DeleteKey", "delete", "Key used to delete stuff");
            checkMessage = Config.Bind<string>("General", "CheckMessage", "Will delete {0}, distance {1}", "Message to display when checking.");
            deletedMessage = Config.Bind<string>("General", "DeletedMessage", "Deleted {0}, distance {1}", "Message to display after deleting.");
            maxDeleteDistance = Config.Bind<int>("General", "MaxDeleteDistance", 50, "Mod ID on the Nexus for update checks");
            nexusID = Config.Bind<int>("General", "NexusID", 0, "Mod ID on the Nexus for update checks");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        
        public void Update()
        {
            if(modEnabled.Value && Player.m_localPlayer != null && AedenthornUtils.CheckKeyHeld(modKey.Value, true) && (AedenthornUtils.CheckKeyDown(deleteKey.Value) || AedenthornUtils.CheckKeyDown(checkKey.Value)))
            {
                Dbgl($"modkey {AedenthornUtils.CheckKeyHeld(modKey.Value, true)}, del key {AedenthornUtils.CheckKeyDown(deleteKey.Value)}, check key {AedenthornUtils.CheckKeyDown(checkKey.Value)}");

                LayerMask layerMask = LayerMask.GetMask(layerMaskString.Value.Split(','));

                RaycastHit raycastHit;
                if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, maxDeleteDistance.Value)
                    && Vector3.Distance(raycastHit.point, Player.m_localPlayer.m_eye.position) < maxDeleteDistance.Value)
                {

                    Transform t = raycastHit.collider.transform;

                    while (t.parent.parent && t.parent.name != "_NetSceneRoot" && !t.name.Contains("(Clone)"))
                    {
                        Dbgl($"name: {t.name}, parent name: {t.parent.name}");
                        t = t.parent;
                    }

                    string name = Utils.GetPrefabName(t.gameObject);

                    if (AedenthornUtils.CheckKeyDown(checkKey.Value))
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(checkMessage.Value, name, Vector3.Distance(raycastHit.point, Player.m_localPlayer.m_eye.position)));
                        return;
                    }
                    else if (t.GetComponent<ZNetView>())
                    {
                        t.GetComponent<ZNetView>().Destroy();
                    }
                    else
                        Destroy(t.gameObject);
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(deletedMessage.Value, name, Vector3.Distance(raycastHit.point, Player.m_localPlayer.m_eye.position)));
                }
            }
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
                return true;
            }
        }
    }
}
