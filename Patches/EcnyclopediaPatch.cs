using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Mirage;
using NuclearOption;
using NuclearOption.Networking;

namespace NuclearOptionEmpMod
{
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
}