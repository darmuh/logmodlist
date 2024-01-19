using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace logmodlist
{
    [BepInPlugin("darmuh.logmodlist", "logmodlist", "1.0.0")]

    public class logmodlist : BaseUnityPlugin
    {
        public static logmodlist instance;
        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "darmuh.logmodlist";
            public const string PLUGIN_NAME = "logmodlist";
            public const string PLUGIN_VERSION = "1.0.0";
        }

        internal static new ManualLogSource Log;

        private void Awake()
        {
            logmodlist.instance = this;
            logmodlist.Log = base.Logger;
            logmodlist.Log.LogInfo((object)"logmodlist loaded...");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

    }
}
