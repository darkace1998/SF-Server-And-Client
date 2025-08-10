using System;
using BepInEx;
using HarmonyLib;

namespace SF_Lidgren;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string AppIdentifier = "monky.SF_Lidgren";

    private void Awake()
    {
        try
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Logger.LogInfo("Preparing patches for SF_Lidgren...");

            // Initialize safe application defaults to prevent issues
            InitializeSafeDefaults();

            Harmony harmony = new(AppIdentifier); // Creates harmony instance with identifier

            Logger.LogInfo("Applying MatchmakingHandlerSockets patches...");
            MatchmakingHandlerSocketsPatches.Patches(harmony);
            Logger.LogInfo("Applying GameManager Patches...");
            GameManagerPatches.Patches(harmony);
            Logger.LogInfo("Applying MatchmakingHandler patch...");
            MatchMakingHandlerPatches.Patches(harmony);
            Logger.LogInfo("Applying MultiplayerManagerSockets Patches...");
            MultiplayerManagerSocketsPatches.Patches(harmony);
            Logger.LogInfo("Applying MultiplayerManager Patch...");
            MultiplayerManagerPatches.Patch(harmony);
            Logger.LogInfo("Applying P2PPackageHandler Patches...");
            P2PPackageHandlerPatch.Patches(harmony);
            Logger.LogInfo("Applying NetworkPlayer Patches...");
            NetworkPlayerPatches.Patches(harmony);
            Logger.LogInfo("Applying PauseManager Patches...");
            PauseManagerPatches.Patches(harmony);
            
            Logger.LogInfo("SF_Lidgren plugin initialization completed successfully!");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize SF_Lidgren plugin: {ex.Message}");
            Logger.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    private void InitializeSafeDefaults()
    {
        try
        {
            // Try to access Application properties safely to prevent Harmony warnings
            var appType = typeof(UnityEngine.Application);
            
            // Safely check if isBatchMode exists before accessing it
            var batchModePropertyInfo = appType.GetProperty("isBatchMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (batchModePropertyInfo == null)
            {
                Logger.LogWarning("Application.isBatchMode property not found in this Unity version, skipping batch mode checks");
            }
            
            // Set safe framerate defaults
            if (UnityEngine.Application.targetFrameRate <= 0)
            {
                UnityEngine.Application.targetFrameRate = 60;
                Logger.LogInfo("Set default target framerate to 60 FPS");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Could not initialize application defaults: {ex.Message}");
        }
    }
}
