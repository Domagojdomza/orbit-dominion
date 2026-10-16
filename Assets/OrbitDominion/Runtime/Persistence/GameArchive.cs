using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace OrbitDominion
{
    public sealed class GameArchive
    {
        public string Path {get;}
        readonly Func<GameSave,bool> validate;
        public GameArchive(string path,Func<GameSave,bool> validate){Path=path;this.validate=validate;}
        GameSave Read(string path)
        {
            GameSave save;try{save=JsonUtility.FromJson<GameSave>(File.ReadAllText(path));}catch(ArgumentException e){throw new InvalidDataException("Invalid game save",e);}
            if(!validate(save))throw new InvalidDataException("Unsupported or corrupt game save");return save;
        }
        public GameSave Load(out string status)
        {
            if(!File.Exists(Path)){status="NEW FLIGHT";return null;}
            try{var save=Read(Path);status="FLIGHT RESTORED";return save;}
            catch(Exception e)when(e is IOException||e is InvalidDataException)
            {
                if(File.Exists(Path+".bak"))try{var backup=Read(Path+".bak");status="BACKUP RESTORED / PRIMARY PRESERVED; SAVE BLOCKED";return backup;}catch(Exception b)when(b is IOException||b is InvalidDataException){}
                status="SAVE INVALID / ORIGINAL PRESERVED; SAVE BLOCKED";return null;
            }
        }
        public void Save(GameSave save)
        {
            if(!validate(save))throw new InvalidDataException("Invalid current state");
            if(File.Exists(Path))Read(Path);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));File.WriteAllText(Path+".tmp",JsonUtility.ToJson(save,true));Read(Path+".tmp");
            if(File.Exists(Path))File.Replace(Path+".tmp",Path,Path+".bak");else File.Move(Path+".tmp",Path);
        }
        public static bool ValidActor(ActorSave a)
        {
            if(a==null||string.IsNullOrEmpty(a.id)||a.id.Length>120||!Enum.IsDefined(typeof(CombatFaction),a.faction)||!Enum.IsDefined(typeof(CombatRole),a.role))return false;
            foreach(float f in new[]{a.hull,a.shield,a.sinceHit,a.maxHull,a.maxShield,a.cooldown,a.repair})if(!SectorState.Finite(f)||f<0||f>10000000)return false;
            return a.maxHull>0&&a.maxHull<=1000&&a.maxShield<=1000&&a.hull<=a.maxHull&&a.shield<=a.maxShield&&ValidVector(a.position)&&ValidVector(a.destination)&&ValidRotation(a.rotation);
        }
        public static bool ValidVector(Vector3 v)=>SectorState.Finite(v.x)&&SectorState.Finite(v.y)&&SectorState.Finite(v.z)&&v.sqrMagnitude<1000000000000f;
        public static bool ValidRotation(Quaternion q){float n=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;return SectorState.Finite(n)&&n>.99f&&n<1.01f;}
    }
}
