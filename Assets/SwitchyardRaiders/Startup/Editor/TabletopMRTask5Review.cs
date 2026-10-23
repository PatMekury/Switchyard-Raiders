using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using SwitchyardRaiders.Startup;
using SwitchyardRaiders.CompanyTrain;
using SwitchyardRaiders.PlayerVehicles;
using Object=UnityEngine.Object;
namespace SwitchyardRaiders.Editor
{
    public static partial class TabletopMRTask5
    {
        const string Evidence="Deliverables/DemeoRework/Task5Evidence";
        const string Temp="Assets/__Task5EvidenceTemporary";
        static void Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1600,1100,24);rt.Create();var prior=RenderTexture.active;var tex=new Texture2D(1600,1100,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1100),0,0);tex.Apply();File.WriteAllBytes(Evidence+"/"+name+".png",tex.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=prior;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);}
        }
        static Bounds BoundsIn(Transform model,Transform space)
        {
            bool first=true;var bounds=new Bounds();
            foreach(var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var filter=r.GetComponent<MeshFilter>();var mesh=r is SkinnedMeshRenderer sk?sk.sharedMesh:filter?filter.sharedMesh:null;if(!mesh)continue;
                var b=mesh.bounds;for(int i=0;i<8;i++){var v=space?space.InverseTransformPoint(r.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)))):r.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){bounds=new Bounds(v,Vector3.zero);first=false;}else bounds.Encapsulate(v);}
            }
            return bounds;
        }
        static void DetailLOD(GameObject root)
        {
            foreach(var lod in root.GetComponentsInChildren<LODGroup>(true)){var levels=lod.GetLODs();lod.enabled=false;foreach(var l in levels)foreach(var r in l.renderers)if(r)r.enabled=false;foreach(var r in levels[0].renderers)if(r)r.enabled=true;}
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.forceMatrixRecalculationPerRender=true;
            foreach(var particle in root.GetComponentsInChildren<ParticleSystem>(true)){particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        }
        [MenuItem("Switchyard Raiders/Demeo Rework/13 - Render calculated MR comparison")]
        public static void Render()
        {
            Guard();Directory.CreateDirectory(Evidence);if(Directory.Exists(Temp))throw new Exception("Temporary evidence directory already exists; preserve/review it first.");
            GameObject current=null,old=null;
            try
            {
                // Import copies under fresh GUIDs for isolated reading. Original public assets,
                // prefabs, GUIDs and live scene remain untouched. Removed in finally.
                Directory.CreateDirectory(Temp);
                File.Copy("Deliverables/DemeoRework/Baseline/Assets/Startup/Prefabs/SR_TabletopPresentation.prefab",Temp+"/Before.prefab");
                File.Copy("Deliverables/DemeoRework/Baseline/Assets/TerrainBase/Prefabs/SR_TerrainBase.prefab",Temp+"/Land.prefab");
                AssetDatabase.ImportAsset(Temp+"/Before.prefab",ImportAssetOptions.ForceSynchronousImport);AssetDatabase.ImportAsset(Temp+"/Land.prefab",ImportAssetOptions.ForceSynchronousImport);
                old=PrefabUtility.LoadPrefabContents(Temp+"/Before.prefab");current=PrefabUtility.LoadPrefabContents("Assets/SwitchyardRaiders/Startup/Prefabs/SR_TabletopPresentation.prefab");
                var before=old.GetComponent<RaidersTabletopBindings>();var after=current.GetComponent<RaidersTabletopBindings>();
                old.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);old.transform.localScale=Vector3.one;old.SetActive(true);
                before.virtualTable.SetActive(false);before.terrainAssembly.SetActive(false);before.gameplay.gameObject.SetActive(true);before.manipulationHandles.SetActive(false);foreach(var seat in before.seats)if(seat)seat.gameObject.SetActive(false);
                var oldLand=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Temp+"/Land.prefab"),old.scene);oldLand.transform.SetParent(before.terrainMount,false);oldLand.SetActive(true);
                var oldEngine=before.gameplay.Find("Company authored locomotive");oldEngine.gameObject.SetActive(true);oldEngine.localPosition=RaidersMatch.Point(16);oldEngine.localRotation=Quaternion.Euler(0,-90,0);oldEngine.GetComponent<CompanyLocomotiveMotion>().SetTravelDistance(.7);
                for(int i=0;i<before.playerLocomotives.Length;i++){var train=before.playerLocomotives[i];train.gameObject.SetActive(true);float d=new[]{0f,3f,6f,23f}[i];train.localPosition=RaidersMatch.Point(d);train.localRotation=Quaternion.LookRotation(RaidersMatch.Point(d+.01f)-RaidersMatch.Point(d));train.GetComponent<PlayerLocomotiveMotion>().SetTravelDistance(.7);}
                current.SetActive(true);current.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);current.transform.localScale=Vector3.one;
                after.virtualTable.SetActive(true);after.virtualTable.transform.Find("Supports").gameObject.SetActive(false);after.terrainAssembly.SetActive(true);after.gameplay.gameObject.SetActive(true);after.manipulationHandles.SetActive(false);
                for(int i=0;i<4;i++){var train=after.playerLocomotives[i];train.gameObject.SetActive(true);float d=new[]{0f,3f,6f,23f}[i];TabletopRailway.Place(train,d,true,.0415f);var motion=train.GetComponent<PlayerLocomotiveMotion>();motion.SetTravelDistance(.7);PlayerVehicleTrackFollower.FitBogies(train,motion,d+TabletopRailway.EngineOffset);}
                var engine=after.gameplay.Find("Company authored locomotive");engine.gameObject.SetActive(true);TabletopRailway.Place(engine,16,true,.0185f);var steam=engine.GetComponent<CompanyLocomotiveMotion>();CompanyTableFollower.FitPilotAxles(engine,steam,16);steam.SetTravelDistance(.7);
                for(int i=0;i<6;i++){var w=after.gameplay.Find("Cargo wagon "+(i+1));w.gameObject.SetActive(true);TabletopRailway.Place(w,16-(i+1)*RaidersMatch.Spacing,false,.016f);}
                DetailLOD(old);DetailLOD(current);
                Camera CameraFor(GameObject root)
                {
                    var go=new GameObject("Fixed evidence camera");SceneManager.MoveGameObjectToScene(go,root.scene);var cam=go.AddComponent<Camera>();cam.scene=root.scene;cam.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.065f,.085f,.09f);cam.nearClipPlane=.01f;cam.farClipPlane=80;cam.fieldOfView=65;cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;cam.GetUniversalAdditionalCameraData().renderShadows=false;
                    foreach(var value in new[]{(new Vector3(45,-35,0),2.3f),(new Vector3(35,145,0),1.3f)}){var lightGo=new GameObject("Evidence lighting");SceneManager.MoveGameObjectToScene(lightGo,root.scene);lightGo.transform.rotation=Quaternion.Euler(value.Item1);var l=lightGo.AddComponent<Light>();l.type=LightType.Directional;l.intensity=value.Item2;l.shadows=LightShadows.None;}
                    return cam;
                }
                var c1=CameraFor(old);var c2=CameraFor(current);float normal=TabletopViewContract.MRBaselineScale(1);
                current.transform.localScale=Vector3.one*normal;
                var report=new StringBuilder("Synthetic Edit Mode MR comparison, not passthrough/native scene or observed headset evidence. Actual retained pre-Task1 and current production meshes. LOD0 forced for consistent detail; no performance claim.\n");
                report.AppendLine("Before terrain mesh bounds="+BoundsIn(oldLand.transform,old.transform).size.ToString("F6")+"; company authored="+BoundsIn(oldEngine,oldEngine).size.ToString("F6")+"; effective scale="+oldEngine.lossyScale.ToString("F7"));
                report.AppendLine("After tray displayed bounds="+BoundsIn(after.virtualTable.transform.Find("TabletopPivot"),null).size.ToString("F6")+"; company authored="+BoundsIn(engine,engine).size.ToString("F6")+"; effective scale="+engine.lossyScale.ToString("F7"));
                foreach(var c in new[]{c1,c2}){c.orthographic=true;c.orthographicSize=1.85f;c.transform.SetPositionAndRotation(new Vector3(0,6,0),Quaternion.Euler(90,0,0));}
                Capture(c1,"before-top");Capture(c2,"after-top");
                var headGo=new GameObject("Synthetic seated eye reference");SceneManager.MoveGameObjectToScene(headGo,current.scene);headGo.transform.position=new Vector3(0,1.3f,0);
                var home=TabletopViewMath.ReachablePose(headGo.transform,normal,0);current.transform.position=home.position;
                old.transform.position=new Vector3(0,.756f,1); // Explicit illustrative old 75cm tabletop +6mm, centre 1m. Not observed MR.
                var focus=new Vector3(engine.position.x,home.position.y+TabletopViewContract.TerrainDatum*normal,engine.position.z);
                foreach(var c in new[]{c1,c2}){c.orthographic=false;c.transform.position=headGo.transform.position;c.transform.LookAt(focus+Vector3.up*.05f);}
                report.AppendLine("Seated fixed camera="+c2.transform.position.ToString("F4")+" euler="+c2.transform.eulerAngles.ToString("F4")+" verticalFOV=65. Before illustrative 75cm support, centre=1m; after reachable free MR home="+home.position.ToString("F6"));
                Capture(c1,"before-seated");
                foreach(float zoom in new[]{1f,3f,6f})
                {
                    current.transform.localScale=Vector3.one*normal*zoom;current.transform.position=focus+(home.position-focus)*zoom;
                    Capture(c2,"after-seated-"+zoom);
                    var bounds=BoundsIn(engine,engine);var min=Vector2.one*float.MaxValue;var max=Vector2.one*float.MinValue;
                    for(int i=0;i<8;i++){var p=engine.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));var v=(Vector2)c2.WorldToViewportPoint(p);min=Vector2.Min(min,v);max=Vector2.Max(max,v);}
                    report.AppendLine("relativeZoom="+zoom+" presentationScale="+(normal*zoom).ToString("F7")+" outer="+(normal*zoom*1.3f).ToString("F6")+" land="+(normal*zoom*1.05f).ToString("F6")+" company length="+(bounds.size.z*.01f*normal*zoom).ToString("F6")+"m screen envelope="+((max-min)*100).ToString("F2")+" percent; terrain pivot="+focus.ToString("F5"));
                }
                report.AppendLine("Whole board scales together about a terrain pivot under the selected engine. At maximum the train may exceed the frame: this is immersive cab/mechanical inspection. No inverse train scaling or FOV change.");
                File.WriteAllText(Evidence+"/measurements.txt",report.ToString());Debug.Log(report.ToString());
            }
            finally{if(old)PrefabUtility.UnloadPrefabContents(old);if(current)PrefabUtility.UnloadPrefabContents(current);AssetDatabase.DeleteAsset(Temp);}
        }
    }
}
