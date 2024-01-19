using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using BepInEx.Bootstrap;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using BepInEx.Configuration;

namespace logmodlist
{

    public class ConfigManager
    {
        public static ConfigManager Instance { get; private set; }

        public static void Init(ConfigFile config)
        {
            Instance = new ConfigManager(config);
        }

        public static ConfigEntry<string> ExpectedModListHash { get; private set; }

        private ConfigManager(ConfigFile config)
        {
            ExpectedModListHash = config.Bind("General", "ExpectedModListHash", "", "The expected modlist hash for this modpack. Do not change this unless you know what you're doing.");
        }

    }

    [HarmonyPatch(typeof(GameNetworkManager))]
    public class startPatch : MonoBehaviour
    {
        private static Dictionary<string, PluginInfo> PluginsLoaded = new Dictionary<string, PluginInfo>();
        public static string generatedHash = "";

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void Postfix()
        {
            logmodlist.Log.LogInfo("Creating Modlist Hash.");
            PluginsLoaded = Chainloader.PluginInfos;
            generatedHash = DictionaryHashGenerator.GenerateHash(PluginsLoaded);

            logmodlist.Log.LogInfo("==========================");
            logmodlist.Log.LogInfo($"Modlist Hash: {generatedHash}");

            if (ConfigManager.ExpectedModListHash.Value != "")
            {
                logmodlist.Log.LogInfo($"Expected Hash (from modpack): {ConfigManager.ExpectedModListHash.Value}");

                if (generatedHash == ConfigManager.ExpectedModListHash.Value)
                {
                    logmodlist.Log.LogWarning("Your modlist matches the expected modlist hash.");
                }
                else
                {
                    logmodlist.Log.LogError("Your modlist does not match the expected modlist hash.");
                    logmodlist.Log.LogWarning("You may experience issues.");
                }
            }
            else
            {
                logmodlist.Log.LogWarning("No expected hash found");
            }

            logmodlist.Log.LogInfo("==========================");

            // Log dictionary contents
            logmodlist.Log.LogInfo("[Modlist Contents]");
            logmodlist.Log.LogInfo("Mod GUID: Mod Version");

            foreach (var entry in PluginsLoaded)
            {
                logmodlist.Log.LogInfo($"{entry.Key}: {entry.Value}");
            }

            logmodlist.Log.LogInfo("==========================");
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager))]
    public class LobbyCreatedPatch
    {

        [HarmonyPatch("SteamMatchmaking_OnLobbyCreated")]
        [HarmonyPostfix]
        static void Postfix(Result result, ref Lobby lobby)
        {
            if (result != Result.OK) return;

            lobby.SetData("ModListHash", DictionaryHashGenerator.GenerateHash(Chainloader.PluginInfos));
            logmodlist.Log.LogInfo($"Setting lobby ModHashList to {startPatch.generatedHash}");
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager))]
    public class LobbyJoinPatch
    {

        [HarmonyPatch("StartClient")]
        [HarmonyPostfix]
        static void Postfix(SteamId id)
        {
            logmodlist.Log.LogInfo("Comparing your modlist with the host's modlist.");

            var lobbyModList = GameNetworkManager.Instance.currentLobby?.GetData("ModListHash");
            if (lobbyModList == null)
            {
                logmodlist.Log.LogWarning("Host does not have a modlist hash.");
                return;
            }
            else
            {
                logmodlist.Log.LogInfo($"Host's modlist hash: {lobbyModList}");
                logmodlist.Log.LogInfo($"Your modlist hash: {startPatch.generatedHash}");

                if (lobbyModList == startPatch.generatedHash)
                {
                    logmodlist.Log.LogInfo("Your modlist matches the host's modlist.");
                }
                else
                {
                    logmodlist.Log.LogWarning("Your modlist does not match the host's modlist.");
                    logmodlist.Log.LogWarning("You may experience issues.");
                }
            }

        }
    }

    public class DictionaryHashGenerator
    {
        public static string GenerateHash(Dictionary<string, BepInEx.PluginInfo> inputDictionary)
        {
            // Sort the values of the dictionary by key to ensure consistent order
            var sortedEntries = inputDictionary.OrderBy(entry => entry.Key);

            // Concatenate the sorted key-value pairs into a single string
            string concatenatedString = string.Join(",", sortedEntries.Select(entry => $"{entry.Key}:{entry.Value}"));

            // Convert the string to bytes
            byte[] inputBytes = Encoding.UTF8.GetBytes(concatenatedString);

            // Compute the hash
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // Convert the hash to a hexadecimal string
                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }
    }

}