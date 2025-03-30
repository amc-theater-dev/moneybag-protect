﻿using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoneybagProtect
{
    [BepInPlugin("mod.moneybagprotect", "Moneybag Protect", "1.0.5")]
    public class MoneybagProtect : BaseUnityPlugin
    {
        public static MoneybagProtect Instance { get; private set; }
        public static ManualLogSource LogInstance { get; private set; }
        private readonly Harmony harmony = new Harmony("mod.moneybagprotect");

        private void Awake()
        {
            Instance = this;
            LogInstance = Logger;
            Logger.LogInfo("Moneybag Protect mod loaded");
            harmony.PatchAll();
        }
    }

    // patch to modify SurplusValuable.Start() to extend indestructibleTimer to 10 seconds and log the change
    [HarmonyPatch(typeof(SurplusValuable), "Start")]
    public class SurplusValuableStartPatch
    {
        static void Postfix(SurplusValuable __instance)
        {
            // locate the private field 'indestructibleTimer'
            FieldInfo timerField = typeof(SurplusValuable).GetField("indestructibleTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (timerField != null)
            {
                // set it to 10 seconds
                timerField.SetValue(__instance, 10f);
                // retrieve the updated value
                float updatedTimer = (float)timerField.GetValue(__instance);
                MoneybagProtect.LogInstance.LogInfo($"Moneybag Protect: indestructibleTimer changed to {updatedTimer} for SurplusValuable instance");
            }
            else
            {
                MoneybagProtect.LogInstance.LogError("Moneybag Protect: Couldn't locate 'indestructibleTimer' in SurplusValuable");
            }
        }
    }

    // patch to add a visual indicator based on the indestructibleTimer
    [HarmonyPatch(typeof(SurplusValuable), "Update")]
    public class SurplusValuableUpdatePatch
    {
        private static Color originalColor;
        private static bool colorCaptured = false;

        static void Postfix(SurplusValuable __instance)
        {
            // locate indestructibleTimer
            FieldInfo timerField = typeof(SurplusValuable).GetField("indestructibleTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (timerField != null)
            {
                // retrieve the current value
                float currentTimer = (float)timerField.GetValue(__instance);

                // locate the Renderer component in the object or children
                Renderer renderer = __instance.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    // capture the original color if not already captured
                    if (!colorCaptured)
                    {
                        originalColor = renderer.material.GetColor("_Color");
                        colorCaptured = true;
                    }

                    // conditional to make sure timer is active
                    if (currentTimer > 0)
                    {
                        // set the color to a glowing cyan
                        renderer.material.SetColor("_EmissionColor", Color.cyan * Mathf.LinearToGammaSpace(1.0f));
                        renderer.material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        // disable emission
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                }
                else
                {
                    MoneybagProtect.LogInstance.LogError("Moneybag Protect: Couldn't locate Renderer component in SurplusValuable or children");
                }
            }
        }
    }
}