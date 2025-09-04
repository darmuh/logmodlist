﻿using BepInEx.Bootstrap;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModListHashChecker;
internal class GamePatching
{
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SteamMatchmaking_OnLobbyCreated))]
    public class LobbyCreatedPatch
    {
        static void Postfix(Result result, ref Lobby lobby)
        {
            if (result != Result.OK) return;

            lobby.SetData("ModListHash", DictionaryHashGenerator.GenerateHash(Chainloader.PluginInfos));
            ModListHashChecker.Log.LogInfo($"Setting lobby ModHashList to {HashGeneration.GeneratedHash}");
        }
    }

    [HarmonyPatch(typeof(RoundManager),nameof(RoundManager.FinishGeneratingLevel))]
    internal class DisplayHashPerRound
    {
        static void Postfix()
        {
            if(HUDManager.Instance == null || !ConfigManager.DisplayHashOnLevelLoad.Value)
                return;

            if (ConfigManager.ChatHashMessageToAll.Value)
                HUDManager.Instance.AddTextToChatOnServer($"{StartOfRound.Instance.localPlayerController.playerUsername} ModListHash: {HashGeneration.GeneratedHash}");
            else
                HUDManager.Instance.AddChatMessage($"Local ModListHash: {HashGeneration.GeneratedHash}", "", -1, true);
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    public class StartRoundPatch
    {
        public static void Postfix()
        {
            if (!ModListHashChecker.instance.ClientMismatch)
                return;

            ModListHashChecker.Log.LogInfo($"hash mismatch detected");
            ModListHashChecker.instance.StartCoroutine(WarningMessage());

        }

        private static IEnumerator WarningMessage()
        {
            yield return new WaitForSeconds(ConfigManager.JoinWarningDelay.Value);
            HUDManager.Instance.DisplayTip("Modlist Hash Mismatch", $"{ConfigManager.JoinWarningText.Value}", false, false, "clientHashMismatch");
        }
    }


    [HarmonyPatch(typeof(GameNetworkManager), (nameof(GameNetworkManager.StartClient)))]
    public class LobbyJoinPatch
    {
        static void Postfix()
        {
            ModListHashChecker.Log.LogInfo("Comparing your modlist with the host's modlist.");

            var lobbyModList = GameNetworkManager.Instance.currentLobby?.GetData("ModListHash");
            if (lobbyModList == null)
            {
                ModListHashChecker.Log.LogWarning("Host does not have a modlist hash.");
                return;
            }
            else
            {
                ModListHashChecker.Log.LogInfo($"Host's modlist hash: {lobbyModList}");
                ModListHashChecker.Log.LogInfo($"Your modlist hash: {HashGeneration.GeneratedHash}");

                if (lobbyModList == HashGeneration.GeneratedHash)
                {
                    ModListHashChecker.Log.LogInfo("Your modlist matches the host's modlist!");
                }
                else
                {
                    ModListHashChecker.Log.LogWarning("Your modlist does not match the host's modlist.");
                    ModListHashChecker.Log.LogWarning("You may experience issues.");
                    ModListHashChecker.instance.ClientMismatch = true;
                }
            }

        }
    }

    [HarmonyPatch(typeof(PreInitSceneScript), (nameof(PreInitSceneScript.Awake)))]
    public class HashGeneration : MonoBehaviour
    {
        public static string GeneratedHash { get; internal set; } = "";

        static void Postfix()
        {
            ModListHashChecker.Log.LogInfo("Creating Modlist Hash.");
            var pluginsLoaded = Chainloader.PluginInfos;
            GeneratedHash = DictionaryHashGenerator.GenerateHash(pluginsLoaded);

            ModListHashChecker.Log.LogInfo("==========================");
            ModListHashChecker.Log.LogInfo($"Modlist Hash: {GeneratedHash}");

            if (!string.IsNullOrEmpty(ConfigManager.ExpectedModListHash.Value))
            {
                ModListHashChecker.Log.LogInfo($"Expected Hash (from modpack): {ConfigManager.ExpectedModListHash.Value}");

                if (GeneratedHash == ConfigManager.ExpectedModListHash.Value)
                {
                    ModListHashChecker.Log.LogMessage("Your modlist matches the expected modlist hash.");
                }
                else
                {
                    ModListHashChecker.Log.LogWarning("Your modlist does not match the expected modlist hash.\nYou may experience issues.");
                    ModListHashChecker.instance.HashMismatch = true;
                }
            }
            else
            {
                ModListHashChecker.Log.LogMessage("No expected hash found");
                if (ConfigManager.NoExpectedHashMessage.Value)
                    ModListHashChecker.instance.NoHashFound = true;
            }

            ModListHashChecker.Log.LogInfo("==========================");

            // Log dictionary contents
            ModListHashChecker.Log.LogInfo("[Modlist Contents]");
            ModListHashChecker.Log.LogInfo("Mod GUID: Mod Version");

            foreach (var entry in pluginsLoaded)
            {
                ModListHashChecker.Log.LogInfo($"{entry.Key}: {entry.Value}");
            }

            ModListHashChecker.Log.LogInfo("==========================");
        }
    }

    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.OnEnable))]
    public class EnablePatch : MonoBehaviour
    {
        public static void Postfix(ref MenuManager __instance)
        {
            if (ModListHashChecker.instance.HashMismatch && ConfigManager.MenuWarning.Value)
            {
                MenuMessage(__instance, ConfigManager.WarningButtonResetText.Value, ConfigManager.WarningButtonIgnoreText.Value, ConfigManager.WarningMessageText.Value);
            }
            else if (ModListHashChecker.instance.NoHashFound && ConfigManager.NoExpectedHashMessage.Value)
            {
                MenuMessage(__instance, ConfigManager.NoHashLeftButtonText.Value, ConfigManager.NoHashRightButtonText.Value, ConfigManager.NoHashMessageText.Value);
            }
            else
                ModListHashChecker.Log.LogInfo($"Not sending any messages");
        }

        static void ResetConfigHash()
        {
            ModListHashChecker.Log.LogInfo($"Setting expected hash to current hash.");
            ConfigManager.ExpectedModListHash.Value = HashGeneration.GeneratedHash;
            ModListHashChecker.instance.HashMismatch = false;
            ModListHashChecker.instance.NoHashFound = false;
        }

        internal static GameObject NewNotification = null!;
        internal static Button FirstButton = null!;
        internal static Button SecondButton = null!;
        internal static TextMeshProUGUI MenuText = null!;

        private static void MenuSetup(MenuManager menuInstance)
        {
            if (NewNotification != null && FirstButton != null && SecondButton != null && MenuText != null)
                return;

            NewNotification = Instantiate(menuInstance.menuNotification, menuInstance.menuNotification.transform.parent);

            FirstButton = NewNotification.GetComponentInChildren<Button>();
            SecondButton = Instantiate(FirstButton, NewNotification.transform);

            MenuText = NewNotification.transform.Find("Panel").Find("NotificationText").gameObject.GetComponent<TextMeshProUGUI>();
        }

        static void MenuMessage(MenuManager menuInstance, string leftButton, string rightButton, string messageText)
        {
            if (menuInstance == null)
                return;

            if (menuInstance.menuNotification == null)
                return;

            MenuSetup(menuInstance);

            if (NewNotification == null || SecondButton == null)
                return;

            TextMeshProUGUI buttonText = SecondButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = $"[ {leftButton} ]";
            SecondButton.onClick.AddListener(ResetConfigHash);

            if (menuInstance.isInitScene)
                return;

            ModListHashChecker.Log.LogDebug("Displaying menu notification: " + messageText);

            MenuText.text = messageText;

            FirstButton.GetComponentInChildren<TextMeshProUGUI>().text = $"[ {rightButton} ]";

            NewNotification.SetActive(value: true);
            Vector3 movePosRight = new(62, -45, 0); //got these values via trial/error
            Vector3 movePosLeft = new(-78, -45, 0); //^
            for (int i = 0; i < NewNotification.GetComponentsInChildren<Button>().Length; i++)
            {
                if (i == 0)
                {
                    EventSystem.current.SetSelectedGameObject(NewNotification.GetComponentsInChildren<Button>()[i].gameObject);
                    NewNotification.GetComponentsInChildren<Button>()[i].gameObject.transform.localPosition = movePosRight;
                }
                else
                    NewNotification.GetComponentsInChildren<Button>()[i].gameObject.transform.localPosition = movePosLeft;
            }
        }
    }
}
