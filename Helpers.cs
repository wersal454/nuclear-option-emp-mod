using UnityEngine;

namespace NuclearOptionEmpMod
{
    public class MonoBehaviourHack : MonoBehaviour { }

    public class SparkTracker : MonoBehaviour
    {
        private Transform followTarget;
        public void Init(Transform t) => followTarget = t;
        void Update() { if (followTarget != null) transform.position = followTarget.position; }
    }

    public class DisplayFlag : MonoBehaviour { }
}