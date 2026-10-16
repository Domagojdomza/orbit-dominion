using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace OrbitDominion.Editor
{
    public static class PrototypeSetup
    {
        public const string ScenePath="Assets/OrbitDominion/Scenes/Solace.unity";
        [MenuItem("Tools/Orbit Dominion/Setup Prototype")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/OrbitDominion/Settings"); Directory.CreateDirectory("Assets/OrbitDominion/Scenes");
            AssetDatabase.Refresh();
            var settings=AssetDatabase.LoadAssetAtPath<FlightSettings>("Assets/OrbitDominion/Settings/Flight.asset");
            if(settings==null){settings=ScriptableObject.CreateInstance<FlightSettings>();AssetDatabase.CreateAsset(settings,"Assets/OrbitDominion/Settings/Flight.asset");}
            var discovery=AssetDatabase.LoadAssetAtPath<DiscoverySettings>("Assets/OrbitDominion/Settings/Discovery.asset");
            if(discovery==null){discovery=ScriptableObject.CreateInstance<DiscoverySettings>();AssetDatabase.CreateAsset(discovery,"Assets/OrbitDominion/Settings/Discovery.asset");}
            const string inputPath="Assets/OrbitDominion/Settings/Flight.inputactions";
            var sector=AssetDatabase.LoadAssetAtPath<SectorSettings>("Assets/OrbitDominion/Settings/Sector.asset");
            if(sector==null){sector=ScriptableObject.CreateInstance<SectorSettings>();AssetDatabase.CreateAsset(sector,"Assets/OrbitDominion/Settings/Sector.asset");}
            var combat=AssetDatabase.LoadAssetAtPath<CombatSettings>("Assets/OrbitDominion/Settings/Combat.asset");
            if(combat==null){combat=ScriptableObject.CreateInstance<CombatSettings>();AssetDatabase.CreateAsset(combat,"Assets/OrbitDominion/Settings/Combat.asset");}
            var empire=AssetDatabase.LoadAssetAtPath<EmpireSettings>("Assets/OrbitDominion/Settings/Empire.asset");
            if(empire==null){empire=ScriptableObject.CreateInstance<EmpireSettings>();AssetDatabase.CreateAsset(empire,"Assets/OrbitDominion/Settings/Empire.asset");}
            if(!File.Exists(inputPath))
            {
                var actions=ScriptableObject.CreateInstance<InputActionAsset>();var map=actions.AddActionMap("Flight");
                var thrust=map.AddAction("Throttle",InputActionType.Value);thrust.AddCompositeBinding("1DAxis").With("Positive","<Keyboard>/w").With("Negative","<Keyboard>/s");
                var steer=map.AddAction("Steer",InputActionType.Value);steer.AddBinding("<Mouse>/delta");steer.AddBinding("<Gamepad>/rightStick");
                steer.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
                var roll=map.AddAction("Roll",InputActionType.Value);roll.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/q").With("Positive","<Keyboard>/e");
                map.AddAction("Boost",InputActionType.Button,"<Keyboard>/leftShift");map.AddAction("Brake",InputActionType.Button,"<Keyboard>/space");
                map.AddAction("Pulse",InputActionType.Button,"<Keyboard>/p");map.AddAction("Pause",InputActionType.Button,"<Keyboard>/escape");
                File.WriteAllText(inputPath,actions.ToJson());Object.DestroyImmediate(actions);AssetDatabase.ImportAsset(inputPath);
            }
            var panel=AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/OrbitDominion/Settings/Hud.asset");
            // Add phase-2 actions without replacing existing bindings or user tuning.
            var upgraded=Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath));bool inputChanged=false;
            var flightMap=upgraded.FindActionMap("Flight",true);
            foreach(var binding in new[]{new[]{"SectorMap","<Keyboard>/m"},new[]{"Capture","<Keyboard>/g"},new[]{"Protect","<Keyboard>/h"},new[]{"Investigate","<Keyboard>/i"},new[]{"Land","<Keyboard>/l"}})
                if(flightMap.FindAction(binding[0])==null){flightMap.AddAction(binding[0],InputActionType.Button,binding[1]);inputChanged=true;}
            foreach(var binding in new[]{new[]{"Scan","<Keyboard>/f"},new[]{"Target","<Keyboard>/tab"},new[]{"Journal","<Keyboard>/j"},new[]{"SaveSurvey","<Keyboard>/f5"},new[]{"Colonize","<Keyboard>/c"},new[]{"Manage","<Keyboard>/b"},new[]{"Fire","<Mouse>/leftButton"},new[]{"CombatTarget","<Keyboard>/r"}})
                if(flightMap.FindAction(binding[0])==null){flightMap.AddAction(binding[0],InputActionType.Button,binding[1]);inputChanged=true;}
            if(inputChanged){File.WriteAllText(inputPath,upgraded.ToJson());AssetDatabase.ImportAsset(inputPath);}
            Object.DestroyImmediate(upgraded);
            if(panel==null){panel=ScriptableObject.CreateInstance<PanelSettings>();panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1600,900);AssetDatabase.CreateAsset(panel,"Assets/OrbitDominion/Settings/Hud.asset");}
            panel.themeStyleSheet=AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/OrbitDominion/Resources/FlightTheme.tss");EditorUtility.SetDirty(panel);
            // Referenced material assets retain the needed Lit variants without including every variant.
            foreach(string materialName in new[]{"ShipSurface","ShipEmission"})
            {
                string path="Assets/OrbitDominion/Resources/"+materialName+".mat";
                if(AssetDatabase.LoadAssetAtPath<Material>(path)==null)
                {
                    var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if(materialName=="ShipEmission"){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.cyan);}
                    AssetDatabase.CreateAsset(material,path);
                }
                if(materialName=="ShipEmission")
                {
                    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
                    material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);
                }
            }
            const string volumePath="Assets/OrbitDominion/Settings/CinematicVolume.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumePath);
            if(profile==null)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,volumePath);
                var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.35f);bloom.threshold.Override(1.15f);bloom.scatter.Override(.65f);
                profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                profile.Add<Vignette>(true).intensity.Override(.18f);
                foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
                EditorUtility.SetDirty(profile);
            }
            if(!profile.TryGet<ColorAdjustments>(out _))
            {
                var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.08f);color.contrast.Override(7);color.saturation.Override(3);
                AssetDatabase.AddObjectToAsset(color,profile);EditorUtility.SetDirty(profile);
            }
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if(pipeline==null)throw new System.InvalidOperationException("Official URP template PC pipeline is missing.");
            pipeline.supportsHDR=true;pipeline.msaaSampleCount=4;GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            PlayerSettings.companyName="Orbit Dominion";PlayerSettings.productName="Orbit Dominion";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
            var playerSettings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var activeInput=playerSettings.FindProperty("activeInputHandler");if(activeInput!=null){activeInput.intValue=1;playerSettings.ApplyModifiedPropertiesWithoutUndo();}
            // Retain procedurally referenced shaders in standalone builds.
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            for(int i=shaders.arraySize-1;i>=0;i--)
                if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==Shader.Find("Universal Render Pipeline/Lit"))
                {shaders.GetArrayElementAtIndex(i).objectReferenceValue=null;shaders.DeleteArrayElementAtIndex(i);}
            foreach(string name in new[]{"Orbit Dominion/Planet","Orbit Dominion/Atmosphere","Orbit Dominion/Space","Orbit Dominion/Rings","Orbit Dominion/Scan","Orbit Dominion/SurfaceTerrain","Orbit Dominion/SurfaceWater","Orbit Dominion/SurfaceSky"})
            {
                var shader=Shader.Find(name);bool exists=false;for(int i=0;i<shaders.arraySize;i++)exists|=shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader;
                if(!exists&&shader!=null){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;}
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
            if(!File.Exists(ScenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var root=new GameObject("Orbit Dominion").AddComponent<PrototypeBootstrap>();root.settings=settings;root.input=AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath);root.panelSettings=panel;root.cinematicProfile=profile;
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            else EditorSceneManager.OpenScene(ScenePath);
            var bootstrap=Object.FindAnyObjectByType<PrototypeBootstrap>();
            if(bootstrap!=null&&bootstrap.sectorSettings==null)
            {
                bootstrap.sectorSettings=sector;EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
            }
            if(bootstrap!=null&&bootstrap.combatSettings==null)
            {
                bootstrap.combatSettings=combat;EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
            }
            if(bootstrap!=null&&bootstrap.empireSettings==null)
            {
                bootstrap.empireSettings=empire;EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
            }
            if(bootstrap!=null&&bootstrap.discoverySettings==null)
            {
                bootstrap.discoverySettings=discovery;EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
            }
            if(bootstrap!=null && bootstrap.cinematicProfile==null)
            {
                bootstrap.cinematicProfile=profile;EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();
            Debug.Log("ORBIT_SETUP_OK: Solace scene, URP, input and configuration ready.");
        }
        [MenuItem("Tools/Orbit Dominion/Build Windows")]
        public static void BuildWindows()
        {
            Setup();Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Windows/OrbitDominion.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Windows build failed: "+report.summary.result);
            Debug.Log("ORBIT_BUILD_OK: "+report.summary.totalSize+" bytes");
        }
    }
}
