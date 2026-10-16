using System.Collections;
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrbitDominion.Tests
{
    public class DiscoverySmokeTests
    {
        Keyboard keyboard;
        InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
        [SetUp] public void Setup()
        {
            PrototypeBootstrap.SuppressPersistence=true;background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard=InputSystem.AddDevice<Keyboard>();
        }
        [TearDown] public void Cleanup()
        {
            InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=background;PrototypeBootstrap.SuppressPersistence=false;Time.timeScale=1;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
        }
        IEnumerator Load(){yield return SceneManager.LoadSceneAsync("Solace");yield return null;yield return null;}
        void Approach(PrototypeBootstrap world,int index,float clearance)
        {
            var planet=world.Worlds[index];world.Player.transform.SetPositionAndRotation(planet.transform.position-Vector3.forward*(planet.Definition.radius+clearance),Quaternion.identity);
        }
        [UnityTest] public IEnumerator DetectionRevealsContactButNotProperties()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();var id=w.Worlds[1].Definition.id;
            Assert.AreEqual(DiscoveryState.Unknown,w.Discovery.Catalog.State(id));
            Approach(w,1,1800);yield return null;yield return null;
            Assert.AreEqual(DiscoveryState.Detected,w.Discovery.Catalog.State(id));Assert.AreEqual("UNKNOWN PLANET",w.Discovery.TargetLabel);
        }
        [UnityTest] public IEnumerator HoldingFScansThenCloseOrbitVerifiesStrategy()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();Approach(w,1,400);yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return new WaitForSeconds(1);
            Assert.Greater(w.Discovery.Scan.Progress,0);Assert.IsFalse(w.Discovery.Catalog.HasDetails(w.Worlds[1].Definition.id));
            Assert.IsTrue(GameObject.Find("Holographic scan sweep").activeInHierarchy);
            yield return new WaitForSeconds(w.discoverySettings.scanSeconds);
            Assert.AreEqual(DiscoveryState.Scanned,w.Discovery.Catalog.State(w.Worlds[1].Definition.id));Assert.AreEqual("AURELIA II",w.Discovery.TargetLabel);
            Approach(w,1,150);yield return null;yield return null;
            Assert.AreEqual(DiscoveryState.Visited,w.Discovery.Catalog.State(w.Worlds[1].Definition.id));
        }
        [UnityTest] public IEnumerator LeavingScanRangeResetsProgressWithoutDiscovery()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();Approach(w,1,400);yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return new WaitForSeconds(1);
            Assert.Greater(w.Discovery.Scan.Progress,0);Approach(w,1,800);yield return null;yield return null;
            Assert.AreEqual(0,w.Discovery.Scan.Progress);Assert.IsFalse(w.Discovery.Catalog.HasDetails(w.Worlds[1].Definition.id));
        }
        [UnityTest] public IEnumerator JournalPausesAndClosesWithKeyboard()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));yield return null;yield return null;
            Assert.IsTrue(w.Discovery.JournalOpen);Assert.IsTrue(w.Player.Paused);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));yield return null;yield return null;
            Assert.IsFalse(w.Discovery.JournalOpen);Assert.IsFalse(w.Player.Paused);
        }
        [UnityTest] public IEnumerator TargetCycleSkipsUnknownWorldsAndCanNavigateHome()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();Approach(w,1,400);yield return null;yield return null;
            Assert.AreEqual(w.Worlds[1],w.Discovery.Target);Assert.AreEqual(DiscoveryState.Unknown,w.Discovery.Catalog.State(w.Worlds[2].Definition.id));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return null;yield return null;
            Assert.AreEqual(w.Worlds[0],w.Discovery.Target);Assert.AreEqual("MERIDIAN",w.Discovery.TargetLabel);
        }
        [UnityTest] public IEnumerator ReloadReconstructsDiscoveriesFromArchiveWithoutDuplicates()
        {
            string folder=Path.Combine(Path.GetTempPath(),"OrbitSceneTest-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"survey.json");
            try
            {
                yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();
                w.Discovery.Initialize(w.Player,w.Worlds,w.discoverySettings,w.settings.galaxySeed,w.Beacon,path);
                string id=w.Worlds[1].Definition.id;w.Discovery.Catalog.Detect(id);w.Discovery.Catalog.CompleteScan(id);w.Discovery.Catalog.Visit(id);w.Discovery.Catalog.ReachBeacon();
                w.Discovery.ToggleJournal();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F5));yield return null;yield return null;
                Assert.IsTrue(File.Exists(path),"F5 must save even while the exploration journal pauses flight.");
                w.Discovery.ToggleJournal();InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                yield return Load();w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();
                w.Discovery.Initialize(w.Player,w.Worlds,w.discoverySettings,w.settings.galaxySeed,w.Beacon,path);
                Assert.AreEqual(DiscoveryState.Visited,w.Discovery.Catalog.State(id));Assert.IsTrue(w.Discovery.Catalog.BeaconReached);
                Assert.AreEqual("AURELIA II",w.Discovery.TargetLabel);Assert.AreEqual(12,UnityEngine.Object.FindObjectsByType<PlanetView>().Length);
                Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<DiscoveryController>().Length);
            }
            finally{foreach(string file in new[]{path,path+".bak",path+".tmp"})if(File.Exists(file))File.Delete(file);Directory.Delete(folder);}
        }
    }
}


