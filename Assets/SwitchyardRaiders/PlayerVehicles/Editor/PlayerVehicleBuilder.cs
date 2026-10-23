using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using SwitchyardRaiders.Startup;
using Object = UnityEngine.Object;

namespace SwitchyardRaiders.PlayerVehicles.Editor
{
    public static class PlayerVehicleBuilder
    {
        public const string Root = "Assets/SwitchyardRaiders/PlayerVehicles";
        public const string Output = "Deliverables/PlayerVehicles";
        public static readonly string[] Names = { "Player01_Adler", "Player02_Falke" };
        [MenuItem("Switchyard Raiders/Player Vehicles/Build prefabs")]
        public static void Build()
        {
            if (!Application.dataPath.Replace('\\','/').EndsWith("/Switchyard Raiders/Assets")) throw new InvalidOperationException("Wrong Unity project");
            Directory.CreateDirectory(Root + "/Prefabs"); Directory.CreateDirectory(Root + "/Animations");
            AssetDatabase.Refresh();
            foreach (var map in new[] { "BaseColor", "Normal", "Mask" })
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"/Art/Atlas_"+map+".png");
                ti.textureType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                ti.sRGBTexture=map=="BaseColor";ti.alphaSource=map=="Mask"?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
                ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.streamingMipmaps=true;ti.isReadable=false;
                ti.wrapMode=TextureWrapMode.Clamp;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;
                ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_6x6,compressionQuality=100});
                ti.SaveAndReimport();
            }
            var mat=Material("SharedAtlas","Universal Render Pipeline/Lit");
            Texture2D Tex(string n)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Atlas_"+n+".png");
            mat.SetTexture("_BaseMap",Tex("BaseColor"));mat.SetColor("_BaseColor",Color.white);
            mat.SetTexture("_BumpMap",Tex("Normal"));mat.SetFloat("_BumpScale",.8f);
            mat.SetTexture("_MetallicGlossMap",Tex("Mask"));mat.SetTexture("_OcclusionMap",Tex("Mask"));
            mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",1);mat.SetFloat("_OcclusionStrength",.65f);
            mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.EnableKeyword("_OCCLUSIONMAP");mat.enableInstancing=true;
            var glass=Material("ThinCabGlass","Universal Render Pipeline/Lit");
            glass.SetColor("_BaseColor",new Color(.30f,.42f,.44f,.14f));glass.SetFloat("_Surface",1);
            glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite",0);glass.SetFloat("_Smoothness",.82f);glass.SetFloat("_Metallic",0);
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=(int)RenderQueue.Transparent;
            glass.SetOverrideTag("RenderType","Transparent");glass.SetShaderPassEnabled("ShadowCaster",false);
            var report=new StringBuilder("Unity "+Application.unityVersion+" | imported asset accounting\n");
            foreach(var name in Names) BuildOne(name,mat,glass,report);
            foreach(var tex in new[]{Tex("BaseColor"),Tex("Normal"),Tex("Mask")})
                report.AppendLine(tex.name+": "+tex.width+"x"+tex.height+" "+tex.format+" runtime bytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex));
            report.AppendLine("Textures shared by both models. Android ASTC 6x6 2048 with mipmaps; this editor memory is not a device allocation measurement.");
            File.WriteAllText(Output+"/Validation/unity_import.txt",report.ToString());AssetDatabase.SaveAssets();Debug.Log(report.ToString());
        }
        private static Material Material(string name,string shader)
        {
            var path=Root+"/Art/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}return m;
        }
        [MenuItem("Switchyard Raiders/Player Vehicles/Install in existing tabletop")]
        public static void InstallIntegration()
        {
            void Attach(RaidersTable table)
            {
                var adapter=table.GetComponent<PlayerVehicleTableIntegration>();
                if(!adapter)adapter=table.gameObject.AddComponent<PlayerVehicleTableIntegration>();
                adapter.table=table;adapter.playerPrefabs=Names.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+n+".prefab")).ToArray();
                adapter.modelScale=.004f;EditorUtility.SetDirty(adapter);
            }
            const string launch="Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab";
            Directory.CreateDirectory(Output+"/Validation/BeforeIntegration");
            if(!File.Exists(Output+"/Validation/BeforeIntegration/SR_Launch.prefab"))File.Copy(launch,Output+"/Validation/BeforeIntegration/SR_Launch.prefab");
            var contents=PrefabUtility.LoadPrefabContents(launch);
            try{foreach(var t in contents.GetComponentsInChildren<RaidersTable>(true))Attach(t);PrefabUtility.SaveAsPrefabAsset(contents,launch);}
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            foreach(var table in Object.FindObjectsByType<RaidersTable>(FindObjectsInactive.Include))
            {
                if(table.gameObject.scene.path!="Assets/SwitchyardRaiders/Startup/Scenes/SR_Launch.unity")continue;
                if(table.gameObject.scene.isDirty)throw new InvalidOperationException("Main scene has unsaved edits; prefab integration saved, scene left alone.");
                if(!File.Exists(Output+"/Validation/BeforeIntegration/SR_Launch.unity"))File.Copy(table.gameObject.scene.path,Output+"/Validation/BeforeIntegration/SR_Launch.unity");
                Attach(table);EditorSceneManager.MarkSceneDirty(table.gameObject.scene);EditorSceneManager.SaveScene(table.gameObject.scene);
            }
            AssetDatabase.SaveAssets();
        }
        private static void BuildOne(string name,Material mat,Material glass,StringBuilder report)
        {
            string path=Root+"/Art/"+name+".fbx";
            var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.globalScale=1;imp.useFileScale=true;
            imp.animationType=ModelImporterAnimationType.Generic;imp.importAnimation=true;imp.importCameras=false;imp.importLights=false;
            imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.CalculateMikk;
            imp.meshCompression=ModelImporterMeshCompression.Off;imp.isReadable=true;imp.optimizeGameObjects=false;
            imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.animationCompression=ModelImporterAnimationCompression.Off;
            imp.SaveAndReimport();
            var takes=imp.defaultClipAnimations;
            foreach(var take in takes)
            {
                foreach(var clipName in new[]{"Forward","Reverse","Doors_Open","Doors_Close","Controls"})
                    if(take.name.EndsWith(clipName))take.name=clipName;
                take.loopTime=take.name=="Forward"||take.name=="Reverse"||take.name=="Controls";
                take.lockRootRotation=true;take.lockRootPositionXZ=true;take.lockRootHeightY=true;
            }
            imp.clipAnimations=takes;imp.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            var controllerPath=Root+"/Animations/"+name+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm=controller.layers[0].stateMachine;
            foreach(var stateName in new[]{"Forward","Reverse","Doors_Open","Doors_Close","Controls"})
            {
                var clip=clips.Single(c=>c.name==stateName);
                var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==stateName)??sm.AddState(stateName);
                state.motion=clip;state.speed=1;state.speedParameterActive=false;state.writeDefaultValues=false;
                if(stateName=="Forward")sm.defaultState=state;
                report.AppendLine(name+" clip "+clip.name+": "+clip.length+" sec; bindings="+AnimationUtility.GetCurveBindings(clip).Length+" loop="+clip.isLooping);
            }
            var root=new GameObject(name);
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
                PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var lod in model.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(lod);
                var nodes=model.GetComponentsInChildren<Transform>(true);
                Transform Find(string n)=>nodes.Single(t=>t.name==n);
                var origin=Find("Basis_Origin");
                var forward=(Find("Basis_Forward").position-origin.position).normalized;
                var up=(Find("Basis_Up").position-origin.position).normalized;
                model.transform.rotation=Quaternion.Inverse(Quaternion.LookRotation(forward,up));
                model.transform.localPosition-=origin.position;
                foreach(var marker in nodes.Where(t=>t.name.StartsWith("Coupler_")||t.name.StartsWith("CabSeat_")||t.name.StartsWith("CabStand_")||t.name.StartsWith("ControlHand_")))
                    marker.rotation=Quaternion.LookRotation(marker.name.EndsWith("Front")?Vector3.forward:Vector3.back,Vector3.up);
                foreach(var end in new[]{"Front","Rear"})Find("Coupler_"+end).SetParent(Find("CouplingPivot_"+end),true);
                var animator=model.GetComponent<Animator>();if(!animator)animator=model.AddComponent<Animator>();
                animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.enabled=true;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);var lods=new LOD[3];float[] thresholds={.55f,.025f,.004f};
                for(int i=0;i<3;i++)
                {
                    var rs=renderers.Where(r=>r.name.EndsWith("LOD"+i)).ToArray();
                    foreach(var r in rs)
                    {
                        bool glazing=r.name.Contains("Glass");r.sharedMaterials=new[]{glazing?glass:mat};r.gameObject.SetActive(true);
                        r.quality=SkinQuality.Bone1;r.updateWhenOffscreen=false;
                        r.shadowCastingMode=glazing?ShadowCastingMode.Off:ShadowCastingMode.On;
                        var bounds=r.localBounds;bounds.Expand(1.0f);r.localBounds=bounds;
                    }
                    lods[i]=new LOD(thresholds[i],rs);
                    report.AppendLine(name+" LOD"+i+": "+rs.Sum(r=>r.sharedMesh.vertexCount)+" vertices; "+rs.Sum(r=>(long)r.sharedMesh.GetIndexCount(0)/3)+" triangles; "+rs.Length+" renderers; "+rs.Sum(r=>r.sharedMaterials.Length)+" material slots");
                }
                var group=root.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.None;group.SetLODs(lods);group.RecalculateBounds();
                root.AddComponent<PlayerLocomotiveMotion>();
                void Collider(string n,Vector3 center,Vector3 size){var go=new GameObject(n);go.transform.SetParent(root.transform,false);var c=go.AddComponent<BoxCollider>();c.center=center;c.size=size;}
                Collider("ChassisCollider",new Vector3(0,.98f,0),new Vector3(2.7f,.8f,13.6f));
                Collider("BatteryCompartmentCollider",new Vector3(0,2.50f,0),new Vector3(2.65f,2.05f,8.5f));
                foreach(int end in new[]{-1,1})Collider("CabFloor_"+end,new Vector3(0,1.46f,end*5.45f),new Vector3(2.65f,.16f,2.25f));
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+name+".prefab");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [MenuItem("Switchyard Raiders/Player Vehicles/Create review scene")]
        public static void CreateReview()
        {
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.58f,.65f,.70f);RenderSettings.ambientEquatorColor=new Color(.34f,.37f,.39f);RenderSettings.ambientGroundColor=new Color(.21f,.20f,.18f);
            var floor=Material("ReviewGround","Universal Render Pipeline/Lit");floor.color=new Color(.27f,.29f,.28f);
            var steel=Material("ReviewRail","Universal Render Pipeline/Lit");steel.color=new Color(.35f,.39f,.40f);steel.SetFloat("_Metallic",.8f);
            var sleeper=Material("ReviewSleeper","Universal Render Pipeline/Lit");sleeper.color=new Color(.16f,.12f,.09f);
            GameObject Cube(string n,Vector3 pos,Vector3 size,Material m){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.position=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
            Cube("Review tabletop",new Vector3(0,-.045f,0),new Vector3(1.5f,.06f,2.2f),floor);
            for(int i=0;i<2;i++)
            {
                float x=i==0?-.28f:.28f;var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+Names[i]+".prefab"));
                go.transform.position=new Vector3(x,0,0);go.transform.localScale=Vector3.one*.08f;
                var demo=go.AddComponent<PlayerVehicleReviewDemo>();demo.basePosition=go.transform.position;
                for(int side=-1;side<=1;side+=2)Cube("1.435m rail gauge",new Vector3(x+side*.7175f*.08f,-.006f,0),new Vector3(.006f,.012f,2.0f),steel);
                for(int k=0;k<35;k++)Cube("Sleeper",new Vector3(x,-.017f,-.98f+k*.058f),new Vector3(.215f,.016f,.023f),sleeper);
            }
            var light=new GameObject("Key light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2.1f;light.color=new Color(1,.91f,.78f);light.transform.rotation=Quaternion.Euler(45,-35,0);light.shadows=LightShadows.Soft;
            var fill=new GameObject("Cool fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.55f;fill.color=new Color(.66f,.79f,1);fill.transform.rotation=Quaternion.Euler(25,135,0);
            var cam=new GameObject("ReviewCamera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(1.45f,1.1f,1.65f);cam.transform.LookAt(new Vector3(0,.13f,0));cam.nearClipPlane=.005f;cam.farClipPlane=30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.10f,.12f,.14f);cam.fieldOfView=40;
            Directory.CreateDirectory(Root+"/Scenes");EditorSceneManager.SaveScene(scene,Root+"/Scenes/PlayerVehicles_Review.unity");
            SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);AssetDatabase.SaveAssets();
        }
    }
}
