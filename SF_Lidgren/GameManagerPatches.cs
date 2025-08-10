using System;
using HarmonyLib;
using UnityEngine;

namespace SF_Lidgren;

public static class GameManagerPatches
{
    public static void Patches(Harmony harmonyInstance)
    {
        var killPlayerMethod = AccessTools.Method(typeof(GameManager), nameof(GameManager.KillPlayer));
        var killPlayerMethodPrefix = new HarmonyMethod(typeof(GameManagerPatches)
            .GetMethod(nameof(KillPlayerMethodPrefix)));

        //harmonyInstance.Patch(killPlayerMethod, prefix: killPlayerMethodPrefix);
    }

    // TODO: Switch this to transpiler + postfix (for stats) at some point
    public static bool KillPlayerMethodPrefix(ref Controller playerToKill, ref Crown ___crown, ref LevelSelection ___levelSelector, GameManager __instance)
    {
        if (!MatchmakingHandler.RunningOnSockets) return true;

        if (__instance.playersAlive.Contains(playerToKill))
            __instance.playersAlive.Remove(playerToKill);

        if (playerToKill.damager != null && !playerToKill.damager.isAI)
        {
            if (___crown.crownBarrer == playerToKill)
                ___crown.SetNewKing(playerToKill.damager, false);

            playerToKill.damager.OnKilledEnemy(playerToKill);
        }

        var numAlive = 0;
        Controller curController = null;
        foreach (var controller in __instance.playersAlive)
        {
            if (controller == null || controller.GetComponent<CharacterInformation>().isDead)
                continue;

            curController = controller;
            numAlive++;
        }

        if (numAlive <= 1)
        {
            Console.WriteLine("Less than 1 player is alive, ending round!");
            if (MatchmakingHandler.IsNetworkMatch)
            {
                if (MultiplayerManager.IsServer || MatchmakingHandler.RunningOnSockets)
                {
                    try
                    {
                        // TODO: Fix this from break sometimes
                        var nextLevel = ___levelSelector.GetNextLevel();
                        
                        // Safely get map type information with validation
                        var mapTypeValue = nextLevel.MapType;
                        var mapIdValue = nextLevel.MapData?.Length >= 4 ? BitConverter.ToInt32(nextLevel.MapData, 0) : 0;
                        
                        Debug.Log($"Next level is: MapType={mapTypeValue}, MapId={mapIdValue}");
                        
                        var b = numAlive != 0 ? (byte)curController!.GetComponent<NetworkPlayer>().NetworkSpawnID : byte.MaxValue;
                        var lastWinnerSetter = AccessTools.PropertySetter(typeof(GameManager), nameof(GameManager.LastWinner));
                        lastWinnerSetter.Invoke(__instance, new object[] { curController });

                        Debug.Log("CALLING CHANGE MAP!!!");
                        __instance.mMultiplayerManager.ChangeMap(nextLevel, b);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error during map change: {ex.Message}");
                        Debug.LogError($"Stack trace: {ex.StackTrace}");
                        
                        // Try to continue with a fallback approach
                        try
                        {
                            Debug.Log("Attempting fallback map change...");
                            var fallbackLevel = new MapWrapper { MapType = 0, MapData = new byte[] { 0, 0, 0, 0 } };
                            __instance.mMultiplayerManager.ChangeMap(fallbackLevel, byte.MaxValue);
                        }
                        catch (System.Exception fallbackEx)
                        {
                            Debug.LogError($"Fallback map change also failed: {fallbackEx.Message}");
                        }
                    }
                    //var flag = __instance.lastMapNumber.MapType == 2;
                    //GameManager.m_AnalyticsTrigger.OnMatchEnd(true, flag);
                }
            }
            //else
            //__instance.AllButOnePlayersDied();
        }

        playerToKill.OnDeath();
        return false;
    }
}
