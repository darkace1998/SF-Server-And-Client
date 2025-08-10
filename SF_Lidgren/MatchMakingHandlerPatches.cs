using HarmonyLib;
using Lidgren.Network;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SF_Lidgren;

public static class MatchMakingHandlerPatches
{
    public static void Patches(Harmony harmonyInstance)
    {
        var awakeMethod = AccessTools.Method(typeof(MatchmakingHandler), "Awake");
        var awakeMethodPostfix = new HarmonyMethod(typeof(MatchMakingHandlerPatches)
            .GetMethod(nameof(AwakeMethodPostfix)));

        var getIsInsideLobbyMethod = AccessTools.Method(typeof(MatchmakingHandler), "get_IsInsideLobby");
        var getIsInsideLobbyMethodPrefix = new HarmonyMethod(typeof(MatchMakingHandlerPatches)
            .GetMethod(nameof(GetIsInsideLobbyMethodPrefix)));

        harmonyInstance.Patch(awakeMethod, postfix: awakeMethodPostfix);
        harmonyInstance.Patch(getIsInsideLobbyMethod, prefix: getIsInsideLobbyMethodPrefix);
    }

    public static void AwakeMethodPostfix(MatchmakingHandler __instance)
    {
        try
        {
            Debug.Log("Creating join server GUI...");
            var tempGUI = __instance.gameObject.AddComponent<TempGUI>();
            if (tempGUI != null)
            {
                Debug.Log("TempGUI component added successfully!");
            }
            else
            {
                Debug.LogError("Failed to add TempGUI component!");
            }

            Debug.Log("Adding MMHSockets...");
            if (!Object.FindObjectOfType<MatchMakingHandlerSockets>())
            {
                var sockets = __instance.gameObject.AddComponent<MatchMakingHandlerSockets>();
                if (sockets != null)
                {
                    Debug.Log("MatchMakingHandlerSockets component added successfully!");
                }
                else
                {
                    Debug.LogError("Failed to add MatchMakingHandlerSockets component!");
                }
            }
            else
            {
                Debug.Log("MatchMakingHandlerSockets already exists, skipping.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in AwakeMethodPostfix: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }
    }

    public static bool GetIsInsideLobbyMethodPrefix(ref bool __result) // Patch to accurately reflect info for socket connections
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;

        __result = NetworkUtils.LidgrenData.ServerConnection.Status == NetConnectionStatus.Connected;
        return false;
    }
}
