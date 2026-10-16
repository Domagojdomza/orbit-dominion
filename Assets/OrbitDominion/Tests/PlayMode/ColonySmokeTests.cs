using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrbitDominion.Tests
{
    public sealed class ColonySmokeTests
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
        string Prepare(PrototypeBootstrap w)
        {
            var planet=w.Worlds[1];string id=planet.Definition.id;
            w.Player.transform.SetPositionAndRotation(planet.transform.position-Vector3.forward*(planet.Definition.radius+150),Quaternion.identity);
            w.Discovery.Catalog.Detect(id);w.Discovery.Catalog.CompleteScan(id);w.Discovery.SelectTarget(planet);return id;
        }
        [UnityTest] public IEnumerator CLaunchesCraftAndCompletesVisibleColony()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();string id=Prepare(w);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));yield return null;yield return null;
            Assert.IsTrue(w.Empire.State.Get(id).deploying);Assert.IsNotNull(GameObject.Find("Colony deployment craft"));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(w.empireSettings.deploymentSeconds+.2f);
            Assert.IsTrue(w.Empire.State.Get(id).owned);Assert.AreEqual(1,w.Worlds[1].GetComponent<ColonyView>().VisibleDevelopment);
            Assert.IsNotNull(GameObject.Find("Colony arcology"));Assert.IsNull(GameObject.Find("Colony deployment craft"));
        }
        [UnityTest] public IEnumerator ManagementInputPausesEconomyAndEscapeResumesConstruction()
        {
            yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();string id=Prepare(w);
            Assert.IsTrue(w.Empire.Colonize());w.Empire.State.Tick(w.empireSettings.deploymentSeconds,id,false);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));yield return null;yield return null;
            Assert.IsTrue(w.Empire.ManagementOpen);Assert.IsTrue(w.Player.Paused);Assert.IsTrue(w.Empire.Build(BuildingKind.Energy));
            float remaining=w.Empire.State.Get(id).constructionRemaining,money=w.Empire.State.Minerals;
            yield return new WaitForSecondsRealtime(.15f);Assert.AreEqual(remaining,w.Empire.State.Get(id).constructionRemaining);Assert.AreEqual(money,w.Empire.State.Minerals);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
            Assert.IsFalse(w.Empire.ManagementOpen);Assert.IsFalse(w.Player.Paused);
            Assert.Less(w.Empire.State.Get(id).constructionRemaining,remaining);
        }
        [UnityTest] public IEnumerator BuildingCompletionAddsSatelliteAndReloadRestoresColonyWithoutDuplicates()
        {
            string folder=Path.Combine(Path.GetTempPath(),"OrbitColonyScene-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(folder,"empire.json");
            try
            {
                yield return Load();var w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();w.Empire.Initialize(w,path);string id=Prepare(w);
                w.Empire.Colonize();w.Empire.State.Tick(12,id,false);w.Empire.State.Tick(60,null,false);
                Assert.IsTrue(w.Empire.State.StartBuilding(id,BuildingKind.Defense));w.Empire.State.Tick(18,null,false);yield return null;yield return null;
                Assert.AreEqual(1,w.Worlds[1].GetComponent<ColonyView>().VisibleDefenses);
                Assert.IsNotNull(w.Worlds[1].transform.Find("Colony infrastructure/Defense satellite"));
                w.Empire.SaveNow();yield return Load();w=UnityEngine.Object.FindAnyObjectByType<PrototypeBootstrap>();w.Empire.Initialize(w,path);yield return null;yield return null;
                Assert.IsTrue(w.Empire.State.Get(id).owned);Assert.IsTrue(w.Discovery.Catalog.HasDetails(id));
                Assert.AreEqual(1,w.Worlds[1].GetComponent<ColonyView>().VisibleDefenses);
                Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<EmpireController>().Length);Assert.AreEqual(12,UnityEngine.Object.FindObjectsByType<ColonyView>().Length);
                // Detach test archive before teardown/application quit.
                w.Empire.Initialize(w,null);
            }
            finally{foreach(string file in new[]{path,path+".bak",path+".tmp"})if(File.Exists(file))File.Delete(file);if(Directory.Exists(folder))Directory.Delete(folder);}
        }
    }
}

