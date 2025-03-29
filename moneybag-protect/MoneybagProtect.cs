using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoneybagProtect
{
    [BepInPlugin("mod.moneybagprotect", "Moneybag Protect", "1.0.1")]
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
}