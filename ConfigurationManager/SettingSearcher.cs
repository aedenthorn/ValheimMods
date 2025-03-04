// Based on code made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace ConfigurationManager
{
    internal static class SettingSearcher
    {
        public static readonly ICollection<string> _updateMethodNames = new[]
        {
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnGUI"
        };

        public static void CollectSettings(out IEnumerable<SettingEntryBase> results, out List<string> modsWithoutSettings, bool showDebug)
        {
            modsWithoutSettings = new List<string>();

            try
            {
                results = GetBepInExCoreConfig();
            }
            catch (Exception ex)
            {
                results = Enumerable.Empty<SettingEntryBase>();
                BepInExPlugin.Logger.LogError(ex);
            }

            var allPlugins = Utilities.Utils.FindPlugins();

            BepInExPlugin.Dbgl($"all plugins: {allPlugins.Length}");

            foreach (var plugin in allPlugins)
            {
                if (plugin == null)
                    continue;

                //BepInExPlugin.Dbgl(plugin.name);

                if (plugin.Info.Metadata.GUID == "com.bepis.bepinex.configurationmanager" || plugin.enabled == false)
                {
                    BepInExPlugin.Dbgl($"plugin: {plugin.Info.Metadata.Name} enabled {plugin.enabled}");
                }
                //BepInExPlugin.Dbgl($"plugin: {plugin.Info.Metadata.Name} enabled {plugin.enabled}");

                var type = plugin.GetType();

                var pluginInfo = plugin.Info.Metadata;

                if (type.GetCustomAttributes(typeof(BrowsableAttribute), false).Cast<BrowsableAttribute>()
                    .Any(x => !x.Browsable))
                {
                    BepInExPlugin.Dbgl($"{pluginInfo.Name} has no settings, skipping.");
                    modsWithoutSettings.Add(pluginInfo.Name);
                    continue;
                }

                var detected = new List<SettingEntryBase>();

                detected.AddRange(GetPluginConfig(plugin).Cast<SettingEntryBase>());

                int count = detected.FindAll(x => x.Browsable == false).Count;
                if(count > 0)
                {
                    BepInExPlugin.Dbgl($"{count} settings are not browseable, removing.");
                    detected.RemoveAll(x => x.Browsable == false);
                }

                if (!detected.Any())
                {
                    BepInExPlugin.Dbgl($"{pluginInfo.Name} has no showable settings, skipping.");
                    modsWithoutSettings.Add(pluginInfo.Name);
                }

                // Allow to enable/disable plugin if it uses any update methods ------
                if (showDebug && type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(x => _updateMethodNames.Contains(x.Name)))
                {
                    // todo make a different class for it and fix access modifiers?
                    var enabledSetting = LegacySettingEntry.FromNormalProperty(plugin, type.GetProperty("enabled"), pluginInfo, plugin);
                    enabledSetting.DispName = "!Allow plugin to run on every frame";
                    enabledSetting.Description = "Disabling this will disable some or all of the plugin's functionality.\nHooks and event-based functionality will not be disabled.\nThis setting will be lost after game restart.";
                    enabledSetting.IsAdvanced = true;
                    detected.Add(enabledSetting);
                }

                if (detected.Any())
                {
                    //BepInExPlugin.Dbgl($"Adding {pluginInfo.Name} to config manager.");
                    results = results.Concat(detected);
                }
            }
        }

        /// <summary>
        /// Bepinex 5 config
        /// </summary>
        public static IEnumerable<SettingEntryBase> GetBepInExCoreConfig()
        {
            var coreConfigProp = typeof(ConfigFile).GetProperty("CoreConfig", BindingFlags.Static | BindingFlags.NonPublic);
            if (coreConfigProp == null) throw new ArgumentNullException(nameof(coreConfigProp));

            var coreConfig = (ConfigFile)coreConfigProp.GetValue(null, null);
            var bepinMeta = new BepInPlugin("BepInEx", "BepInEx", typeof(BepInEx.Bootstrap.Chainloader).Assembly.GetName().Version.ToString());

            return coreConfig
                .Select(x => new ConfigSettingEntry(x.Value, null) { IsAdvanced = true, PluginInfo = bepinMeta })
                .Cast<SettingEntryBase>();
        }

        /// <summary>
        /// Used by bepinex 5 plugins
        /// </summary>
        public static IEnumerable<ConfigSettingEntry> GetPluginConfig(BaseUnityPlugin plugin)
        {
            return plugin.Config.Select(x => new ConfigSettingEntry(x.Value, plugin));
        }
    }
}