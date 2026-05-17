using HarmonyLib;
using NuclearOption;

namespace NuclearOptionEmpMod
{
    [HarmonyPatch(typeof(OpticalSeekerCruiseMissile), "SlowChecks")]
    public static class OpticalSeekerCruiseMissile_SlowChecks_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(OpticalSeekerCruiseMissile __instance)
        {
            Missile missile = Traverse.Create(__instance).Field("missile").GetValue<Missile>();
            if (missile != null && missile.targetID == PersistentID.None)
                return false;
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
            if (missile != null && missile.targetID == PersistentID.None)
                return false;
            return true;
        }
    }
}