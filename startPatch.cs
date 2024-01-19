using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using BepInEx.Bootstrap;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace logmodlist
{
    [HarmonyPatch(typeof(GameNetworkManager), "Awake")]
    public class startPatch : MonoBehaviour
    {
        private static Dictionary<string, PluginInfo> PluginsLoaded = new Dictionary<string, PluginInfo>();

        static void Postfix()
        {
            logmodlist.Log.LogInfo("Creating Modlist Hash.");
            PluginsLoaded = Chainloader.PluginInfos;
            string ModListHash = DictionaryHashGenerator.GenerateHash(PluginsLoaded);
            logmodlist.Log.LogInfo("\t==========================");
            logmodlist.Log.LogInfo("\t");
            logmodlist.Log.LogInfo($"Modlist Hash: {ModListHash}");
            logmodlist.Log.LogInfo("\t");
            logmodlist.Log.LogInfo("\t==========================");
            // Log dictionary contents
            logmodlist.Log.LogInfo("[Modlist Contents]");
            logmodlist.Log.LogInfo("Mod GUID: Mod Version");

            foreach (var entry in PluginsLoaded)
            {
                logmodlist.Log.LogInfo($"{entry.Key}: {entry.Value}");
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