using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NuclearOption;

namespace NuclearOptionEmpMod
{
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
                EmpModPlugin.RestorePlayerUI();
                EmpModPlugin.IsUIEmpDisabled = false;
            }
            else
            {
                if (SceneSingleton<DynamicMap>.i != null && !SceneSingleton<DynamicMap>.i.gameObject.activeSelf)
                    SceneSingleton<DynamicMap>.i.gameObject.SetActive(true);
            }
        }
    }
}