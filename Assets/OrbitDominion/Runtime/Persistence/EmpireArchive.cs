using System.IO;
using UnityEngine;

namespace OrbitDominion
{
    public sealed class EmpireArchive
    {
        public string Path {get;}
        public EmpireArchive(string path)=>Path=path;
        EmpireSave Read(string path,EmpireState state,DiscoveryCatalog discovery)
        {
            EmpireSave save;
            try{save=JsonUtility.FromJson<EmpireSave>(File.ReadAllText(path));}
            catch(System.ArgumentException e){throw new InvalidDataException("Invalid colony archive",e);}
            if(!state.IsValid(save,discovery))throw new InvalidDataException("Incompatible colony archive");return save;
        }
        public string Load(EmpireState state,DiscoveryCatalog discovery)
        {
            if(!File.Exists(Path))return "NEW EMPIRE / SURVEY LOG RETAINED";
            try{state.Restore(Read(Path,state,discovery),discovery);return "EMPIRE RESTORED";}
            catch(System.Exception e)when(e is IOException||e is InvalidDataException)
            {
                if(File.Exists(Path+".bak"))try{state.Restore(Read(Path+".bak",state,discovery),discovery);return "BACKUP RESTORED / PRIMARY NEEDS REPAIR";}catch(System.Exception backupError)when(backupError is IOException||backupError is InvalidDataException){}
                return "INVALID EMPIRE ARCHIVE / ORIGINAL PRESERVED";
            }
        }
        public void Save(EmpireState state,DiscoveryCatalog discovery)
        {
            if(File.Exists(Path))Read(Path,state,discovery);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            File.WriteAllText(Path+".tmp",JsonUtility.ToJson(state.Snapshot(discovery),true));Read(Path+".tmp",state,discovery);
            if(File.Exists(Path))File.Replace(Path+".tmp",Path,Path+".bak");else File.Move(Path+".tmp",Path);
        }
    }
}
