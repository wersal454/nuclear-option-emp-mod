using System.Linq;
using HarmonyLib;
using NuclearOption;
using UnityEngine;

namespace NuclearOptionEmpMod
{
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
}