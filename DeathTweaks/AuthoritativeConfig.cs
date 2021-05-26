using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System;
using HarmonyLib;

namespace AuthoritativeConfig
{
    public class Config
    {
        private static Config _instance = null;
        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Config();
                }
                return _instance;
            }
            set { }
        }
        public static ZNet ZNet => ZNet.instance;
        public Dictionary<string, ConfigBaseEntry> _configEntries;
        public BaseUnityPlugin _mod;

        public static string GUID => Instance._mod.Info.Metadata.GUID;
        public static string RPC_SYNC_GUID => "AuthoritativeConfig_" + Config.GUID;
        private static BepInEx.Configuration.ConfigEntry<bool> _ServerIsAuthoritative;
        private static bool _DefaultBindAuthority;
        public static BepInEx.Logging.ManualLogSource Logger;

        public void init(BaseUnityPlugin mod, bool defaultBindServerAuthority = false)
        {
            _mod = mod;
            //logger
            Logger = new BepInEx.Logging.ManualLogSource(RPC_SYNC_GUID);
            BepInEx.Logging.Logger.Sources.Add(Logger);

            _configEntries = new Dictionary<string, ConfigBaseEntry>();
            _DefaultBindAuthority = defaultBindServerAuthority;
            _ServerIsAuthoritative = _mod.Config.Bind("ServerAuthoritativeConfig", "ServerIsAuthoritative", true, "<Server Only> Forces Clients to use Server defined configs.");
            Harmony.CreateAndPatchAll(typeof(Config));
            Logger.LogInfo("Initialized Server Authoritative Config Manager.");
        }

        #region Harmony_Hooks
        [HarmonyPatch(typeof(Game), "Start")]
        [HarmonyPostfix]
        private static void RegisterSyncConfigRPC()
        {
            Logger.LogInfo($"Authoritative Config Registered -> {Config.RPC_SYNC_GUID}");
            ZRoutedRpc.instance.Register(Config.RPC_SYNC_GUID, new Action<long, ZPackage>(Config.RPC_SyncServerConfig));
            //clear server values
            foreach (ConfigBaseEntry entry in Config.Instance._configEntries.Values)
            {
                entry.ClearServerValue();
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
        [HarmonyPostfix]
        private static void RequestConfigFromServer()
        {
            if (!ZNet.IsServer() && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
            {
                long? serverPeerID = AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID").Invoke(ZRoutedRpc.instance, null) as long?;
                ZRoutedRpc.instance.InvokeRoutedRPC((long)serverPeerID, Config.RPC_SYNC_GUID, new object[] { new ZPackage() });
                Logger.LogInfo($"Authoritative Config Registered -> {Config.RPC_SYNC_GUID}");
                Debug.Log(Config.Instance._mod.Info.Metadata.Name + ": Authoritative Config Requested -> " + Config.RPC_SYNC_GUID);
            }
            else if (!ZNet.IsServer())
            {
                Logger.LogWarning($"Failed to Request Configs. Bad Peer? Too Early?");
            }
        }
        #endregion

        #region Bind_Impl
        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, ConfigDescription configDescription = null, bool? serverAuthoritative = null)
        {
            ConfigEntry<T> entry = new ConfigEntry<T>(_mod.Config.Bind(section, key, defaultValue, configDescription), serverAuthoritative != null ? (bool)serverAuthoritative : _DefaultBindAuthority);
            _configEntries[entry.BaseEntry.Definition.ToString()] = entry;
            return entry;
        }

        public ConfigEntry<T> Bind<T>(ConfigDefinition configDefinition, T defaultValue, ConfigDescription configDescription = null, bool? serverAuthoritative = null)
        {
            ConfigEntry<T> entry = new ConfigEntry<T>(_mod.Config.Bind(configDefinition, defaultValue, configDescription), serverAuthoritative != null ? (bool)serverAuthoritative : _DefaultBindAuthority);
            _configEntries[entry.BaseEntry.Definition.ToString()] = entry;
            return entry;
        }

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description, bool? serverAuthoritative = null)
        {
            ConfigEntry<T> entry = new ConfigEntry<T>(_mod.Config.Bind(section, key, defaultValue, description), serverAuthoritative != null ? (bool)serverAuthoritative : _DefaultBindAuthority);
            _configEntries[entry.BaseEntry.Definition.ToString()] = entry;
            return entry;
        }
        #endregion

        #region RPC
        public static void SendConfigToClient(long sender)
        {
            if (ZNet.IsServer())
            {
                ZPackage pkg = new ZPackage();
                int entries = 0;
                foreach (var item in Config.Instance._configEntries)
                {
                    if (item.Value.ServerAuthoritative)
                    {
                        pkg.Write(item.Key);
                        pkg.Write(item.Value.BaseEntry.GetSerializedValue());
                        entries++;
                        Logger.LogInfo($"Sending Config {item.Key}: {item.Value.BaseEntry.GetSerializedValue()}");
                    }
                }
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, Config.RPC_SYNC_GUID, new object[] { pkg });
                Logger.LogInfo($"Sent {entries} config pairs to client {sender}");
            }
        }

        public static void ReadConfigPkg(ZPackage pkg)
        {
            if (!ZNet.IsServer())
            {
                int entries = 0;
                while (pkg.GetPos() != pkg.Size())
                {
                    string configKey = pkg.ReadString();
                    string stringVal = pkg.ReadString();
                    entries++;
                    if (Config.Instance._configEntries.ContainsKey(configKey))
                    {
                        Config.Instance._configEntries[configKey].SetSerializedValue(stringVal);
                        Logger.LogInfo($"Applied Server Authoritative config pair => {configKey}: {stringVal}");
                    }
                    else
                    {
                        Logger.LogError($"Recieved config key we dont have locally. Possible Version Mismatch. {configKey}: {stringVal}");
                    }
                }
                Logger.LogInfo($"Applied {entries} config pairs");
            }
        }

        public static void RPC_SyncServerConfig(long sender, ZPackage pkg)
        {
            if (ZNet.IsServer() && _ServerIsAuthoritative.Value)
            {
                SendConfigToClient(sender);
            }
            else if (!ZNet.IsServer() && pkg != null && pkg.Size() > 0)
            {
                //Only read configs from the server.
                long? serverPeerID = AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID").Invoke(ZRoutedRpc.instance, null) as long?;
                if (serverPeerID == sender)
                {
                    //Client handle recieving config
                    ReadConfigPkg(pkg);
                }
            }
        }
        #endregion
    }

    public class ConfigBaseEntry
    {
        protected object _serverValue = null;
        public BepInEx.Configuration.ConfigEntryBase BaseEntry;
        public bool ServerAuthoritative;
        protected bool _didError = false;

        internal ConfigBaseEntry(BepInEx.Configuration.ConfigEntryBase configEntry, bool serverAuthoritative)
        {
            BaseEntry = configEntry;
            ServerAuthoritative = serverAuthoritative;
        }

        public void SetSerializedValue(string value)
        {
            try
            {
                object tmp = (_serverValue = TomlTypeConverter.ConvertToValue(value, BaseEntry.SettingType));
                _didError = false;
            }
            catch (Exception ex)
            {
                Config.Logger.LogWarning($"Config value of setting \"{BaseEntry.Definition}\" could not be parsed and will be ignored. Reason: {ex.Message}; Value: {value}");
            }
        }

        public void ClearServerValue()
        {
            _serverValue = null;
            _didError = false;
        }
    }

    public class ConfigEntry<T> : ConfigBaseEntry
    {
        public T ServerValue => (T)_serverValue;
        private BepInEx.Configuration.ConfigEntry<T> _configEntry;

        internal ConfigEntry(BepInEx.Configuration.ConfigEntry<T> configEntry, bool serverAuthoritative) : base(configEntry, serverAuthoritative)
        {
            _configEntry = configEntry;
        }

        public T Value
        {
            get
            {
                //Todo: Extended behaviour for value selection?
                if (ServerAuthoritative && !Config.ZNet.IsServer())
                {
                    if (_serverValue != null)
                    {
                        return ServerValue;
                    }
                    else
                    {
                        if (!_didError)
                        {
                            Config.Logger.LogWarning($"No Recieved value for Server Authoritative Config. {BaseEntry.Definition.ToString()}. Falling Back to Client Config.");
                            _didError = true;
                        }
                        return _configEntry.Value;
                    }
                }
                return _configEntry.Value;
            }
            set
            {
                _configEntry.Value = value;
            }
        }
    }
}