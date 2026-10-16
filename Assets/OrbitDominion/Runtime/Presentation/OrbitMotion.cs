using UnityEngine;
namespace OrbitDominion
{
    public sealed class OrbitMotion : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float degreesPerSecond = 2;
        void Update() => transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
