﻿// Based on code made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using BepInEx;
using BepInEx.Logging;
using System;
using System.Reflection;

namespace ConfigurationManager
{
    internal class LegacySettingEntry : SettingEntryBase
    {
        public Type _settingType;

        public LegacySettingEntry()
        {
        }

        public override string DispName
        {
            get => string.IsNullOrEmpty(base.DispName) ? Property.Name : base.DispName;
            protected internal set => base.DispName = value;
        }

        public object Instance { get; internal set; }
        public PropertyInfo Property { get; internal set; }

        public override Type SettingType => _settingType ?? (_settingType = Property.PropertyType);

        public override object Get() => Property.GetValue(Instance, null);

        protected override void SetValue(object newVal) => Property.SetValue(Instance, newVal, null);

        /// <summary>
        /// Instance of the object that holds this setting. 
        /// Null if setting is not in a ConfigWrapper.
        /// </summary>
        public object Wrapper { get; internal set; }

        public static LegacySettingEntry FromConfigWrapper(object instance, PropertyInfo settingProp,
            BepInPlugin pluginInfo, BaseUnityPlugin pluginInstance)
        {
            try
            {
                var wrapper = settingProp.GetValue(instance, null);

                if (wrapper == null)
                {
                    BepInExPlugin.Logger.Log(LogLevel.Debug, $"Skipping ConfigWrapper entry because it's null : {instance} | {settingProp.Name} | {pluginInfo?.Name}");
                    return null;
                }

                var innerProp = wrapper.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);

                var entry = new LegacySettingEntry();
                entry.SetFromAttributes(settingProp.GetCustomAttributes(false), pluginInstance);

                if (innerProp == null)
                {
                    BepInExPlugin.Logger.Log(LogLevel.Error, "Failed to find property Value of ConfigWrapper");
                    return null;
                }

                entry.Browsable = innerProp.CanRead && innerProp.CanWrite && entry.Browsable != false;

                entry.Property = innerProp;
                entry.Instance = wrapper;

                entry.Wrapper = wrapper;

                if (entry.DispName == "Value")
                    entry.DispName = wrapper.GetType().GetProperty("Key", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(wrapper, null) as string;

                if (string.IsNullOrEmpty(entry.Category))
                {
                    var section = wrapper.GetType().GetProperty("Section", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(wrapper, null) as string;
                    if (section != pluginInfo?.GUID)
                        entry.Category = section;
                }

                var strToObj = wrapper.GetType().GetField("_strToObj",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(wrapper);
                if (strToObj != null)
                {
                    var inv = strToObj.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
                    if (inv != null)
                        entry.StrToObj = s => inv.Invoke(strToObj, new object[] { s });
                }

                var objToStr = wrapper.GetType().GetField("_objToStr",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(wrapper);
                if (objToStr != null)
                {
                    var inv = objToStr.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
                    if (inv != null)
                        entry.ObjToStr = o => inv.Invoke(objToStr, new object[] { o }) as string;
                }
                else
                {
                    entry.ObjToStr = o => o.ToString();
                }

                return entry;
            }
            catch (SystemException ex)
            {
                BepInExPlugin.Logger.Log(LogLevel.Error,
                    $"Failed to create ConfigWrapper entry : {instance} | {settingProp?.Name} | {pluginInfo?.Name} | Error: {ex.Message}");
                return null;
            }
        }

        public static LegacySettingEntry FromNormalProperty(object instance, PropertyInfo settingProp,
            BepInPlugin pluginInfo, BaseUnityPlugin pluginInstance)
        {
            var entry = new LegacySettingEntry();
            entry.SetFromAttributes(settingProp.GetCustomAttributes(false), pluginInstance);

            if (entry.Browsable == null)
                entry.Browsable = settingProp.CanRead && settingProp.CanWrite;
            entry.ReadOnly = settingProp.CanWrite;

            entry.Property = settingProp;
            entry.Instance = instance;

            return entry;
        }
    }
}