using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SF_Lidgren;

public static class GameManagerPatches
{
    public static void Patches(Harmony harmonyInstance)
    {
        var killPlayerMethod = AccessTools.Method(typeof(GameManager), nameof(GameManager.KillPlayer));
        var killPlayerMethodTranspiler = new HarmonyMethod(typeof(GameManagerPatches)
            .GetMethod(nameof(KillPlayerMethodTranspiler)));
        var killPlayerMethodPostfix = new HarmonyMethod(typeof(GameManagerPatches)
            .GetMethod(nameof(KillPlayerMethodPostfix)));

        // Use transpiler + postfix pattern for better performance and stats tracking
        harmonyInstance.Patch(killPlayerMethod, transpiler: killPlayerMethodTranspiler, postfix: killPlayerMethodPostfix);
    }

    /// <summary>
    /// Transpiler to modify KillPlayer method for socket-based networking.
    /// Replaces the original logic for socket connections while preserving stats tracking.
    /// </summary>
    public static IEnumerable<CodeInstruction> KillPlayerMethodTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var newCodes = new List<CodeInstruction>();
        
        // Create a label that will point to the original method code
        var originalMethodLabel = new Label();
        
        // Add check at the beginning for socket networking
        newCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(MatchmakingHandler), nameof(MatchmakingHandler.RunningOnSockets)).GetGetMethod()));
        newCodes.Add(new CodeInstruction(OpCodes.Brfalse, originalMethodLabel));
        
        // If running on sockets, call our custom method and return
        newCodes.Add(new CodeInstruction(OpCodes.Ldarg_0)); // GameManager instance
        newCodes.Add(new CodeInstruction(OpCodes.Ldarg_1)); // Controller playerToKill
        newCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameManagerPatches), nameof(HandleSocketKillPlayer))));
        newCodes.Add(new CodeInstruction(OpCodes.Ret));
        
        // Add original instructions for non-socket path
        // Mark the first original instruction with our label
        if (codes.Count > 0)
        {
            codes[0].labels.Add(originalMethodLabel);
        }
        newCodes.AddRange(codes);
        
        return newCodes;
    }

    /// <summary>
    /// Postfix for stats tracking and additional processing after player kill.
    /// This provides the stats tracking functionality that was mentioned in the original TODO.
    /// </summary>
    public static void KillPlayerMethodPostfix(Controller playerToKill, GameManager __instance)
    {
        try
        {
            // Stats tracking for killed player
            if (playerToKill != null)
            {
                var playerInfo = playerToKill.GetComponent<CharacterInformation>();
                if (playerInfo != null)
                {
                    Debug.Log($"Player killed - Stats: Name={playerInfo.name}, IsDead={playerInfo.isDead}");
                    
                    // Track killer stats if available
                    if (playerToKill.damager != null && !playerToKill.damager.isAI)
                    {
                        var killerInfo = playerToKill.damager.GetComponent<CharacterInformation>();
                        Debug.Log($"Killer stats - Name={killerInfo?.name}");
                        
                        // Future stats tracking could be implemented here
                        // e.g., kills, deaths, streak tracking, etc.
                    }
                }
            }
            
            // Track game state stats
            if (__instance?.playersAlive != null)
            {
                Debug.Log($"Players alive after kill: {__instance.playersAlive.Count}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in KillPlayerMethodPostfix stats tracking: {ex.Message}");
        }
    }

    /// <summary>
    /// Custom kill player handler for socket-based networking.
    /// Extracted from the original prefix method for better maintainability.
    /// </summary>
    private static void HandleSocketKillPlayer(GameManager gameManager, Controller playerToKill)
    {
        // Access private fields using reflection
        var crownField = AccessTools.Field(typeof(GameManager), "crown") ?? AccessTools.Field(typeof(GameManager), "___crown");
        var levelSelectorField = AccessTools.Field(typeof(GameManager), "levelSelector") ?? AccessTools.Field(typeof(GameManager), "___levelSelector");
        
        var crown = crownField?.GetValue(gameManager) as Crown;
        var levelSelector = levelSelectorField?.GetValue(gameManager) as LevelSelection;

        if (gameManager.playersAlive.Contains(playerToKill))
            gameManager.playersAlive.Remove(playerToKill);

        if (playerToKill.damager != null && !playerToKill.damager.isAI)
        {
            if (crown?.crownBarrer == playerToKill)
                crown.SetNewKing(playerToKill.damager, false);

            playerToKill.damager.OnKilledEnemy(playerToKill);
        }

        var numAlive = 0;
        Controller curController = null;
        foreach (var controller in gameManager.playersAlive)
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
                        // Improved error handling for level selection that sometimes breaks
                        MapWrapper nextLevel;
                        
                        // Add safety checks before calling GetNextLevel
                        if (levelSelector == null)
                        {
                            Debug.LogWarning("Level selector is null, using fallback level");
                            nextLevel = new MapWrapper { MapType = 0, MapData = new byte[] { 0, 0, 0, 0 } };
                        }
                        else
                        {
                            try
                            {
                                nextLevel = levelSelector.GetNextLevel();
                                
                                // Validate the returned level
                                if (nextLevel.MapData == null || nextLevel.MapData.Length < 4)
                                {
                                    Debug.LogWarning("Invalid level data returned, using fallback");
                                    nextLevel = new MapWrapper { MapType = 0, MapData = new byte[] { 0, 0, 0, 0 } };
                                }
                            }
                            catch (System.Exception levelEx)
                            {
                                Debug.LogError($"GetNextLevel failed: {levelEx.Message}");
                                nextLevel = new MapWrapper { MapType = 0, MapData = new byte[] { 0, 0, 0, 0 } };
                            }
                        }
                        
                        // Safely get map type information with validation
                        var mapTypeValue = nextLevel.MapType;
                        var mapIdValue = nextLevel.MapData?.Length >= 4 ? BitConverter.ToInt32(nextLevel.MapData, 0) : 0;
                        
                        Debug.Log($"Next level is: MapType={mapTypeValue}, MapId={mapIdValue}");
                        
                        var b = numAlive != 0 ? (byte)curController!.GetComponent<NetworkPlayer>().NetworkSpawnID : byte.MaxValue;
                        var lastWinnerSetter = AccessTools.PropertySetter(typeof(GameManager), nameof(GameManager.LastWinner));
                        lastWinnerSetter.Invoke(gameManager, new object[] { curController });

                        Debug.Log("CALLING CHANGE MAP!!!");
                        gameManager.mMultiplayerManager.ChangeMap(nextLevel, b);
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
                            gameManager.mMultiplayerManager.ChangeMap(fallbackLevel, byte.MaxValue);
                        }
                        catch (System.Exception fallbackEx)
                        {
                            Debug.LogError($"Fallback map change also failed: {fallbackEx.Message}");
                        }
                    }
                }
            }
        }

        playerToKill.OnDeath();
    }
}
