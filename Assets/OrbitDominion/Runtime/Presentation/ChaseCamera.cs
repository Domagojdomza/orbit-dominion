using UnityEngine;
namespace OrbitDominion
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        public PlayerFlight player;
        Camera lens;
        void Awake() => lens = GetComponent<Camera>();
        void LateUpdate()
        {
            if (player == null || player.Paused) return;
            float blend = 1-Mathf.Exp(-7*Time.deltaTime);
            var desired = player.transform.position + player.transform.rotation * new Vector3(0,4.2f,-18);
            if (Vector3.Distance(transform.position,desired)>200) transform.position=desired;
            transform.position = Vector3.Lerp(transform.position,desired,blend);
            transform.rotation = Quaternion.Slerp(transform.rotation,player.transform.rotation,1-Mathf.Exp(-9*Time.deltaTime));
            float fov = player.Pulse ? player.settings.pulseFov : player.Boosting ? player.settings.boostFov : player.settings.baseFov;
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView,fov,1-Mathf.Exp(-3*Time.deltaTime));
        }
    }
}
