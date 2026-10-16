using System;
using UnityEngine;
namespace OrbitDominion
{
    public enum CombatFaction { Union, Hostile, Neutral }
    public enum CombatRole { Player, Fighter, Satellite, Command }
    public sealed class VitalState
    {
        public float Hull {get;private set;}
        public float Shield {get;private set;}
        public float MaxHull {get;}
        public float MaxShield {get;}
        public float SinceHit {get;private set;}
        public bool Alive=>Hull>0;
        public VitalState(float hull,float shield){MaxHull=hull;MaxShield=shield;Reset();}
        public void Reset(){Hull=MaxHull;Shield=MaxShield;SinceHit=0;}
        public bool Restore(float hull,float shield,float sinceHit)
        {
            if(!SectorState.Finite(hull)||!SectorState.Finite(shield)||!SectorState.Finite(sinceHit)||hull<0||hull>MaxHull||shield<0||shield>MaxShield||sinceHit<0)return false;
            Hull=hull;Shield=shield;SinceHit=sinceHit;return true;
        }
        public bool Damage(float amount,CombatFaction attacker,CombatFaction defender,bool protectedShip=false)
        {
            if(!Alive||protectedShip||attacker==defender||float.IsNaN(amount)||float.IsInfinity(amount)||amount<=0)return false;
            SinceHit=0;float absorbed=Math.Min(Shield,amount);Shield-=absorbed;Hull=Math.Max(0,Hull-(amount-absorbed));return true;
        }
        public void Tick(float dt,float delay,float recharge)
        {
            if(!Alive||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            float before=SinceHit;SinceHit+=dt;
            Shield=Math.Min(MaxShield,Shield+Math.Max(0,SinceHit-Math.Max(before,delay))*recharge);
        }
    }
    public static class CombatRules
    {
        // First intersection along a normalized ray. Origins within a sphere are obstructed.
        public static float SphereHit(Vector3 origin,Vector3 direction,Vector3 center,float radius)
        {
            Vector3 offset=origin-center;float c=offset.sqrMagnitude-radius*radius;if(c<=0)return 0;
            float b=Vector3.Dot(offset,direction),discriminant=b*b-c;
            if(discriminant<0)return float.PositiveInfinity;
            float distance=-b-Mathf.Sqrt(discriminant);return distance>=0?distance:float.PositiveInfinity;
        }
        public static bool CanFire(bool alive,bool paused,bool pulse,float cooldown)=>alive&&!paused&&!pulse&&cooldown<=0;
    }
}
