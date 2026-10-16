using System;
using System.Collections.Generic;
using UnityEngine;
namespace OrbitDominion
{
    [Serializable] public sealed class ActorSave
    {
        public string id,homePlanet,fleetId;public CombatFaction faction;public CombatRole role;
        public Vector3 position,destination;public Quaternion rotation=Quaternion.identity;
        public float hull,shield,sinceHit,maxHull,maxShield,cooldown,repair;public bool traveling;
    }
    [Serializable] public sealed class CombatSave
    {
        public List<ActorSave> actors=new();public float respawn,protection,warning;public bool engaged;public string target;
    }
    [Serializable] public sealed class GameSave
    {
        public int version=2,seed;public EmpireSave empire;public SectorSave sector;public CombatSave combat;
        public Vector3 playerPosition,velocity;public Quaternion playerRotation=Quaternion.identity;
        public bool pulse;public float combatLock,orbitalSeconds;public string planetTarget;public SurfaceSave surface;
    }
}
