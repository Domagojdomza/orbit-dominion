using System;
using System.IO;
using UnityEngine;

namespace OrbitDominion
{
    // This slice saves discovery only. Full empire/ship saves are intentionally a later phase.
    public sealed class DiscoveryArchive
    {
        public string Path {get;}
        public DiscoveryArchive(string path)=>Path=path;
        DiscoverySave Read(string path,DiscoveryCatalog catalog)
        {
            var save=JsonUtility.FromJson<DiscoverySave>(File.ReadAllText(path));
            if(!catalog.IsValid(save))throw new InvalidDataException("Unsupported or corrupt discovery archive.");
            return save;
        }
        public string Load(DiscoveryCatalog catalog)
        {
            if(!File.Exists(Path))return "NEW SURVEY LOG";
            try{catalog.Restore(Read(Path,catalog));return "SURVEY LOG RESTORED";}
            catch(Exception e) when(e is IOException || e is InvalidDataException || e is ArgumentException)
            {
                if(File.Exists(Path+".bak"))
                {
                    try{catalog.Restore(Read(Path+".bak",catalog));return "BACKUP RESTORED / MAIN FILE NEEDS REPAIR";}
                    catch(Exception backupError) when(backupError is IOException || backupError is InvalidDataException || backupError is ArgumentException) { }
                }
                return "ARCHIVE INVALID / ORIGINAL FILE PRESERVED";
            }
        }
        public void Save(DiscoveryCatalog catalog)
        {
            // Never replace a corrupt/incompatible previous save, including after backup recovery.
            if(File.Exists(Path))Read(Path,catalog);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string temp=Path+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(catalog.Snapshot(),true));
            Read(temp,catalog);
            if(File.Exists(Path))File.Replace(temp,Path,Path+".bak");else File.Move(temp,Path);
        }
    }
}
