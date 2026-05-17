using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NuclearOption;
using NuclearOption.Networking;

namespace NuclearOptionEmpMod
{
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
}