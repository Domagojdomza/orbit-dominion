using UnityEngine;
namespace OrbitDominion
{
    public sealed class RuntimeMetrics : MonoBehaviour
    {
        float warmup=5,seconds,worst;int frames,slowFrames;
        void Update(){if(Time.timeScale==0)return;float dt=Time.unscaledDeltaTime;if(warmup>0){warmup-=dt;return;}seconds+=dt;frames++;worst=Mathf.Max(worst,dt);if(dt>.0334f)slowFrames++;}
        void OnApplicationQuit(){if(frames>0)Debug.Log($"ORBIT_FRAME_SAMPLE: frames={frames}, seconds={seconds:F2}, averageFPS={frames/seconds:F1}, worstMs={worst*1000:F1}, framesOver33ms={slowFrames}");}
    }
}
