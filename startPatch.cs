using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using System.Threading;
using BepInEx.Bootstrap;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using BepInEx.Configuration;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using TMPro;


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
        public static ConfigEntry<bool> MenuWarning { get; private set; }
        public static ConfigEntry<bool> JoinWarning { get; private set; }
        public static ConfigEntry<string> WarningMessageText { get; private set; }
        public static ConfigEntry<string> JoinWarningText { get; private set; }
        public static ConfigEntry<int> JoinWarningDelay { get; private set; }
        public static ConfigEntry<string> WarningButtonIgnoreText { get; private set; }
        public static ConfigEntry<string> WarningButtonResetText { get; private set; }

        private ConfigManager(ConfigFile config)
        {
            ExpectedModListHash = config.Bind("General", "ExpectedModListHash", "", "The expected modlist hash for this modpack. Do not change this unless you know what you're doing.");
            MenuWarning = config.Bind("General", "MenuWarning", true, "Enable or Disable displaying a warning message in the menus when the hash does not match the expected hash.");
            JoinWarning = config.Bind("General", "JoinWarning", true, "Enable or Disable displaying a warning message when a client joins and the hash does not match the host hash.");
            JoinWarningText = config.Bind("Join Warning", "JoinWarningText", "Your modlist does not match the expected modlist hash.\n\n You may experience issues.", "Message to display in Hash Mismatch Menu Warning Message");
            JoinWarningDelay = config.Bind("Join Warning", "JoinWarningDelay", 5, "Seconds delay until hud warning is displayed for mismatched hash on client join.");
            WarningMessageText = config.Bind("Menu Warning", "WarningMessageText", "Your modlist does not match the expected modlist hash.\n\n You may experience issues.", "Message to display in Hash Mismatch Menu Warning Message");
            WarningButtonIgnoreText = config.Bind("Menu Warning", "WarningButtonIgnoreText", "Okay", "Button text for ignoring the Hash Mismatch Menu Warning Message");
            WarningButtonResetText = config.Bind("Menu Warning", "WarningButtonResetText", "Reset", "Button text for reseting ExpectedModListHash to the detected hash in Hash Mismatch Menu Warning Message");
        }

    }

    [HarmonyPatch(typeof(PreInitSceneScript))]
    public class startPatch : MonoBehaviour
    {
        private static Dictionary<string, PluginInfo> PluginsLoaded = new Dictionary<string, PluginInfo>();
        public static string generatedHash { get; internal set; } = "";

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
                    logmodlist.instance.hashMismatch = true;
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

    [HarmonyPatch(typeof(MenuManager), "OnEnable")]
    public class EnablePatch : MonoBehaviour
    {
        public static void Postfix(ref MenuManager __instance)
        {
            if (logmodlist.instance.hashMismatch && ConfigManager.MenuWarning.Value)
            {
                if(__instance != null && __instance.menuNotification != null)
                {
                    // cloning the one button from notification
                    Button setNewHash = Instantiate(__instance.menuNotification.GetComponentInChildren<Button>());

                    // set parent of cloned button to menuNotif
                    setNewHash.transform.SetParent(__instance.menuNotification.transform, false);
                    TextMeshProUGUI clonedText = setNewHash.GetComponentInChildren<TextMeshProUGUI>();
                    clonedText.text = $"[ {ConfigManager.WarningButtonResetText.Value} ]";

                    setNewHash.onClick.AddListener(resetConfigHash);

                    if (!__instance.isInitScene)
                    {
                        Debug.Log("Displaying menu notification: " + ConfigManager.WarningMessageText.Value);
                        __instance.menuNotificationText.text = ConfigManager.WarningMessageText.Value;
                        __instance.menuNotificationButtonText.text = $"[ {ConfigManager.WarningButtonIgnoreText.Value} ]";

                        __instance.menuNotification.SetActive(value: true);
                        Vector3 movePosRight = new Vector3(62, -45, 0); //got these values via trial/error
                        Vector3 movePosLeft = new Vector3(-78, -45, 0); //^
                        for (int i = 0; i < __instance.menuNotification.GetComponentsInChildren<Button>().Length; i++)
                        {
                            if (i == 0)
                            {
                                EventSystem.current.SetSelectedGameObject(__instance.menuNotification.GetComponentsInChildren<Button>()[i].gameObject);
                                __instance.menuNotification.GetComponentsInChildren<Button>()[i].gameObject.transform.localPosition = movePosRight;
                            }
                            else
                                __instance.menuNotification.GetComponentsInChildren<Button>()[i].gameObject.transform.localPosition = movePosLeft;

                        }
                    }
                    //__instance.DisplayMenuNotification(ConfigManager.WarningMessageText.Value, ConfigManager.WarningButtonText.Value);
                    logmodlist.Log.LogInfo($"hash mismatch message sent");

                    return;
                }
                else
                    logmodlist.Log.LogInfo($"handling null menuNotification");

            }
            else
                logmodlist.Log.LogInfo($"No hash mismatch detected");
        }

        static void resetConfigHash()
        {
            logmodlist.Log.LogInfo($"config hash reset");
            ConfigManager.ExpectedModListHash.Value = startPatch.generatedHash;
            logmodlist.instance.hashMismatch = false;
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "Start")]
    public class StartRoundPatch
    {
        static int delayTask = ConfigManager.JoinWarningDelay.Value * 1000;
        public static void Postfix()
        {
            if (logmodlist.instance.clientMismatch)
            {
                logmodlist.Log.LogInfo($"hash mismatch detected");
                Task.Run(() =>
                {
                    Thread.Sleep(delayTask);
                    logmodlist.Log.LogInfo($"display warning");
                    HUDManager.Instance.DisplayTip("Modlist Hash Mismatch", $"{ConfigManager.JoinWarningText.Value}", false, false, "clientHashMismatch");
                });
            }
            else
                logmodlist.Log.LogInfo($"no hash mismatch");

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
                    logmodlist.instance.clientMismatch = true;
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
