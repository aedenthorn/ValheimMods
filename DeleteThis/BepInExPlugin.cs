using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DeleteThis
{
    [BepInPlugin("aedenthorn.DeleteThis", "Delete This", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<string> modKey;
        private static ConfigEntry<string> deleteKey;
        private static ConfigEntry<string> deletedMessage;
        private static ConfigEntry<int> maxDeleteDistance;
        private static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            modKey = Config.Bind<string>("General", "ModKey", "left alt", "Modifier key required to allow deletion");
            deleteKey = Config.Bind<string>("General", "ModKey", "delete", "Key used to delete stuff");
            deletedMessage = Config.Bind<string>("General", "DeletedMessage", "Deleted {0}", "Message to display after deleting.");
            maxDeleteDistance = Config.Bind<int>("General", "MaxDeleteDistance", 50, "Mod ID on the Nexus for update checks");
            nexusID = Config.Bind<int>("General", "NexusID", 0, "Mod ID on the Nexus for update checks");

            Config.Save();

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if(AedenthornUtils.CheckKeyHeld(modKey.Value, false) && AedenthornUtils.CheckKeyDown(deleteKey.Value))
            {
                RaycastHit raycastHit;
                if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out raycastHit, maxDeleteDistance.Value)
                    && Vector3.Distance(raycastHit.point, Player.m_localPlayer.m_eye.position) < maxDeleteDistance.Value)
                {

                    GameObject go = raycastHit.collider.gameObject;
                    if (go.GetComponent<ZNetView>())
                    {
                        go.GetComponent<ZNetView>().Destroy();
                    }
                    else
                        Destroy(go);
                }
            }
        }

    }
}
