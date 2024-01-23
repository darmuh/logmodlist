using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ModListHashChecker
{
        [HarmonyPatch(typeof(PreInitSceneScript))]
        public class HashGeneration : MonoBehaviour
        {
            private static Dictionary<string, PluginInfo> PluginsLoaded = new Dictionary<string, PluginInfo>();
            public static string generatedHash { get; internal set; } = "";

            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            static void Postfix()
            {
                ModListHashChecker.Log.LogInfo("Creating Modlist Hash.");
                PluginsLoaded = Chainloader.PluginInfos;
                generatedHash = DictionaryHashGenerator.GenerateHash(PluginsLoaded);

                ModListHashChecker.Log.LogInfo("==========================");
                ModListHashChecker.Log.LogInfo($"Modlist Hash: {generatedHash}");

                /*    if (ConfigManager.AutosetModListHash.Value)
                    {
                        ModListHashChecker.Log.LogInfo("Overriding expected hash to the current hash");
                        ConfigManager.ExpectedModListHash.Value = generatedHash;
                        ConfigManager.config.Save();
                    }*/

                if (ConfigManager.ExpectedModListHash.Value != "")
                {
                    ModListHashChecker.Log.LogInfo($"Expected Hash (from modpack): {ConfigManager.ExpectedModListHash.Value}");

                    if (generatedHash == ConfigManager.ExpectedModListHash.Value)
                    {
                        ModListHashChecker.Log.LogWarning("Your modlist matches the expected modlist hash.");
                    }
                    else
                    {
                        ModListHashChecker.Log.LogError("Your modlist does not match the expected modlist hash.");
                        ModListHashChecker.Log.LogWarning("You may experience issues.");
                        ModListHashChecker.instance.hashMismatch = true;
                    }
                }
                else
                {
                    ModListHashChecker.Log.LogWarning("No expected hash found");
                    if (ConfigManager.NoExpectedHashMessage.Value)
                        ModListHashChecker.instance.noHashFound = true;
                }

                ModListHashChecker.Log.LogInfo("==========================");

                // Log dictionary contents
                ModListHashChecker.Log.LogInfo("[Modlist Contents]");
                ModListHashChecker.Log.LogInfo("Mod GUID: Mod Version");

                foreach (var entry in PluginsLoaded)
                {
                    ModListHashChecker.Log.LogInfo($"{entry.Key}: {entry.Value}");
                }

                ModListHashChecker.Log.LogInfo("==========================");
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
}
