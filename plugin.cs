using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirage;
using NuclearOption;
using NuclearOption.Networking;

namespace NuclearOptionEmpMod
{
    [BepInPlugin("com.yourname.empmod", "EMP Weapon Mod", "2.5.0")]
    public class EmpModPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> EmpRadius;
        public static ConfigEntry<bool> DebugLog;
        public static bool IsUIEmpDisabled = false;
        public static ConfigEntry<bool> ScrambleMissiles;
        public static ConfigEntry<bool> ScrambleFriendlyFire;
        public static ConfigEntry<float> ScrambleExplodeChance;
        public static ConfigEntry<float> ScrambleRetargetChance;

        private void Awake()
        {
            EmpRadius = Config.Bind("EMP", "Radius", 900f, "Radius of EMP effect in meters");
            DebugLog = Config.Bind("EMP", "DebugLog", true, "Show debug messages in log");
            Logger.LogInfo("EMP Mod v2.5.0 loaded!");
            new Harmony("com.yourname.empmod").PatchAll();
            SceneManager.sceneLoaded += OnSceneLoaded;
            ScrambleMissiles = Config.Bind("EMP", "ScrambleMissiles", true, "Scramble missile targets when they are hit by EMP");
            ScrambleFriendlyFire = Config.Bind("EMP", "ScrambleFriendlyFire", true, "If true, scrambled missiles can target friendly units too");
            ScrambleExplodeChance = Config.Bind("EMP", "ScrambleExplodeChance", 0.5f, new ConfigDescription("Chance that a scrambled missile will self-destruct instead of retargeting", new AcceptableValueRange<float>(0f, 1f)));
            ScrambleRetargetChance = Config.Bind("EMP", "ScrambleRetargetChance", 0.5f, new ConfigDescription("Chance that a scrambled missile will retarget instead of flying blind", new AcceptableValueRange<float>(0f, 1f)));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            IsUIEmpDisabled = false;
            if (DynamicMap.i != null) DynamicMap.i.gameObject.SetActive(true);
            
            // Принудительно восстанавливаем UI, если он мог остаться выключенным
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

    public class DecalGlobalMonitor : MonoBehaviour
    {
        private float nextLogTime = 0f;
        public float logInterval = 1f;
        private bool hasLoggedUpdate = false;

        void Awake()
        {
            Debug.Log("[DECAL MONITOR] Awake called – monitor is alive!");
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy() => Debug.Log("[DECAL MONITOR] OnDestroy called – monitor is being destroyed!");
        void OnEnable() => Debug.Log("[DECAL MONITOR] OnEnable");
        void OnDisable() => Debug.Log("[DECAL MONITOR] OnDisable");

        void Update()
        {
            if (!hasLoggedUpdate)
            {
                Debug.Log("[DECAL MONITOR] Update is running (DebugLog=" + EmpModPlugin.DebugLog.Value + ")");
                hasLoggedUpdate = true;
            }
            if (!EmpModPlugin.DebugLog.Value) return;
            if (Time.time >= nextLogTime)
            {
                nextLogTime = Time.time + logInterval;
                LogAllDecalsInSceneExtended();
            }
        }

        public static void LogAllDecalsInSceneExtended()
        {
            var decals = UnityEngine.Object.FindObjectsOfType<DecalProjector>();
            Debug.Log($"[DECAL MONITOR] Total decals: {decals.Length}");
        }
    }

    [HarmonyPatch(typeof(Shockwave), "Start")]
    public static class ShockwaveStartPatch
    {
        static void Postfix(Shockwave __instance)
        {
            if (!EmpModPlugin.DebugLog.Value) return;
            var groundDecal = Traverse.Create(__instance).Field("groundDecal").GetValue<GameObject>();
            if (groundDecal != null)
            {
                DecalProjector dp = groundDecal.GetComponent<DecalProjector>();
                if (dp != null)
                    Debug.Log($"[SHOCKWAVE DECAL] Created {dp.name} at {groundDecal.transform.position}, " +
                              $"size={dp.size}, material={dp.material?.name}, drawDist={dp.drawDistance}");
                else
                    Debug.Log($"[SHOCKWAVE DECAL] Object without DecalProjector: {groundDecal.name}");
            }
        }
    }

    [HarmonyPatch(typeof(Encyclopedia), "AfterLoad", new Type[0])]
    public static class EncyclopediaAfterLoadPatch
    {
        static void Prefix(Encyclopedia __instance)
        {
            if (__instance.weaponMounts.Any(m => m.name.EndsWith("_EMP")))
            {
                Debug.Log("[EMP] Weapon mounts with _EMP already exist, skipping creation");
                return;
            }
            Debug.Log("[EMP] Creating EMP weapon variants...");
            CreateEmpVariants(__instance, "AShM2");
            CreateEmpVariants(__instance, "AGM_heavy");
            Debug.Log("[EMP] EMP weapon variant creation complete");
        }

        static void CreateEmpVariants(Encyclopedia enc, string sourceName)
        {
            var sourcePrefab = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(go => go.transform.parent == null
                                   && go.name == sourceName
                                   && go.GetComponent<Missile>() != null);
            if (sourcePrefab == null)
            {
                Debug.LogWarning($"[EMP] Source prefab '{sourceName}' not found!");
                return;
            }
            Debug.Log($"[EMP] Found source prefab: {sourcePrefab.name}");
            var origMissile = sourcePrefab.GetComponent<Missile>();
            var origDef = origMissile.definition as MissileDefinition;
            var origInfo = origMissile.GetWeaponInfo();
            var empPrefab = InstantiateCleanPrefab(sourcePrefab, sourceName + "_EMP");
            var empMissile = empPrefab.GetComponent<Missile>();
            Traverse.Create(empMissile).Field("blastYield").SetValue(1f);
            Traverse.Create(empMissile).Field("pierceDamage").SetValue(0f);
            Debug.Log($"[EMP] Created EMP prefab: {empPrefab.name}");
            var empDef = UnityEngine.Object.Instantiate(origDef);
            empDef.name = sourceName + "_EMP";
            empDef.jsonKey = empDef.name;
            empDef.unitName = origDef.unitName + " EMP";
            empDef.unitPrefab = empPrefab;
            empDef.dontAutomaticallyAddToEncyclopedia = false;
            empMissile.definition = empDef;
            Debug.Log($"[EMP] Created EMP definition: {empDef.name}, unitPrefab: {empDef.unitPrefab?.name ?? "null"}");
            var empInfo = UnityEngine.Object.Instantiate(origInfo);
            empInfo.name = sourceName + "_EMP_info";
            empInfo.weaponName = origInfo.weaponName + " EMP";
            empInfo.shortName = "EMP";
            empInfo.description = "Electromagnetic warhead. Disables engines and electronics.";
            empInfo.blastDamage = 0f;
            empInfo.pierceDamage = 0f;
            empInfo.energy = true;
            empInfo.weaponPrefab = empPrefab;
            Traverse.Create(empMissile).Field("info").SetValue(empInfo);
            enc.missiles.Add(empDef);
            var sourceMounts = enc.weaponMounts
                .Where(m => m != null && m.name.StartsWith(sourceName) && !m.name.Contains("_EMP"))
                .ToArray();
            Debug.Log($"[EMP] Found {sourceMounts.Length} source mounts for {sourceName}");
            foreach (var srcMount in sourceMounts)
            {
                Debug.Log($"[EMP] Processing source mount: {srcMount.name}, prefab: {srcMount.prefab?.name ?? "null"}");
                if (srcMount.prefab == null)
                {
                    Debug.LogWarning($"[EMP] Mount {srcMount.name} has null prefab, skipping");
                    continue;
                }
                var newMountPrefab = UnityEngine.Object.Instantiate(srcMount.prefab);
                newMountPrefab.name = srcMount.prefab.name + "_EMP";
                newMountPrefab.hideFlags = HideFlags.HideAndDontSave;
                newMountPrefab.transform.SetParent(null);
                newMountPrefab.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(newMountPrefab);
                var netIdMount = newMountPrefab.GetComponentInChildren<NetworkIdentity>();
                if (netIdMount != null)
                {
                    Traverse.Create(netIdMount).Field("_hasSpawned").SetValue(false);
                    netIdMount.PrefabHash = newMountPrefab.name.GetHashCode();
                    Traverse.Create(netIdMount).Method("NetworkReset", new object[0]).GetValue();
                }
                foreach (var mm in newMountPrefab.GetComponentsInChildren<MountedMissile>(true))
                    Traverse.Create(mm).Field("info").SetValue(empInfo);
                foreach (var mc in newMountPrefab.GetComponentsInChildren<MountedCargo>(true))
                    Traverse.Create(mc).Field("info").SetValue(empInfo);
                var colorable = newMountPrefab.GetComponent<ColorableMount>();
                if (colorable != null)
                {
                    var renderers = newMountPrefab.GetComponentsInChildren<Renderer>(true);
                    Traverse.Create(colorable).Field("colorableRenderers").SetValue(renderers);
                }
                var newMount = UnityEngine.Object.Instantiate(srcMount);
                newMount.name = srcMount.name + "_EMP";
                newMount.jsonKey = newMount.name;
                newMount.mountName = empInfo.weaponName;
                newMount.prefab = newMountPrefab;
                newMount.info = empInfo;
                newMount.dontAutomaticallyAddToEncyclopedia = false;
                try
                {
                    newMount.Initialize();
                    Debug.Log($"[EMP] Initialized mount: {newMount.name}, ammo={newMount.ammo}, mountName={newMount.mountName}, info={newMount.info?.name ?? "null"}");
                }
                catch (Exception ex) { Debug.LogWarning($"[EMP] Initialize() failed for {newMount.name}: {ex.Message}"); }
                enc.weaponMounts.Add(newMount);
                int aircraftAdded = 0;
                foreach (var aircraftDef in enc.aircraft)
                {
                    var wm = aircraftDef.unitPrefab?.GetComponentInChildren<WeaponManager>();
                    if (wm == null) continue;
                    foreach (var hs in wm.hardpointSets)
                    {
                        if (hs.weaponOptions == null) continue;
                        if (hs.weaponOptions.Contains(srcMount))
                        {
                            hs.weaponOptions.Add(newMount);
                            aircraftAdded++;
                        }
                    }
                }
                Debug.Log($"[EMP] Added {newMount.name} to {aircraftAdded} aircraft hardpointSets");
            }
            UnityEngine.Object.DontDestroyOnLoad(empPrefab);
        }

        static GameObject InstantiateCleanPrefab(GameObject original, string newName)
        {
            var clone = UnityEngine.Object.Instantiate(original);
            clone.name = newName;
            clone.transform.SetParent(null);
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.SetActive(false);
            var netId = clone.GetComponentInChildren<NetworkIdentity>();
            if (netId != null)
            {
                Traverse.Create(netId).Field("_hasSpawned").SetValue(false);
                netId.PrefabHash = newName.GetHashCode();
                Traverse.Create(netId).Method("NetworkReset", new object[0]).GetValue();
            }
            UnityEngine.Object.DontDestroyOnLoad(clone);
            return clone;
        }
    }

    [HarmonyPatch(typeof(Hardpoint), "SpawnMount")]
    public static class Hardpoint_SpawnMount_Patch
    {
        static void Postfix(Hardpoint __instance, WeaponMount weaponMount, GameObject __result)
        {
            if (__result == null) return;
            if (weaponMount != null && weaponMount.name.EndsWith("_EMP"))
            {
                if (!__result.activeSelf)
                {
                    __result.SetActive(true);
                    if (EmpModPlugin.DebugLog.Value)
                        Debug.Log($"[EMP] Activated spawned mount object '{__result.name}' for EMP mount");
                }
            }
        }
    }

    [HarmonyPatch(typeof(WeaponManager), "SpawnWeapons")]
    public static class WeaponManagerSpawnPatch
    {
        static void Postfix(WeaponManager __instance)
        {
            if (!EmpModPlugin.DebugLog.Value) return;
            var aircraft = __instance.GetComponent<Aircraft>();
            if (aircraft == null || aircraft.loadout == null) return;
            Debug.Log($"[EMP-DIAG] SpawnWeapons for aircraft: {aircraft.name}");
            for (int i = 0; i < __instance.hardpointSets.Length && i < aircraft.loadout.weapons.Count; i++)
            {
                var hs = __instance.hardpointSets[i];
                var mount = aircraft.loadout.weapons[i];
                if (mount != null && mount.name.EndsWith("_EMP"))
                {
                    Debug.Log($"[EMP-DIAG] HardpointSet[{i}] name={hs.name}, mount={mount.name}, " +
                              $"prefab={(mount.prefab != null ? mount.prefab.name : "NULL")}, " +
                              $"info={(mount.info != null ? mount.info.name : "NULL")}, " +
                              $"ammo={mount.ammo}");
                    if (mount.prefab != null)
                    {
                        var renderers = mount.prefab.GetComponentsInChildren<Renderer>(true);
                        Debug.Log($"[EMP-DIAG]   Prefab renderers: {renderers.Length}, activeInHierarchy={mount.prefab.activeInHierarchy}");
                        foreach (var r in renderers.Take(3))
                            Debug.Log($"[EMP-DIAG]     Renderer: {r.gameObject.name} enabled={r.enabled} material={r.sharedMaterial?.name ?? "null"}");
                    }
                    else
                    {
                        Debug.LogError($"[EMP-DIAG]   MOUNT PREFAB IS NULL! Mount={mount.name}");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.Detonate))]
    public static class MissileDetonatePatch
    {
        private static Material sphereMaterial;
        private static Material arcMaterial;

        private static GameObject shockwaveDecalPrefab = null;
        private static GameObject scorchMarkDecalPrefab = null;

        public static float CurrentEMPWaveRadius = 0f;
        private static float waveStartTime = 0f;
        private static float waveDuration = 0f;

        private static void CreateMaterials()
        {
            if (sphereMaterial != null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                sphereMaterial = new Material(shader);
                sphereMaterial.SetFloat("_Surface", 1);
                sphereMaterial.SetFloat("_Blend", 1);
                sphereMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                sphereMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                sphereMaterial.SetFloat("_ZWrite", 0);
                sphereMaterial.SetColor("_BaseColor", new Color(0.3f, 0.5f, 1f, 0.02f));
                sphereMaterial.SetColor("_EmissionColor", new Color(0.4f, 0.6f, 1f) * 4f);
                sphereMaterial.EnableKeyword("_EMISSION");
                sphereMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                sphereMaterial.renderQueue = 3000;
                sphereMaterial.SetFloat("_ReceiveShadows", 0);
                sphereMaterial.SetFloat("_Cull", 0f);

                arcMaterial = new Material(shader);
                arcMaterial.SetFloat("_Surface", 0);
                arcMaterial.SetColor("_BaseColor", new Color(0.2f, 0.7f, 1f, 1f));
                arcMaterial.SetColor("_EmissionColor", new Color(0.3f, 0.8f, 1f) * 10f);
                arcMaterial.EnableKeyword("_EMISSION");
                arcMaterial.SetFloat("_ReceiveShadows", 0);
            }
        }

        [HarmonyPrefix]
        static void Prefix(Missile __instance)
        {
            var info = __instance.GetWeaponInfo();
            if (info == null || !info.name.EndsWith("_EMP_info")) return;
            Traverse.Create(__instance).Field("blastYield").SetValue(0f);
            Traverse.Create(__instance).Field("pierceDamage").SetValue(0f);
            var blastDamageField = Traverse.Create(__instance).Field("blastDamage");
            if (blastDamageField.FieldExists()) blastDamageField.SetValue(0f);
            var explosionForceField = Traverse.Create(__instance).Field("explosionForce");
            if (explosionForceField.FieldExists()) explosionForceField.SetValue(0f);
        }

        [HarmonyPostfix]
        static void Postfix(Missile __instance)
        {
            var info = __instance.GetWeaponInfo();
            if (info == null || !info.name.EndsWith("_EMP_info")) return;
            float radius = EmpModPlugin.EmpRadius.Value;
            float visualRadius = radius * 2f;
            float duration = 2.5f;
            float speed = visualRadius / duration;
            Vector3 pos = __instance.transform.position;
            CreateMaterials();

            var componentsToDisable = new List<Component>();
            var gameObjectsToDisable = new List<GameObject>();
            var powerSuppliesToDisable = new List<PowerSupply>();

            float collectionRadius = radius * 2f;
            // Сбор TargetDetector (сканеры/радары) для отключения
            foreach (var td in UnityEngine.Object.FindObjectsOfType<TargetDetector>())
            {
                if (td != null && Vector3.Distance(td.transform.position, pos) <= collectionRadius)
                    componentsToDisable.Add(td);
            }

            var engineTypes = new Type[] { typeof(TurbineEngine), typeof(Turbofan), typeof(Turbojet), typeof(DuctedFan), typeof(RotorShaft) };
            foreach (var type in engineTypes)
                foreach (var obj in UnityEngine.Object.FindObjectsOfType(type))
                {
                    var comp = obj as Component;
                    if (comp != null && Vector3.Distance(comp.transform.position, pos) <= collectionRadius)
                        componentsToDisable.Add(comp);
                }
            foreach (var comp in UnityEngine.Object.FindObjectsOfType<Component>())
            {
                if (comp == null || Vector3.Distance(comp.transform.position, pos) > collectionRadius) continue;
                if (comp.GetType().Name.IndexOf("Radar", StringComparison.OrdinalIgnoreCase) >= 0)
                    componentsToDisable.Add(comp);
            }
            string[] displayNames = { "MFD", "Display", "Screen", "HUD", "CockpitUI" };
            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (go == null || Vector3.Distance(go.transform.position, pos) > collectionRadius) continue;
                if (displayNames.Any(kw => go.name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0))
                    gameObjectsToDisable.Add(go);
            }
            foreach (var ps in UnityEngine.Object.FindObjectsOfType<PowerSupply>())
                if (ps != null && Vector3.Distance(ps.transform.position, pos) <= collectionRadius)
                    powerSuppliesToDisable.Add(ps);

            // Сбор unitsInRange (уже есть)
            var unitsInRange = UnityEngine.Object.FindObjectsOfType<Unit>()
                .Where(u => u != null && !u.disabled && Vector3.Distance(u.transform.position, pos) <= collectionRadius)
                .ToArray();

            // Отключаем сенсоры, радары и оружие у наземной и водной техники
            foreach (var unit in unitsInRange)
            {
                if (unit is GroundVehicle || unit is Ship)
                {
                    // 1. Отключаем все сенсорные объекты (scanner, scanner2, radar) по имени
                    foreach (Transform child in unit.GetComponentsInChildren<Transform>())
                    {
                        string nameLower = child.name.ToLower();
                        if (nameLower.Contains("scanner") || nameLower.Contains("radar"))
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
            
                    // 2. Отключаем все компоненты TargetDetector (сканеры, радары) — ДОБАВЛЕНО
                    foreach (var td in unit.GetComponentsInChildren<TargetDetector>())
                    {
                        if (td != null)
                            td.enabled = false;
                    }
            
                    // 3. Отключаем все компоненты оружия (Weapon)
                    foreach (var weapon in unit.GetComponentsInChildren<Weapon>())
                    {
                        if (weapon != null)
                            weapon.enabled = false;
                    }
                }
            }

            // Собираем ракеты для скремблирования цели (исключаем другие EMP-ракеты)
            List<Missile> missilesToScramble = new List<Missile>();
            if (EmpModPlugin.ScrambleMissiles.Value)
            {
                foreach (var missile in UnityEngine.Object.FindObjectsOfType<Missile>())
                {
                    if (missile == __instance) continue;
                    if (missile.disabled) continue;
                    var missileInfo = missile.GetWeaponInfo();
                    //if (missileInfo != null && missileInfo.energy) continue; // не глушим другие EMP-ракеты
                    if (Vector3.Distance(missile.transform.position, pos) <= collectionRadius)
                        missilesToScramble.Add(missile);
                }
            }

            var host = new GameObject("EMP_CoroutineHost");
            var hostHack = host.AddComponent<MonoBehaviourHack>();

            var flare = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flare.name = "EMP_Flare";
            flare.transform.position = pos;
            flare.transform.localScale = Vector3.one * (visualRadius * 0.05f);
            var flareRenderer = flare.GetComponent<MeshRenderer>();
            var flareMat = new Material(sphereMaterial);
            flareMat.SetColor("_BaseColor", new Color(0.4f, 0.7f, 1f, 0.95f));
            flareMat.SetColor("_EmissionColor", new Color(0.4f, 0.6f, 1f) * 15f);
            flareRenderer.material = flareMat;
            flareRenderer.shadowCastingMode = ShadowCastingMode.Off;
            UnityEngine.Object.Destroy(flare.GetComponent<Collider>());
            flare.transform.SetParent(Datum.origin, true);
            hostHack.StartCoroutine(FadeFlare(flare, 3f));

            waveStartTime = Time.time;
            waveDuration = duration;

            hostHack.StartCoroutine(EmpVisualEffect(pos, visualRadius, duration));
            hostHack.StartCoroutine(DisableComponentsWithEffects(componentsToDisable, gameObjectsToDisable, powerSuppliesToDisable, missilesToScramble, pos));
            foreach (var unit in unitsInRange)
                hostHack.StartCoroutine(DelayedStaticSound(unit, pos));
            hostHack.StartCoroutine(DisablePlayerUIWhenReached(pos));

            UnityEngine.Object.Destroy(host, duration + 4f);
        }

        private static System.Collections.IEnumerator FadeFlare(GameObject flare, float duration)
        {
            float elapsed = 0;
            Material mat = flare.GetComponent<MeshRenderer>().material;
            Color col = mat.GetColor("_BaseColor");
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(0.9f, 0f, elapsed / duration);
                col.a = a;
                mat.SetColor("_BaseColor", col);
                yield return null;
            }
            UnityEngine.Object.Destroy(flare);
        }

        private static System.Collections.IEnumerator EmpVisualEffect(Vector3 center, float maxDiameter, float duration)
        {
            if (shockwaveDecalPrefab == null)
            {
                var sw = UnityEngine.Object.FindObjectOfType<Shockwave>();
                if (sw != null)
                {
                    shockwaveDecalPrefab = Traverse.Create(sw).Field("groundDecal").GetValue<GameObject>();
                    if (shockwaveDecalPrefab != null)
                        Debug.Log($"[EMP] Stored shockwave decal prefab: {shockwaveDecalPrefab.name}");
                }
            }
            if (scorchMarkDecalPrefab == null)
            {
                var sw = UnityEngine.Object.FindObjectOfType<Shockwave>();
                if (sw != null)
                {
                    var scorchField = Traverse.Create(sw).Field("scorchDecal");
                    if (scorchField.FieldExists())
                        scorchMarkDecalPrefab = scorchField.GetValue<GameObject>();
                }
                if (scorchMarkDecalPrefab == null && GameAssets.i?.scorchMarkDecal != null)
                    scorchMarkDecalPrefab = GameAssets.i.scorchMarkDecal;
                if (scorchMarkDecalPrefab != null)
                    Debug.Log($"[EMP] Stored scorch mark decal prefab: {scorchMarkDecalPrefab.name}");
            }

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "EMP_Sphere";
            sphere.transform.position = center;
            sphere.transform.localScale = Vector3.zero;
            var sphereRenderer = sphere.GetComponent<MeshRenderer>();
            sphereRenderer.material = sphereMaterial;
            sphereRenderer.shadowCastingMode = ShadowCastingMode.Off;
            UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(Datum.origin, true);

            List<LineRenderer> arcs = new List<LineRenderer>();
            for (int i = 0; i < 20; i++)
            {
                var arcGO = new GameObject("EMP_Arc");
                arcGO.transform.SetParent(Datum.origin, false);
                arcGO.transform.position = center;
                var lr = arcGO.AddComponent<LineRenderer>();
                lr.material = arcMaterial;
                lr.startWidth = 0.5f;
                lr.endWidth = 0.25f;
                lr.useWorldSpace = false;
                lr.shadowCastingMode = ShadowCastingMode.Off;
                arcs.Add(lr);
            }

            int lightningCount = 12;
            int segmentsPerLightning = 8;
            List<LineRenderer> centerLightnings = new List<LineRenderer>();
            List<Vector3[]> lightningPaths = new List<Vector3[]>();
            for (int i = 0; i < lightningCount; i++)
            {
                var boltGO = new GameObject("EMP_CenterLightning");
                boltGO.transform.SetParent(Datum.origin, false);
                boltGO.transform.position = center;
                var lr = boltGO.AddComponent<LineRenderer>();
                lr.material = arcMaterial;
                lr.useWorldSpace = false;
                lr.shadowCastingMode = ShadowCastingMode.Off;
                lr.positionCount = segmentsPerLightning;
                Vector3[] emptyPath = new Vector3[segmentsPerLightning];
                lr.SetPositions(emptyPath);
                centerLightnings.Add(lr);
                lightningPaths.Add(emptyPath);
            }

            List<LineRenderer> crossBolts = new List<LineRenderer>();
            for (int i = 0; i < 5; i++)
            {
                var boltGO = new GameObject("EMP_CrossLightning");
                boltGO.transform.SetParent(Datum.origin, false);
                boltGO.transform.position = center;
                var lr = boltGO.AddComponent<LineRenderer>();
                lr.material = arcMaterial;
                lr.startWidth = 0.15f;
                lr.endWidth = 0.07f;
                lr.useWorldSpace = false;
                lr.shadowCastingMode = ShadowCastingMode.Off;
                crossBolts.Add(lr);
            }

            float flareScale = maxDiameter * 0.05f;

            if (scorchMarkDecalPrefab != null)
            {
                var scorchDecal = UnityEngine.Object.Instantiate(scorchMarkDecalPrefab);
                scorchDecal.name = "EMP_ScorchMark";
                var scorchProj = scorchDecal.GetComponent<DecalProjector>();
                if (scorchProj != null)
                {
                    scorchProj.material = new Material(scorchProj.material);
                    scorchProj.size = new Vector3(flareScale, flareScale, flareScale * 0.3f);
                    scorchProj.fadeFactor = 1f;
                    TransformDecalToGround(scorchDecal, center);
                }
            }

            GameObject shockDecal = null;
            DecalProjector shockProj = null;
            Material shockMat = null;
            float maxRadius = maxDiameter * 0.5f;
            float currentRadius = maxDiameter * 0.05f;
            float expansionSpeed = maxRadius / duration;
            float decalBirthTime = 0f;

            CurrentEMPWaveRadius = currentRadius;

            if (shockwaveDecalPrefab != null)
            {
                shockDecal = UnityEngine.Object.Instantiate(shockwaveDecalPrefab);
                shockDecal.name = "EMP_Shockwave";
                decalBirthTime = Time.time;
                shockProj = shockDecal.GetComponent<DecalProjector>();
                if (shockProj != null)
                {
                    Vector3 groundPos = center;
                    Vector3 groundNormal = Vector3.up;
                    RaycastHit hit;
                    if (Physics.Raycast(center + Vector3.up * 500f, Vector3.down, out hit, 1000f))
                    {
                        groundPos = hit.point;
                        groundNormal = hit.normal;
                    }
                    else
                    {
                        groundPos = new Vector3(center.x, Datum.LocalSeaY, center.z);
                    }

                    shockDecal.transform.position = groundPos + groundNormal * 0.05f;
                    shockDecal.transform.rotation = Quaternion.LookRotation(Vector3.down);
                    shockDecal.transform.SetParent(Datum.origin, true);

                    shockMat = new Material(shockProj.material);
                    shockProj.material = shockMat;
                    shockProj.size = new Vector3(maxDiameter, maxDiameter, maxDiameter * 0.3f);
                    shockMat.SetFloat(Shader.PropertyToID("_decalSize"), maxDiameter);
                    shockMat.SetFloat(Shader.PropertyToID("_opacity"), 0.5f);
                    shockMat.SetFloat(Shader.PropertyToID("_shockwaveExpansion"), maxRadius / currentRadius);
                    if (shockMat.HasProperty("_BaseColor"))
                        shockMat.SetColor("_BaseColor", new Color(0.15f, 0.3f, 1.0f, 1.0f));
                    else if (shockMat.HasProperty("_Color"))
                        shockMat.SetColor("_Color", new Color(0.15f, 0.3f, 1.0f, 1.0f));
                    if (shockMat.HasProperty("_EmissionColor"))
                    {
                        shockMat.EnableKeyword("_EMISSION");
                        shockMat.SetColor("_EmissionColor", new Color(0.1f, 0.4f, 1.0f) * 3f);
                    }
                }
            }

            yield return null;

            float elapsed = 0f;
            bool fadingStarted = false;

            float sparkTimer = 0f;
            float groundLightningTimer = 0f;
            float arcUpdateTimer = 0f;
            float centerLightningUpdateTimer = 0f;

            const float sparkInterval = 0.004f;
            const float groundLightningInterval = 0.008f;
            const float arcUpdateInterval = 0.03f;
            const float centerLightningInterval = 0.05f;

            while (true)
            {
                float timeScale = Time.timeScale;
                if (timeScale <= 0f)
                {
                    yield return null;
                    continue;
                }

                float deltaGame = Time.deltaTime;
                elapsed += deltaGame;

                if (elapsed <= duration)
                {
                    currentRadius += expansionSpeed * deltaGame;
                    currentRadius = Mathf.Min(currentRadius, maxRadius);
                }
                CurrentEMPWaveRadius = currentRadius;

                float progress = Mathf.Clamp01(elapsed / duration);

                Color sphereColor;
                if (progress < 0.2f)
                    sphereColor = Color.Lerp(Color.white, new Color(0.3f, 0.5f, 1f, 0.2f), progress / 0.2f);
                else if (progress < 0.8f)
                {
                    float t = (progress - 0.2f) / 0.6f;
                    sphereColor = Color.Lerp(new Color(0.3f, 0.5f, 1f, 0.15f), new Color(0.1f, 0.2f, 0.5f, 0.1f), t);
                }
                else
                {
                    float t = (progress - 0.8f) / 0.2f;
                    sphereColor = Color.Lerp(new Color(0.1f, 0.2f, 0.5f, 0.1f), new Color(0.05f, 0.05f, 0.2f, 0f), t);
                }
                sphere.transform.localScale = Vector3.one * (currentRadius * 2f);
                sphereMaterial.SetColor("_BaseColor", sphereColor);
                Color emissionColor = Color.Lerp(new Color(0.8f, 0.9f, 1f) * 4f, Color.black * 0.5f, progress);
                sphereMaterial.SetColor("_EmissionColor", emissionColor);

                if (elapsed < duration)
                {
                    arcUpdateTimer += deltaGame;
                    if (arcUpdateTimer >= arcUpdateInterval)
                    {
                        arcUpdateTimer -= arcUpdateInterval;
                        float arcThickness = Mathf.Lerp(0.5f, 0.1f, progress);
                        float noiseAmplitude = currentRadius * 0.01f;
                        foreach (var lr in arcs)
                        {
                            lr.startWidth = arcThickness;
                            lr.endWidth = arcThickness * 0.5f;
                            int points = 33;
                            Vector3 ringNormal = UnityEngine.Random.onUnitSphere;
                            Vector3 ringAxis1 = Vector3.Cross(ringNormal, ringNormal != Vector3.forward ? Vector3.forward : Vector3.up).normalized;
                            Vector3 ringAxis2 = Vector3.Cross(ringNormal, ringAxis1).normalized;
                            Vector3[] positions = new Vector3[points + 1];
                            for (int j = 0; j <= points; j++)
                            {
                                float angle = j * 360f / points * Mathf.Deg2Rad;
                                Vector3 basePos = (ringAxis1 * Mathf.Cos(angle) + ringAxis2 * Mathf.Sin(angle)) * currentRadius;
                                positions[j] = basePos + UnityEngine.Random.insideUnitSphere * noiseAmplitude;
                            }
                            lr.positionCount = points + 1;
                            AnimationCurve wCurve = new AnimationCurve();
                            for (int k = 0; k <= points; k++)
                            {
                                float t = (float)k / points;
                                float rnd = UnityEngine.Random.Range(arcThickness * 0.4f, arcThickness * 1.2f);
                                wCurve.AddKey(t, rnd);
                            }
                            lr.widthCurve = wCurve;
                            lr.SetPositions(positions);
                        }
                    }

                    if (arcs.Count >= 2 && arcUpdateTimer <= 0f)
                    {
                        foreach (var bolt in crossBolts)
                        {
                            int idx1 = UnityEngine.Random.Range(0, arcs.Count);
                            int idx2 = UnityEngine.Random.Range(0, arcs.Count);
                            var arc1 = arcs[idx1];
                            var arc2 = arcs[idx2];
                            if (arc1.positionCount > 1 && arc2.positionCount > 1)
                            {
                                int p1 = UnityEngine.Random.Range(0, arc1.positionCount);
                                int p2 = UnityEngine.Random.Range(0, arc2.positionCount);
                                bolt.positionCount = 2;
                                bolt.SetPosition(0, arc1.GetPosition(p1));
                                bolt.SetPosition(1, arc2.GetPosition(p2));
                            }
                        }
                    }

                    centerLightningUpdateTimer += deltaGame;
                    if (centerLightningUpdateTimer >= centerLightningInterval)
                    {
                        centerLightningUpdateTimer -= centerLightningInterval;
                        for (int i = 0; i < centerLightnings.Count; i++)
                        {
                            Vector3 mainDir = UnityEngine.Random.onUnitSphere;
                            float length = currentRadius;
                            Vector3[] path = lightningPaths[i];
                            path[0] = Vector3.zero;
                            path[segmentsPerLightning - 1] = mainDir * length;
                            for (int j = 1; j < segmentsPerLightning - 1; j++)
                            {
                                float t = (float)j / (segmentsPerLightning - 1);
                                Vector3 straightPoint = Vector3.Lerp(path[0], path[segmentsPerLightning - 1], t);
                                Vector3 perpOffset = (UnityEngine.Random.onUnitSphere * (length * 0.2f)) * Mathf.Sin(t * Mathf.PI);
                                path[j] = straightPoint + perpOffset;
                            }
                            var lr = centerLightnings[i];
                            lr.positionCount = segmentsPerLightning;
                            lr.SetPositions(path);
                            AnimationCurve widthCurve = new AnimationCurve();
                            for (int j = 0; j < segmentsPerLightning; j++)
                            {
                                float t = (float)j / (segmentsPerLightning - 1);
                                float w = Mathf.Lerp(0.3f, 0.1f, t) * UnityEngine.Random.Range(0.7f, 1.3f);
                                widthCurve.AddKey(t, w);
                            }
                            lr.widthCurve = widthCurve;
                        }
                    }
                }
                else
                {
                    foreach (var lr in arcs) lr.positionCount = 0;
                    foreach (var bolt in crossBolts) bolt.positionCount = 0;
                    foreach (var lr in centerLightnings) lr.positionCount = 0;
                }

                if (shockMat != null && shockDecal != null)
                {
                    if (currentRadius < maxRadius)
                    {
                        float expansionValue = maxRadius / currentRadius;
                        shockMat.SetFloat(Shader.PropertyToID("_shockwaveExpansion"), expansionValue);
                    }
                    else
                    {
                        if (!fadingStarted) fadingStarted = true;
                        shockMat.SetFloat(Shader.PropertyToID("_shockwaveExpansion"), maxRadius / currentRadius);
                        float currentOpacity = shockMat.GetFloat(Shader.PropertyToID("_opacity"));
                        currentOpacity -= 0.13f * deltaGame;
                        if (currentOpacity <= 0f)
                        {
                            UnityEngine.Object.Destroy(shockDecal);
                            shockDecal = null;
                        }
                        else
                            shockMat.SetFloat(Shader.PropertyToID("_opacity"), currentOpacity);
                    }
                    if (Time.time - decalBirthTime > 20f)
                    {
                        if (shockDecal != null)
                        {
                            UnityEngine.Object.Destroy(shockDecal);
                            shockDecal = null;
                        }
                    }
                }

                if (elapsed <= duration)
                {
                    sparkTimer += deltaGame;
                    groundLightningTimer += deltaGame;
                    while (sparkTimer >= sparkInterval && currentRadius > 0.5f)
                    {
                        sparkTimer -= sparkInterval;
                        Vector3 randomDir = UnityEngine.Random.insideUnitSphere;
                        randomDir.y = 0; randomDir.Normalize();
                        Vector3 samplePoint = center + randomDir * UnityEngine.Random.Range(0f, currentRadius);
                        RaycastHit groundHit;
                        if (Physics.Raycast(samplePoint + Vector3.up * 10f, Vector3.down, out groundHit, 20f))
                            CreateGroundSpark(groundHit.point + Vector3.up * 0.1f, randomDir);
                    }
                    while (groundLightningTimer >= groundLightningInterval && currentRadius > 0.5f)
                    {
                        groundLightningTimer -= groundLightningInterval;
                        Vector3 randomDir = UnityEngine.Random.insideUnitSphere;
                        randomDir.y = 0; randomDir.Normalize();
                        Vector3 samplePoint = center + randomDir * UnityEngine.Random.Range(0f, currentRadius);
                        RaycastHit groundHit;
                        if (Physics.Raycast(samplePoint + Vector3.up * 10f, Vector3.down, out groundHit, 20f))
                            CreateGroundLightning(groundHit.point + Vector3.up * 0.15f);
                    }
                }

                if (elapsed > duration + 3f)
                    break;

                yield return null;
            }

            CurrentEMPWaveRadius = 0f;
            UnityEngine.Object.Destroy(sphere, 0.5f);
            foreach (var lr in arcs) UnityEngine.Object.Destroy(lr.gameObject, 0.5f);
            foreach (var bolt in crossBolts) UnityEngine.Object.Destroy(bolt.gameObject, 0.5f);
            foreach (var lr in centerLightnings) UnityEngine.Object.Destroy(lr.gameObject, 0.5f);
            if (shockDecal != null) UnityEngine.Object.Destroy(shockDecal, 20f);
        }

        private static System.Collections.IEnumerator DisableComponentsWithEffects(
            List<Component> components, List<GameObject> gameObjects, List<PowerSupply> powerSupplies,
            List<Missile> missilesToScramble, Vector3 empCenter)
        {
            var allTargets = new List<(Transform t, object target)>();
            foreach (var comp in components)
                if (comp != null) allTargets.Add((comp.transform, comp));
            foreach (var go in gameObjects)
                if (go != null) allTargets.Add((go.transform, go));
            foreach (var ps in powerSupplies)
                if (ps != null) allTargets.Add((ps.transform, ps));

            while (allTargets.Count > 0 || missilesToScramble.Count > 0)
            {
                if ((Time.time - waveStartTime) >= waveDuration)
                    yield break;

                float currentRadius = CurrentEMPWaveRadius;

                for (int i = allTargets.Count - 1; i >= 0; i--)
                {
                    var (t, target) = allTargets[i];
                    if (t == null) { allTargets.RemoveAt(i); continue; }
                    float dist = Vector3.Distance(t.position, empCenter);
                    float triggerRadius = currentRadius + 1.0f;
                    if (dist <= triggerRadius)
                    {
                        if (target is Component comp)
                        {
                            var trv = Traverse.Create(comp);
                            if (trv.Field("operable").FieldExists()) trv.Field("operable").SetValue(false);
                            else if (trv.Field("inoperable").FieldExists()) trv.Field("inoperable").SetValue(true);
                            if (comp.GetType().Name.IndexOf("Radar", StringComparison.OrdinalIgnoreCase) >= 0 && comp is MonoBehaviour mb)
                                mb.enabled = false;
                            CreateSparkEffect(comp.gameObject);
                        }
                        else if (target is GameObject go)
                        {
                            CreateSparkEffect(go);
                        }
                        else if (target is PowerSupply ps)
                        {
                            Traverse.Create(ps).Field("powered").SetValue(false);
                            CreateSparkEffect(ps.gameObject);
                        }
                        allTargets.RemoveAt(i);
                    }
                }

                for (int i = missilesToScramble.Count - 1; i >= 0; i--)
                {
                    var missile = missilesToScramble[i];
                    if (missile == null || missile.gameObject == null || missile.disabled)
                    {
                        missilesToScramble.RemoveAt(i);
                        continue;
                    }
                    float dist = Vector3.Distance(missile.transform.position, empCenter);
                    if (dist <= currentRadius + 1.0f)
                    {
                        if (missile != null && !missile.disabled && missile.gameObject != null)
                        {
                            try
                            {
                                ScrambleMissileTarget(missile);
                            }
                            catch (Exception e)
                            {
                                if (EmpModPlugin.DebugLog.Value)
                                    Debug.LogWarning($"[EMP] Failed to scramble missile {missile?.name}: {e.Message}");
                            }
                        }
                        missilesToScramble.RemoveAt(i);
                    }
                }

                yield return null;
            }
        }

        private static void ScrambleMissileTarget(Missile missile)
        {
            if (missile == null || missile.gameObject == null || missile.disabled) return;

            string oldTargetName = Traverse.Create(missile).Field("target").GetValue<Unit>()?.name ?? "none";

            // 1. Сброс targetID убирает предупреждение об угрозе
            missile.Network_targetID = PersistentID.None;

            // 2. Отключаем seeker, чтобы ракета не маневрировала, но цель (targetUnit) сохранится → не взорвётся
            var seeker = missile.GetComponent<MissileSeeker>();
            if (seeker != null)
                seeker.enabled = false;

            // 3. Шанс самоуничтожения
            if (UnityEngine.Random.value < EmpModPlugin.ScrambleExplodeChance.Value)
            {
                missile.Detonate(Vector3.up, false, false);
                if (EmpModPlugin.DebugLog.Value)
                    Debug.Log($"[EMP] Missile {missile.name} self-destructed");
                return;
            }

            // 4. Шанс перенацеливания (pillar scan – только XZ, высота игнорируется)
            if (UnityEngine.Random.value < EmpModPlugin.ScrambleRetargetChance.Value)
            {
                List<Unit> candidates = new List<Unit>();
                Vector3 mPos = missile.transform.position;
                foreach (var unit in UnitRegistry.allUnits)
                {
                    if (unit == null || unit.disabled || unit == missile) continue;
                    if (!EmpModPlugin.ScrambleFriendlyFire.Value && unit.NetworkHQ == missile.NetworkHQ) continue;
                    float dx = unit.transform.position.x - mPos.x;
                    float dz = unit.transform.position.z - mPos.z;
                    if (dx * dx + dz * dz > 5000f * 5000f) continue;
                    candidates.Add(unit);
                }
                if (candidates.Count > 0)
                {
                    Unit newTarget = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    missile.SetTarget(newTarget); // перезапустит наведение на новую цель
                    if (EmpModPlugin.DebugLog.Value)
                        Debug.Log($"[EMP] Missile {missile.name} retargeted to {newTarget.name}");
                    return;
                }
            }

            // 5. Если не взорвалась и не перенацелилась – ракета продолжит лететь прямо (seeker отключён)
            if (EmpModPlugin.DebugLog.Value)
                Debug.Log($"[EMP] Missile {missile.name} flying blind");

            // 6. Визуальный эффект (безопасный)
            try
            {
                if (missile != null && missile.gameObject != null && !missile.disabled && missile.isActiveAndEnabled)
                {
                    foreach (var r in missile.GetComponentsInChildren<Renderer>())
                        if (r != null && r.material != null && r.material.HasProperty("_EmissionColor"))
                            r.material.SetColor("_EmissionColor", new Color(0.3f, 0.7f, 1f) * 4f);
                    if (missile.isActiveAndEnabled)
                        missile.StartCoroutine(ResetEmissionAfterDelay(missile, 1.5f));
                }
            }
            catch { }
        }

        private static System.Collections.IEnumerator ResetEmissionAfterDelay(Missile missile, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (missile == null || missile.gameObject == null || missile.disabled) yield break;
            var renderers = missile.GetComponentsInChildren<Renderer>();
            if (renderers == null) yield break;
            foreach (var r in renderers)
            {
                if (r != null && r.material != null && r.material.HasProperty("_EmissionColor"))
                    r.material.SetColor("_EmissionColor", Color.black);
            }
        }

        private static System.Collections.IEnumerator DelayedStaticSound(Unit unit, Vector3 empCenter)
        {
            float endTime = waveStartTime + waveDuration;
            while (unit != null && unit.gameObject != null && Time.time < endTime)
            {
                float dist = Vector3.Distance(unit.transform.position, empCenter);
                if (dist <= CurrentEMPWaveRadius)
                    break;
                yield return null;
            }
            if (unit == null || unit.gameObject == null || Time.time >= endTime) yield break;
            var go = new GameObject("EMP_Static");
            go.transform.position = unit.transform.position;
            go.transform.parent = unit.transform;
            var source = go.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.maxDistance = 10f;
            source.rolloffMode = AudioRolloffMode.Linear;
            var clip = AudioClip.Create("static", 44100, 1, 44100, false, dataArray =>
            {
                for (int i = 0; i < dataArray.Length; i++)
                    dataArray[i] = UnityEngine.Random.Range(-0.5f, 0.5f);
            });
            source.clip = clip;
            source.Play();
            UnityEngine.Object.Destroy(go, 0.91f);
        }

        private static System.Collections.IEnumerator DisablePlayerUIWhenReached(Vector3 empCenter)
        {
            var hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || hud.aircraft == null || hud.aircraft.disabled)
                yield break;
            Transform playerTransform = hud.aircraft.transform;
            float endTime = waveStartTime + waveDuration;

            while (playerTransform != null && hud.aircraft != null && !hud.aircraft.disabled && Time.time < endTime)
            {
                float dist = Vector3.Distance(playerTransform.position, empCenter);
                if (dist <= CurrentEMPWaveRadius && dist <= EmpModPlugin.EmpRadius.Value)
                {
                    if (!EmpModPlugin.IsUIEmpDisabled)
                    {
                        var fh = SceneSingleton<FlightHud>.i;
                        if (fh != null)
                        {
                            var hudCenter = Traverse.Create(fh).Field("HUDCenter").GetValue<Component>();
                            if (hudCenter != null) hudCenter.gameObject.SetActive(false);
                            GameObject hudCenterGo = GameObject.Find("SceneEssentials/Canvas/HUDCanvas/HUDCenter");
                            if (hudCenterGo != null) hudCenterGo.SetActive(false);

                            var compass = Traverse.Create(fh).Field("compass").GetValue<Component>();
                            if (compass != null) compass.gameObject.SetActive(false);

                            var pitchCompass = Traverse.Create(fh).Field("pitchCompass").GetValue<Component>();
                            if (pitchCompass != null) pitchCompass.gameObject.SetActive(false);

                            var pitchCenter = Traverse.Create(fh).Field("pitchCompassCenter").GetValue<GameObject>();
                            if (pitchCenter != null) pitchCenter.SetActive(false);

                            var joystickVector = Traverse.Create(fh).Field("virtualJoystickVector").GetValue<Component>();
                            if (joystickVector != null) joystickVector.gameObject.SetActive(false);

                            fh.statusAnchor?.gameObject.SetActive(false);
                            if (fh.HMDCenter != null)
                            {
                                foreach (Transform child in fh.HMDCenter)
                                {
                                    if (child.name == "TopRightPanel")
                                    {
                                        foreach (Transform trChild in child)
                                        {
                                            if (trChild.name == "PowerPanel")
                                            {
                                                foreach (Transform ppChild in trChild)
                                                {
                                                    if (ppChild.name == "chargeBarBackground" || ppChild.name == "charge Label")
                                                        ppChild.gameObject.SetActive(false);
                                                }
                                                trChild.gameObject.SetActive(true);
                                            }
                                            else
                                            {
                                                trChild.gameObject.SetActive(false);
                                            }
                                        }
                                        child.gameObject.SetActive(true);
                                    }
                                    else if (child.name == "LowerLeftPanel")
                                    {
                                        foreach (Transform llChild in child)
                                        {
                                            if (llChild.name == "ThreatList" || llChild.name == "HUDMapAnchor")
                                                llChild.gameObject.SetActive(false);
                                        }
                                        child.gameObject.SetActive(true);
                                    }
                                    else
                                    {
                                        child.gameObject.SetActive(false);
                                    }
                                }
                            }
                            fh.waterline?.gameObject.SetActive(false);
                            fh.velocityVector?.gameObject.SetActive(false);
                            fh.virtualJoystickPos?.gameObject.SetActive(false);

                            string[] pathsToDisable = new string[]
                            {
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

                            foreach (string path in pathsToDisable)
                            {
                                Transform t = GameObject.Find("SceneEssentials")?.transform;
                                if (t == null) continue;
                                string[] parts = path.Split('/');
                                for (int i = 1; i < parts.Length; i++)
                                {
                                    t = t.Find(parts[i]);
                                    if (t == null) break;
                                }
                                if (t != null) t.gameObject.SetActive(false);
                            }

                            Transform velocityVector = GameObject.Find("SceneEssentials/Canvas/HUDCanvas/velocityVector")?.transform;
                            if (velocityVector != null)
                            {
                                var img = velocityVector.GetComponent<Image>();
                                if (img != null) img.enabled = false;
                            }
                        }

                        var dynamicMap = SceneSingleton<DynamicMap>.i;
                        if (dynamicMap != null)
                        {
                            var mapCanvas = dynamicMap.transform.Find("MapCanvas");
                            if (mapCanvas != null)
                            {
                                var gridToolTip = mapCanvas.Find("mapBackground/GridToolTip");
                                if (gridToolTip != null) gridToolTip.gameObject.SetActive(false);
                                mapCanvas.gameObject.SetActive(false);
                            }
                            var virtualMfd = dynamicMap.GetComponentInChildren<VirtualMFD>();
                            if (virtualMfd != null)
                            {
                                var topInstruments = virtualMfd.transform.Find("TopInstruments");
                                if (topInstruments != null) topInstruments.gameObject.SetActive(false);
                            }
                        }

                        var tacScreen = UnityEngine.Object.FindObjectOfType<TacScreen>();
                        if (tacScreen != null)
                        {
                            var cam = Traverse.Create(tacScreen).Field("cam").GetValue<Camera>();
                            if (cam != null) cam.enabled = false;
                            var rt = Traverse.Create(tacScreen).Field("renderTexture").GetValue<RenderTexture>();
                            if (rt != null)
                            {
                                RenderTexture.active = rt;
                                GL.Clear(true, true, Color.black);
                                RenderTexture.active = null;
                            }
                            var screenMat = Traverse.Create(tacScreen).Field("screenMaterial").GetValue<Material>();
                            if (screenMat != null)
                            {
                                screenMat.SetColor("_EmissionColor", Color.black);
                                screenMat.SetColor("_BaseColor", Color.black);
                            }
                        }
                        var combatHUD = SceneSingleton<CombatHUD>.i;
                        if (combatHUD != null)
                        {
                            combatHUD.iconLayer?.gameObject.SetActive(false);
                            Traverse.Create(combatHUD).Field("weaponStatus").GetValue<Component>()?.gameObject.SetActive(false);
                            Traverse.Create(combatHUD).Field("threatList").GetValue<Component>()?.gameObject.SetActive(false);
                            Traverse.Create(combatHUD).Field("targetArrow").GetValue<Image>()?.gameObject.SetActive(false);
                            Traverse.Create(combatHUD).Field("targetText").GetValue<Text>()?.gameObject.SetActive(false);
                            Traverse.Create(combatHUD).Field("targetInfo").GetValue<Text>()?.gameObject.SetActive(false);
                            Traverse.Create(combatHUD).Field("objectiveOverlay").GetValue<Component>()?.gameObject.SetActive(false);
                            Traverse.Create(combatHUD).Field("countermeasureBackground").GetValue<GameObject>()?.SetActive(false);
                            Traverse.Create(combatHUD).Field("countermeasureImage").GetValue<Image>()?.gameObject.SetActive(false);
                            Traverse.Create(combatHUD).Field("countermeasureName").GetValue<Text>()?.gameObject.SetActive(false);
                            Traverse.Create(combatHUD).Field("countermeasureAmmo").GetValue<Text>()?.gameObject.SetActive(false);
                            Traverse.Create(combatHUD).Field("aircraftActionsReportAnchor").GetValue<Transform>()?.gameObject.SetActive(false);
                        }
                        var actionsReport = SceneSingleton<AircraftActionsReport>.i;
                        if (actionsReport != null) actionsReport.gameObject.SetActive(false);

                        if (NightVision.i != null)
                        {
                            bool nightActive = Traverse.Create(NightVision.i).Field("nightVisActive").GetValue<bool>();
                            if (nightActive)
                                NightVision.Toggle();
                        }

                        EmpModPlugin.IsUIEmpDisabled = true;
                        hud.aircraft.onDisableUnit += (_) =>
                        {
                            EmpModPlugin.IsUIEmpDisabled = false;
                        };
                    }
                    yield break;
                }
                yield return null;
            }
        }

        private static void CreateWaterEffect(Vector3 center, float maxDiameter, float duration)
        {
            var waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterPlane.name = "EMP_WaterRing";
            waterPlane.transform.position = new Vector3(center.x, Datum.LocalSeaY + 0.05f, center.z);
            waterPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            waterPlane.transform.localScale = Vector3.one * 0.1f;
            var renderer = waterPlane.GetComponent<MeshRenderer>();
            Material waterMat = new Material(sphereMaterial.shader);
            waterMat.SetFloat("_Surface", 1);
            waterMat.SetFloat("_Blend", 1);
            waterMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            waterMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            waterMat.SetFloat("_ZWrite", 0);
            waterMat.SetColor("_BaseColor", new Color(0.2f, 0.5f, 1f, 0.6f));
            waterMat.SetColor("_EmissionColor", Color.black);
            waterMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            waterMat.renderQueue = 3000;
            renderer.material = waterMat;
            UnityEngine.Object.Destroy(waterPlane.GetComponent<Collider>());
            var hack = waterPlane.AddComponent<MonoBehaviourHack>();
            hack.StartCoroutine(AnimateWaterRing(waterPlane, maxDiameter, duration));
        }

        private static System.Collections.IEnumerator AnimateWaterRing(GameObject plane, float maxDiameter, float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.1f;
            Vector3 endScale = new Vector3(maxDiameter, 1f, maxDiameter);
            Material mat = plane.GetComponent<MeshRenderer>().material;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                plane.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                Color c = mat.GetColor("_BaseColor");
                c.a = Mathf.Lerp(0.5f, 0f, t);
                mat.SetColor("_BaseColor", c);
                yield return null;
            }
            UnityEngine.Object.Destroy(plane);
        }

        private static void CreateGroundSpark(Vector3 position, Vector3 outDirection)
        {
            var effectGO = new GameObject("EMP_GroundSpark");
            effectGO.transform.position = position;
            effectGO.transform.SetParent(Datum.origin, true);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(effectGO.transform, false);
            sphere.transform.localScale = Vector3.one * 0.25f;
            var spRenderer = sphere.GetComponent<MeshRenderer>();
            spRenderer.material = new Material(arcMaterial);
            bool orange = UnityEngine.Random.value > 0.7f;
            Color baseColor = orange ? new Color(1f, 0.5f, 0.1f, 1f) : new Color(0.5f, 0.7f, 1f, 1f);
            Color emissionColor = orange ? new Color(1f, 0.4f, 0f) * 8f : new Color(0.3f, 0.8f, 1f) * 8f;
            spRenderer.material.SetColor("_BaseColor", baseColor);
            spRenderer.material.SetColor("_EmissionColor", emissionColor);
            spRenderer.shadowCastingMode = ShadowCastingMode.Off;
            UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());
            var bolt = new GameObject("EMP_GroundBolt");
            bolt.transform.SetParent(effectGO.transform, false);
            var lr = bolt.AddComponent<LineRenderer>();
            Material boltMat = new Material(arcMaterial);
            boltMat.SetColor("_BaseColor", baseColor);
            boltMat.SetColor("_EmissionColor", emissionColor);
            lr.material = boltMat;
            lr.startWidth = 0.08f;
            lr.endWidth = 0.03f;
            lr.useWorldSpace = false;
            Vector3 dir = UnityEngine.Random.onUnitSphere;
            dir.y = -Mathf.Abs(dir.y) * 0.5f;
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, dir * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f);
            var lightGO = new GameObject("EMP_GroundLight");
            lightGO.transform.SetParent(effectGO.transform, false);
            var lightComp = lightGO.AddComponent<Light>();
            lightComp.color = new Color(0.4f, 0.7f, 1f);
            lightComp.intensity = 5f;
            lightComp.range = 2f;
            var hack = effectGO.AddComponent<MonoBehaviourHack>();
            hack.StartCoroutine(AnimateGroundSpark(effectGO, outDirection, 15f, 21f));
        }

        private static System.Collections.IEnumerator AnimateGroundSpark(GameObject target, Vector3 moveDir, float duration, float speed)
        {
            float elapsed = 0f;
            var renderers = target.GetComponentsInChildren<MeshRenderer>();
            var lights = target.GetComponentsInChildren<Light>();
            float groundCheckInterval = 0.5f;
            float nextGroundCheck = 0f;

            while (elapsed < duration)
            {
                while (Time.timeScale == 0f) yield return null;

                elapsed += Time.deltaTime;
                target.transform.position += moveDir * speed * Time.deltaTime;

                if (elapsed >= nextGroundCheck)
                {
                    nextGroundCheck += groundCheckInterval;
                    RaycastHit hit;
                    if (Physics.Raycast(target.transform.position + Vector3.up * 2f, Vector3.down, out hit, 5f))
                    {
                        Vector3 pos = target.transform.position;
                        pos.y = hit.point.y + 1f + UnityEngine.Random.Range(-0.5f, 0.5f);
                        target.transform.position = pos;
                    }
                }

                float fade = 1f - (elapsed / duration);
                foreach (var r in renderers)
                {
                    var col = r.material.GetColor("_BaseColor");
                    col.a = fade;
                    r.material.SetColor("_BaseColor", col);
                }
                foreach (var l in lights) l.intensity = 5f * fade;
                yield return null;
            }
            UnityEngine.Object.Destroy(target);
        }

        private static void CreateGroundLightning(Vector3 position)
        {
            var effectGO = new GameObject("EMP_GroundLightning");
            effectGO.transform.position = position;
            effectGO.transform.SetParent(Datum.origin, true);
            int lineCount = 3;
            var lineRenderers = new LineRenderer[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                var boltObj = new GameObject("EMP_GBolt");
                boltObj.transform.SetParent(effectGO.transform, false);
                var lr = boltObj.AddComponent<LineRenderer>();
                lr.material = arcMaterial;
                lr.useWorldSpace = false;
                lr.shadowCastingMode = ShadowCastingMode.Off;
                lineRenderers[i] = lr;
            }
            var lightGO = new GameObject("EMP_GLight");
            lightGO.transform.SetParent(effectGO.transform, false);
            var lightComp = lightGO.AddComponent<Light>();
            lightComp.color = new Color(0.4f, 0.7f, 1f);
            lightComp.intensity = 8f;
            lightComp.range = 3f;
            var hack = effectGO.AddComponent<MonoBehaviourHack>();
            hack.StartCoroutine(AnimateGroundLightning(effectGO, lineRenderers, lightComp, 15f));
        }

        private static System.Collections.IEnumerator AnimateGroundLightning(GameObject target, LineRenderer[] lines, Light light, float duration)
        {
            float elapsed = 0f;
            float updateTimer = 0f;
            const float updateInterval = 0.05f;
            while (elapsed < duration)
            {
                while (Time.timeScale == 0f) yield return null;
                elapsed += Time.deltaTime;
                updateTimer += Time.deltaTime;
                float fade = 1f - (elapsed / duration);
                if (light != null) light.intensity = 8f * fade;
                if (updateTimer >= updateInterval)
                {
                    updateTimer -= updateInterval;
                    foreach (var lr in lines)
                    {
                        int segCount = UnityEngine.Random.Range(4, 10);
                        lr.positionCount = segCount;
                        AnimationCurve widthCurve = new AnimationCurve();
                        for (int i = 0; i < segCount; i++)
                        {
                            float t = (float)i / (segCount - 1);
                            float rndWidth = UnityEngine.Random.Range(0.02f, 0.12f);
                            widthCurve.AddKey(t, rndWidth);
                        }
                        lr.widthCurve = widthCurve;
                        Vector3 start = UnityEngine.Random.insideUnitSphere * 0.5f;
                        start.y = 0;
                        Vector3 end = UnityEngine.Random.insideUnitSphere * 2.0f;
                        end.y = Mathf.Abs(end.y) * 0.5f;
                        Vector3[] points = new Vector3[segCount];
                        for (int i = 0; i < segCount; i++)
                        {
                            float t = (float)i / (segCount - 1);
                            points[i] = Vector3.Lerp(start, end, t) + UnityEngine.Random.insideUnitSphere * 0.3f;
                        }
                        lr.SetPositions(points);
                    }
                }
                foreach (var lr in lines)
                {
                    if (lr.material.HasProperty("_BaseColor"))
                    {
                        Color c = lr.material.GetColor("_BaseColor");
                        c.a = fade;
                        lr.material.SetColor("_BaseColor", c);
                    }
                }
                yield return null;
            }
            UnityEngine.Object.Destroy(target);
        }

        private static void TransformDecalToGround(GameObject decal, Vector3 center)
        {
            Vector3 groundPos = center;
            Vector3 groundNormal = Vector3.up;
            RaycastHit hit;
            if (Physics.Raycast(center + Vector3.up * 500f, Vector3.down, out hit, 1000f))
            {
                groundPos = hit.point;
                groundNormal = hit.normal;
            }
            else
            {
                groundPos = new Vector3(center.x, Datum.LocalSeaY, center.z);
            }
            decal.transform.position = groundPos + groundNormal * 0.05f;
            decal.transform.rotation = Quaternion.LookRotation(Vector3.down, groundNormal);
        }

        private static void CreateSparkEffect(GameObject target)
        {
            if (target == null) return;
            var pos = target.transform.position;
            var effectGO = new GameObject("EMP_SparkEffect");
            effectGO.transform.position = pos;
            var tracker = effectGO.AddComponent<SparkTracker>();
            tracker.Init(target.transform);
            bool isDisplay = target.name.IndexOf("Screen", StringComparison.OrdinalIgnoreCase) >= 0 || target.name.IndexOf("Display", StringComparison.OrdinalIgnoreCase) >= 0 || target.name.IndexOf("MFD", StringComparison.OrdinalIgnoreCase) >= 0 || target.name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isDisplay) effectGO.AddComponent<DisplayFlag>();
            Rigidbody rb = target.GetComponentInParent<Rigidbody>();
            Vector3 velocity = (rb != null) ? rb.velocity : Vector3.zero;
            Vector3 mainDir = velocity.sqrMagnitude > 0.5f ? -velocity.normalized : Vector3.down;
            var controller = effectGO.AddComponent<MonoBehaviourHack>();
            var arcsContainer = new GameObject("EMP_MiniArcs");
            arcsContainer.transform.SetParent(effectGO.transform, false);
            float miniArcStartWidth = isDisplay ? 0.02f : 0.04f;
            float miniArcEndWidth = isDisplay ? 0.01f : 0.02f;
            for (int i = 0; i < 3; i++)
            {
                var arcObj = new GameObject("EMP_MiniArc");
                arcObj.transform.SetParent(arcsContainer.transform, false);
                var lr = arcObj.AddComponent<LineRenderer>();
                lr.material = arcMaterial;
                lr.startWidth = miniArcStartWidth;
                lr.endWidth = miniArcEndWidth;
                lr.useWorldSpace = false;
                lr.positionCount = 5;
            }
            controller.StartCoroutine(AnimateMiniArcs(arcsContainer, 3f));
            UnityEngine.Object.Destroy(arcsContainer, 3.1f);
            controller.StartCoroutine(CreateSparksOverTime(effectGO, mainDir, 48, 7f));
            Collider parentCollider = target.GetComponentInParent<Collider>();
            if (parentCollider != null) IgnoreCollisionWithParent(effectGO, parentCollider, 1.5f);
            var lightGO = new GameObject("EMP_SparkLight");
            lightGO.transform.SetParent(effectGO.transform, false);
            var l = lightGO.AddComponent<Light>();
            l.color = new Color(0.4f, 0.7f, 1f);
            l.intensity = 20f;
            l.range = 5f;
            controller.StartCoroutine(FadeLight(l, 5f));
            UnityEngine.Object.Destroy(effectGO, 7.2f);
        }

        private static void IgnoreCollisionWithParent(GameObject effect, Collider parentCol, float duration)
        {
            Collider[] childCols = effect.GetComponentsInChildren<Collider>();
            foreach (var col in childCols) Physics.IgnoreCollision(col, parentCol, true);
            var hack = effect.GetComponent<MonoBehaviourHack>();
            if (hack != null) hack.StartCoroutine(EnableCollisionAfterTime(effect, parentCol, duration));
        }

        private static System.Collections.IEnumerator EnableCollisionAfterTime(GameObject effect, Collider parentCol, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (effect == null || parentCol == null) yield break;
            Collider[] childCols = effect.GetComponentsInChildren<Collider>();
            foreach (var col in childCols) Physics.IgnoreCollision(col, parentCol, false);
        }

        private static System.Collections.IEnumerator CreateSparksOverTime(GameObject parent, Vector3 mainDirection, int count, float totalDuration)
        {
            float delayBetween = 0.06f;
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = mainDirection + UnityEngine.Random.insideUnitSphere * 0.5f;
                dir.Normalize();
                var spark = CreateSingleSpark(parent, dir, totalDuration - (i * delayBetween));
                yield return new WaitForSeconds(delayBetween);
            }
        }

        private static GameObject CreateSingleSpark(GameObject parent, Vector3 direction, float lifetime)
        {
            var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "EMP_Spark";
            spark.transform.SetParent(parent.transform, false);
            spark.transform.localScale = new Vector3(0.04f, 0.015f, 0.04f);
            spark.transform.localPosition = direction * 0.3f;
            var renderer = spark.GetComponent<MeshRenderer>();
            var mat = new Material(arcMaterial);
            mat.SetColor("_BaseColor", new Color(0.4f, 0.7f, 1f, 1f));
            mat.SetColor("_EmissionColor", new Color(0.3f, 0.8f, 1f) * 8f);
            renderer.material = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var collider = spark.GetComponent<Collider>();
            collider.isTrigger = true;
            collider.enabled = false;
            var hack = spark.AddComponent<MonoBehaviourHack>();
            hack.StartCoroutine(AnimateSpark(spark, direction, lifetime, collider));
            spark.AddComponent<SparkDestroyOnTrigger>();
            return spark;
        }

        private static System.Collections.IEnumerator AnimateSpark(GameObject spark, Vector3 direction, float duration, Collider col)
        {
            float elapsed = 0;
            Material mat = spark.GetComponent<MeshRenderer>().material;
            Vector3 localStart = spark.transform.localPosition;
            float collisionDelay = 0.25f;
            float speedMultiplier = 4.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                spark.transform.localPosition = localStart + direction * t * 3f * speedMultiplier;
                Color c = mat.GetColor("_BaseColor");
                c.a = Mathf.Lerp(1f, 0f, t);
                mat.SetColor("_BaseColor", c);
                if (!col.enabled && elapsed >= collisionDelay) col.enabled = true;
                yield return null;
            }
            UnityEngine.Object.Destroy(spark);
        }

        public class SparkDestroyOnTrigger : MonoBehaviour { void OnTriggerEnter(Collider other) => Destroy(gameObject); }

        private static System.Collections.IEnumerator AnimateMiniArcs(GameObject arcsContainer, float duration)
        {
            var arcs = arcsContainer.GetComponentsInChildren<LineRenderer>();
            if (arcs.Length == 0) yield break;
            Transform parentTransform = arcsContainer.transform.parent;
            if (parentTransform == null) yield break;
            Transform rootTransform = parentTransform.root;
            float maxDistance = 2.5f;
            Vector3[] localTargets = new Vector3[arcs.Length];
            bool[] targetValid = new bool[arcs.Length];
            float elapsed = 0f;
            float updateTimer = 0f;
            const float updateInterval = 0.05f;
            while (elapsed < duration)
            {
                while (Time.timeScale == 0f) yield return null;
                elapsed += Time.deltaTime;
                updateTimer += Time.deltaTime;
                if (updateTimer >= updateInterval)
                {
                    updateTimer -= updateInterval;
                    Vector3 currentOrigin = parentTransform.position;
                    Vector3 vehicleCenter = rootTransform ? rootTransform.position : currentOrigin;
                    Vector3 toCenter = (vehicleCenter - currentOrigin).normalized;
                    for (int i = 0; i < arcs.Length; i++)
                    {
                        if (!targetValid[i] || UnityEngine.Random.value < 0.4f)
                        {
                            Vector3 bestPoint = Vector3.zero;
                            bool found = false;
                            float closestDist = maxDistance;
                            for (int rayIdx = 0; rayIdx < 5; rayIdx++)
                            {
                                Vector3 rayDir = rayIdx == 0 ? toCenter : (toCenter + UnityEngine.Random.insideUnitSphere * 0.8f).normalized;
                                if (Physics.Raycast(currentOrigin, rayDir, out RaycastHit hit, maxDistance))
                                {
                                    if (!found || hit.distance < closestDist)
                                    {
                                        closestDist = hit.distance;
                                        bestPoint = hit.point;
                                        found = true;
                                    }
                                }
                            }
                            if (found)
                                localTargets[i] = parentTransform.InverseTransformPoint(bestPoint);
                            else
                            {
                                Vector3 fallback = currentOrigin + toCenter * 1.5f;
                                localTargets[i] = parentTransform.InverseTransformPoint(fallback);
                            }
                            targetValid[i] = true;
                        }
                        Vector3 worldTarget = parentTransform.TransformPoint(localTargets[i]);
                        worldTarget += UnityEngine.Random.insideUnitSphere * 0.1f;
                        Vector3 worldStart = currentOrigin + UnityEngine.Random.insideUnitSphere * 0.4f;
                        Vector3[] worldPoints = new Vector3[5];
                        worldPoints[0] = worldStart;
                        for (int j = 1; j < 4; j++)
                        {
                            float t = j / 4f;
                            Vector3 mid = Vector3.Lerp(worldStart, worldTarget, t);
                            Vector3 perp = Vector3.Cross((worldTarget - worldStart).normalized, UnityEngine.Random.onUnitSphere).normalized;
                            float offset = UnityEngine.Random.Range(-0.5f, 0.5f);
                            worldPoints[j] = mid + perp * offset;
                        }
                        worldPoints[4] = worldTarget;
                        List<Vector3> finalWorld = new List<Vector3> { worldPoints[0] };
                        for (int j = 0; j < worldPoints.Length - 1; j++)
                        {
                            if (Physics.Linecast(worldPoints[j], worldPoints[j+1], out RaycastHit hit))
                            {
                                finalWorld.Add(hit.point);
                                break;
                            }
                            finalWorld.Add(worldPoints[j+1]);
                        }
                        Vector3[] localPoints = new Vector3[finalWorld.Count];
                        for (int k = 0; k < finalWorld.Count; k++)
                            localPoints[k] = parentTransform.InverseTransformPoint(finalWorld[k]);
                        var lr = arcs[i];
                        lr.positionCount = localPoints.Length;
                        for (int k = 0; k < localPoints.Length; k++)
                            lr.SetPosition(k, localPoints[k]);
                    }
                }
                yield return null;
            }
        }

        private static System.Collections.IEnumerator FadeLight(Light light, float duration)
        {
            float startIntensity = light.intensity;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                if (light == null) yield break;
                light.intensity = Mathf.Lerp(startIntensity, 0, t / duration);
                yield return null;
            }
            if (light != null) light.intensity = 0;
        }
    }

    [HarmonyPatch(typeof(Missile), "CheckExclusionZone")]
    public static class Missile_CheckExclusionZone_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(Missile __instance)
        {
            WeaponInfo info = __instance.GetWeaponInfo();
            if (info == null || !info.energy)
                return true;

            Debug.Log("[EMP] EMP-ракета: перехватываем CheckExclusionZone");

            GlobalPosition zonePos;
            Unit target = Traverse.Create(__instance).Field("target").GetValue<Unit>();
            if (target != null && __instance.NetworkHQ != null)
                __instance.NetworkHQ.TryGetKnownPosition(target, out zonePos);
            else
                zonePos = Traverse.Create(__instance).Field("aimPoint").GetValue<GlobalPosition>();

            float empRadius = EmpModPlugin.EmpRadius.Value;
            ExclusionZone empZone = new ExclusionZone(__instance, zonePos, empRadius);

            if (__instance.NetworkHQ != null)
            {
                var zones = Traverse.Create(__instance.NetworkHQ).Field("exclusionZones").GetValue<List<ExclusionZone>>();
                zones?.Add(empZone);
            }

            if (SceneSingleton<DynamicMap>.i != null)
            {
                var map = SceneSingleton<DynamicMap>.i;
                GameObject icon = UnityEngine.Object.Instantiate(GameAssets.i.exclusionZoneDisplay, map.iconLayer.transform);
                icon.transform.localPosition = new Vector3(zonePos.x, zonePos.z, 0f) * map.mapDisplayFactor;
                float scale = empRadius * map.mapDisplayFactor * 2f;
                icon.transform.localScale = new Vector3(scale, scale, scale);
                var img = icon.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(0.3f, 0.7f, 1f, 0.5f);
                __instance.onDisableUnit += (_) => UnityEngine.Object.Destroy(icon);
                Debug.Log($"[EMP] ExclusionZone created manually. pos={icon.transform.localPosition}, scale={icon.transform.localScale}");
            }
            else
            {
                Debug.LogWarning("[EMP] DynamicMap.i равен null");
            }

            if (NetworkSceneSingleton<MessageManager>.i != null)
                NetworkSceneSingleton<MessageManager>.i.RpcAllHQMessage("EMP weapon deployed! Electronics at risk in target area.");

            return false;
        }
    }

    [HarmonyPatch(typeof(DynamicMap), "DisplayExclusionZone")]
    public static class DynamicMap_DisplayExclusionZone_Patch
    {
        [HarmonyPostfix]
        static void Postfix(DynamicMap __instance, ExclusionZone exclusionZone)
        {
            Debug.Log($"[EMP] DisplayExclusionZone called. iconLayer.childCount={__instance.iconLayer.transform.childCount}");

            Unit sourceUnit;
            if (UnitRegistry.TryGetUnit(new PersistentID?(exclusionZone.sourceId), out sourceUnit) && sourceUnit is Missile missile)
            {
                WeaponInfo info = missile.GetWeaponInfo();
                if (info != null && info.energy)
                {
                    Transform iconLayer = __instance.iconLayer.transform;
                    if (iconLayer.childCount > 0)
                    {
                        Transform icon = iconLayer.GetChild(iconLayer.childCount - 1);
                        Image img = icon.GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = new Color(0.3f, 0.7f, 1f, 0.5f);
                            Debug.Log($"[EMP] EMP ExclusionZone icon color set to blue. New color={img.color}");
                        }
                        else
                        {
                            var sr = icon.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                sr.color = new Color(0.3f, 0.7f, 1f, 0.5f);
                                Debug.Log("[EMP] Applied color to SpriteRenderer instead.");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[EMP] iconLayer.childCount == 0, cannot colorize ExclusionZone.");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(OpticalSeekerCruiseMissile), "SlowChecks")]
    public static class OpticalSeekerCruiseMissile_SlowChecks_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(OpticalSeekerCruiseMissile __instance)
        {
            Missile missile = Traverse.Create(__instance).Field("missile").GetValue<Missile>();
            if (missile != null && missile.targetID == PersistentID.None)
                return false; // скремблированная ракета – не даём игре её детонировать
            return true;
        }
    }

    [HarmonyPatch(typeof(BallisticMissileGuidance), "SlowChecks")]
    public static class BallisticMissileGuidance_SlowChecks_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(BallisticMissileGuidance __instance)
        {
            Missile missile = Traverse.Create(__instance).Field("missile").GetValue<Missile>();
            if (missile != null && missile.targetID == PersistentID.None)
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(MissileSeeker), "Seek")]
    public static class MissileSeeker_Seek_Patch
    {
        static bool Prefix(MissileSeeker __instance)
        {
            Missile missile = Traverse.Create(__instance).Field("missile").GetValue<Missile>();
            // Если ракета была скремблирована (targetID сброшен) – не даём seeker’у работать
            if (missile != null && missile.targetID == PersistentID.None)
                return false; // полностью пропускаем Seek
            return true;
        }
    }

    [HarmonyPatch(typeof(DynamicMap), "Maximize")]
    public static class DynamicMap_Maximize_EMP_Patch
    {
        static void Postfix()
        {
            if (!EmpModPlugin.IsUIEmpDisabled) return;

            var dynamicMap = SceneSingleton<DynamicMap>.i;
            if (dynamicMap == null) return;

            var mapImage = dynamicMap.mapImage?.transform;
            if (mapImage != null)
            {
                var iconLayer = mapImage.Find("iconLayer");
                if (iconLayer != null)
                    iconLayer.gameObject.SetActive(false);
            }

            GameObject gameplayCanvas = GameObject.Find("SceneEssentials/Canvas/GameplayUICanvas");
            if (gameplayCanvas != null)
            {
                Transform topInstruments = gameplayCanvas.transform.Find("VirtualMFD/TopInstruments");
                if (topInstruments != null) topInstruments.gameObject.SetActive(false);

                Transform mapCanvas = gameplayCanvas.transform.Find("MapCanvas");
                if (mapCanvas != null)
                {
                    foreach (Transform child in mapCanvas)
                    {
                        if (child.name == "mapBackground")
                        {
                            child.gameObject.SetActive(true);
                            Transform gridToolTip = child.Find("GridToolTip");
                            if (gridToolTip != null) gridToolTip.gameObject.SetActive(false);
                            Transform gridAircraft = child.Find("GridAircraft");
                            if (gridAircraft != null) gridAircraft.gameObject.SetActive(false);
                            Transform mapScaleProxy = child.Find("mapScaleProxy");
                            if (mapScaleProxy != null) mapScaleProxy.gameObject.SetActive(false);
                            Transform iconLayer2 = child.Find("mapImage/iconLayer");
                            if (iconLayer2 != null) iconLayer2.gameObject.SetActive(false);
                        }
                        else
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(NightVision), "Update")]
    public static class NightVision_Update_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(NightVision __instance)
        {
            if (EmpModPlugin.IsUIEmpDisabled)
            {
                bool isActive = Traverse.Create(__instance).Field("nightVisActive").GetValue<bool>();
                return isActive;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(VirtualMFD), "VirtualMFD_onMapMaximized")]
    public static class VirtualMFDMapMaximizePatch
    {
        [HarmonyPrefix]
        static bool Prefix(VirtualMFD __instance)
        {
            if (EmpModPlugin.IsUIEmpDisabled)
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(CombatHUD), "SetAircraft")]
    public static class CombatHUD_SetAircraft_Patch
    {
        static void Postfix(CombatHUD __instance, Aircraft aircraft)
        {
            if (aircraft == null) return;
    
            if (EmpModPlugin.IsUIEmpDisabled)
            {
                EmpModPlugin.RestorePlayerUI();   // вызываем из EmpModPlugin
                EmpModPlugin.IsUIEmpDisabled = false;
            }
            else
            {
                if (SceneSingleton<DynamicMap>.i != null && !SceneSingleton<DynamicMap>.i.gameObject.activeSelf)
                    SceneSingleton<DynamicMap>.i.gameObject.SetActive(true);
            }
        }
    }

    public class SparkTracker : MonoBehaviour
    {
        private Transform followTarget;
        public void Init(Transform t) => followTarget = t;
        void Update() { if (followTarget != null) transform.position = followTarget.position; }
    }

    public class MonoBehaviourHack : MonoBehaviour { }
    public class DisplayFlag : MonoBehaviour { }
}