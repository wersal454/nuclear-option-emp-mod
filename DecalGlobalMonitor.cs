using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NuclearOptionEmpMod
{
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
}