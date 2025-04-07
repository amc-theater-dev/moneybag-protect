using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoneybagProtect
{
    [BepInPlugin("mod.moneybagprotect", "Moneybag Protect", "1.1.0")]
    public class MoneybagProtect : BaseUnityPlugin
    {
        public static MoneybagProtect Instance { get; private set; }
        public static ManualLogSource LogInstance { get; private set; }
        private readonly Harmony harmony = new Harmony("mod.moneybagprotect");

        public ConfigEntry<float> CustomProtectTimer;
        public ConfigEntry<string> CustomProtectColor;

        private void Awake()
        {
            Instance = this;
            LogInstance = Logger;
            CustomProtectTimer = Config.Bind("settings", "protection", 10f, "amount of time tax refund moneybag is protected after spawn");
            CustomProtectColor = Config.Bind("settings", "protectionColor", "cyan", "color of the protection effect. possible values: red, green, cyan, yellow, magenta, white");
            Logger.LogInfo("Moneybag Protect 1.1.0 loaded");
            harmony.PatchAll();
        }

        // assigns emission color based on user-selected config value, defaults to cyan
        public Color GetColorFromConfig()
        {
            switch (CustomProtectColor.Value.ToLower())
            {
                case "red":
                    return Color.red;
                case "green":
                    return Color.green;
                case "cyan":
                    return Color.cyan;
                case "yellow":
                    return Color.yellow;
                case "magenta":
                    return Color.magenta;
                case "white":
                    return Color.white;
                default:
                    return Color.cyan;
            }
        }
    }

    // patch to modify SurplusValuable.Start() to extend indestructibleTimer to the configured value and log the change
    [HarmonyPatch(typeof(SurplusValuable), "Start")]
    public class SurplusValuableStartPatch
    {
        static void Postfix(SurplusValuable __instance)
        {
            // locate the private field 'indestructibleTimer'
            FieldInfo timerField = typeof(SurplusValuable).GetField("indestructibleTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (timerField != null)
            {
                // get the timer amount from the config
                float timerAmount = MoneybagProtect.Instance.CustomProtectTimer.Value;
                // set it to the field
                timerField.SetValue(__instance, timerAmount);
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
        static void Postfix(SurplusValuable __instance)
        {
            // locate indestructibleTimer
            FieldInfo timerField = typeof(SurplusValuable).GetField("indestructibleTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (timerField == null)
            {
                MoneybagProtect.LogInstance.LogError("Moneybag Protect: can't locate 'indestructibleTimer' in SurplusValuable");
                return;
            }

            // retrieve the current value
            float currentTimer = (float)timerField.GetValue(__instance);

            // locate the Renderer component in the object or children
            Renderer renderer = __instance.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                MoneybagProtect.LogInstance.LogError("Moneybag Protect: can't locate Renderer component in SurplusValuable or its children");
                return;
            }

            if (currentTimer > 0)
            {
                // set a glowing emission while invincibility is active
                Color emissionColor = MoneybagProtect.Instance.GetColorFromConfig();
                renderer.material.SetColor("_EmissionColor", emissionColor * Mathf.LinearToGammaSpace(1.0f));
                renderer.material.EnableKeyword("_EMISSION");
            }
            else
            {
                // disable emission once the timer expires
                renderer.material.DisableKeyword("_EMISSION");
            }
        }
    }
}

