using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NuclearOption;
using NuclearOption.Networking;

namespace NuclearOptionEmpMod
{
    [BepInPlugin("com.wersal.empmod", "EMP Weapon Mod", "2.5.0")]
    public class EmpModPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> EmpRadius;
        public static ConfigEntry<bool> DebugLog;
        public static ConfigEntry<bool> ScrambleMissiles;
        public static ConfigEntry<bool> ScrambleFriendlyFire;
        public static ConfigEntry<float> ScrambleExplodeChance;
        public static ConfigEntry<float> ScrambleRetargetChance;
        public static bool IsUIEmpDisabled = false;

        private void Awake()
        {
            EmpRadius = Config.Bind("EMP", "Radius", 900f, "Radius of EMP effect in meters");
            DebugLog = Config.Bind("EMP", "DebugLog", true, "Show debug messages in log");
            ScrambleMissiles = Config.Bind("EMP", "ScrambleMissiles", true, "Scramble missile targets when they are hit by EMP");
            ScrambleFriendlyFire = Config.Bind("EMP", "ScrambleFriendlyFire", true, "If true, scrambled missiles can target friendly units too");
            ScrambleExplodeChance = Config.Bind("EMP", "ScrambleExplodeChance", 0.5f, new ConfigDescription("Chance that a scrambled missile will self-destruct instead of retargeting", new AcceptableValueRange<float>(0f, 1f)));
            ScrambleRetargetChance = Config.Bind("EMP", "ScrambleRetargetChance", 0.5f, new ConfigDescription("Chance that a scrambled missile will retarget instead of flying blind", new AcceptableValueRange<float>(0f, 1f)));

            Logger.LogInfo("EMP Mod v2.5.0 loaded!");
            new Harmony("com.wersal.empmod").PatchAll();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            IsUIEmpDisabled = false;
            if (DynamicMap.i != null) DynamicMap.i.gameObject.SetActive(true);

            RestorePlayerUI();

            if (GameObject.Find("EMP_DecalGlobalMonitor") != null) return;
            var monitorObj = new GameObject("EMP_DecalGlobalMonitor");
            DontDestroyOnLoad(monitorObj);
            monitorObj.AddComponent<DecalGlobalMonitor>();
            Debug.Log($"[EMP] DecalGlobalMonitor spawned for scene: {scene.name}");
        }

        public static void RestorePlayerUI()
        {
            FlightHud.EnableCanvas(true);
            var fh = SceneSingleton<FlightHud>.i;
            if (fh != null)
            {
                var hudCenter = Traverse.Create(fh).Field("HUDCenter").GetValue<Component>();
                if (hudCenter != null) hudCenter.gameObject.SetActive(true);
        
                var compass = Traverse.Create(fh).Field("compass").GetValue<Component>();
                if (compass != null) compass.gameObject.SetActive(true);
        
                var pitchCompass = Traverse.Create(fh).Field("pitchCompass").GetValue<Component>();
                if (pitchCompass != null) pitchCompass.gameObject.SetActive(true);
        
                var pitchCenter = Traverse.Create(fh).Field("pitchCompassCenter").GetValue<GameObject>();
                if (pitchCenter != null) pitchCenter.SetActive(true);
        
                var joystickVector = Traverse.Create(fh).Field("virtualJoystickVector").GetValue<Component>();
                if (joystickVector != null) joystickVector.gameObject.SetActive(true);
        
                fh.statusAnchor?.gameObject.SetActive(true);
                fh.waterline?.gameObject.SetActive(true);
                fh.velocityVector?.gameObject.SetActive(true);
                fh.virtualJoystickPos?.gameObject.SetActive(true);
        
                if (fh.HMDCenter != null)
                {
                    foreach (Transform child in fh.HMDCenter)
                        child.gameObject.SetActive(true);
                }
        
                string[] pathsToEnable = {
                    "SceneEssentials/Canvas/HUDCanvas/HUDCenter",
                    "SceneEssentials/Canvas/HUDCanvas/targetDesignator",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/Altitude/Background",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/Altitude/radarAlt",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/Altitude/Altitude",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/Speed/overspeedDisplay",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/Speed/Airspeed",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/Speed/Background",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/Bearing/Bearing_Value",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/Bearing/Background",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/ArtificialHorizon",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/ArtificialHorizon/vector",
                    "SceneEssentials/Canvas/HUDCanvas/HMDCenter/ArtificialHorizon/horizon"
                };
                foreach (string path in pathsToEnable)
                {
                    Transform t = GameObject.Find("SceneEssentials")?.transform;
                    if (t == null) continue;
                    string[] parts = path.Split('/');
                    for (int i = 1; i < parts.Length; i++)
                    {
                        t = t.Find(parts[i]);
                        if (t == null) break;
                    }
                    if (t != null) t.gameObject.SetActive(true);
                }
        
                Transform velVec = GameObject.Find("SceneEssentials/Canvas/HUDCanvas/velocityVector")?.transform;
                if (velVec != null)
                {
                    var img = velVec.GetComponent<Image>();
                    if (img != null) img.enabled = true;
                }
            }
        
            var dynamicMap = SceneSingleton<DynamicMap>.i;
            if (dynamicMap != null)
            {
                dynamicMap.gameObject.SetActive(true);
                var mapCanvas = dynamicMap.transform.Find("MapCanvas");
                if (mapCanvas != null) mapCanvas.gameObject.SetActive(true);
                var virtualMfd = dynamicMap.GetComponentInChildren<VirtualMFD>();
                if (virtualMfd != null)
                {
                    var topInstruments = virtualMfd.transform.Find("TopInstruments");
                    if (topInstruments != null) topInstruments.gameObject.SetActive(true);
                }
            }
        
            var tacScreen = UnityEngine.Object.FindObjectOfType<TacScreen>();
            if (tacScreen != null)
            {
                var cam = Traverse.Create(tacScreen).Field("cam").GetValue<Camera>();
                if (cam != null) cam.enabled = true;
                var screenMat = Traverse.Create(tacScreen).Field("screenMaterial").GetValue<Material>();
                if (screenMat != null)
                {
                    screenMat.SetColor("_EmissionColor", Color.white);
                    screenMat.SetColor("_BaseColor", Color.white);
                }
            }
        
            var combatHUD = SceneSingleton<CombatHUD>.i;
            if (combatHUD != null)
            {
                combatHUD.iconLayer?.gameObject.SetActive(true);
                Traverse.Create(combatHUD).Field("weaponStatus").GetValue<Component>()?.gameObject.SetActive(true);
                Traverse.Create(combatHUD).Field("threatList").GetValue<Component>()?.gameObject.SetActive(true);
                Traverse.Create(combatHUD).Field("targetArrow").GetValue<Image>()?.gameObject.SetActive(true);
                Traverse.Create(combatHUD).Field("targetText").GetValue<Text>()?.gameObject.SetActive(true);
                Traverse.Create(combatHUD).Field("targetInfo").GetValue<Text>()?.gameObject.SetActive(true);
                Traverse.Create(combatHUD).Field("objectiveOverlay").GetValue<Component>()?.gameObject.SetActive(true);
                Traverse.Create(combatHUD).Field("countermeasureBackground").GetValue<GameObject>()?.SetActive(true);
                Traverse.Create(combatHUD).Field("countermeasureImage").GetValue<Image>()?.gameObject.SetActive(true);
                Traverse.Create(combatHUD).Field("countermeasureName").GetValue<Text>()?.gameObject.SetActive(true);
                Traverse.Create(combatHUD).Field("countermeasureAmmo").GetValue<Text>()?.gameObject.SetActive(true);
                Traverse.Create(combatHUD).Field("aircraftActionsReportAnchor").GetValue<Transform>()?.gameObject.SetActive(true);
            }
        
            var actionsReport = SceneSingleton<AircraftActionsReport>.i;
            if (actionsReport != null) actionsReport.gameObject.SetActive(true);
        }
    }
}