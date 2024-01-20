using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace logmodlist
{
    [BepInPlugin("TeamMLC.logmodlist", "logmodlist", "1.0.0")] //rename from logmodlist to "ModlistHashChecker" ??

    public class logmodlist : BaseUnityPlugin
    {
        public static logmodlist instance;
        public bool hashMismatch = false;
        public bool clientMismatch = false;
        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "TeamMLC.logmodlist";
            public const string PLUGIN_NAME = "logmodlist";
            public const string PLUGIN_VERSION = "1.0.0";
        }

        internal static new ManualLogSource Log;

        private void Awake()
        {
            logmodlist.instance = this;
            logmodlist.Log = base.Logger;
            logmodlist.Log.LogInfo((object)"logmodlist loaded...");
            ConfigManager.Init(Config);
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

    }
}
