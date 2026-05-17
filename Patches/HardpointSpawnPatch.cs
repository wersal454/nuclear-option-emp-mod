using HarmonyLib;
using NuclearOption;
using UnityEngine;

namespace NuclearOptionEmpMod
{
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
}