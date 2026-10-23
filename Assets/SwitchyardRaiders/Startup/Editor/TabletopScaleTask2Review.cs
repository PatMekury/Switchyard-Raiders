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
    public static partial class TabletopScaleTask2
    {
        const string Evidence="Deliverables/DemeoRework/Task2Evidence";
        static Bounds ModelBounds(Transform root)
        {
            bool first=true;var result=new Bounds();
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var filter=r.GetComponent<MeshFilter>();var mesh=r is SkinnedMeshRenderer sk?sk.sharedMesh:filter?filter.sharedMesh:null;if(!mesh)continue;var b=mesh.bounds;
                for(int i=0;i<8;i++)
                {var p=root.InverseTransformPoint(r.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}
            }
            return result;
        }
        static void Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1600,1100,24);rt.Create();var prior=RenderTexture.active;var image=new Texture2D(1600,1100,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,1100),0,0);image.Apply();File.WriteAllBytes(Evidence+"/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=prior;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);}
        }
        [MenuItem("Switchyard Raiders/Demeo Rework/5 - Render calibrated zoom stages")]
        public static void Render()
        {
            Guard();Directory.CreateDirectory(Evidence);var root=PrefabUtility.LoadPrefabContents(Presentation);
            try
            {
                root.SetActive(true);root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.transform.localScale=Vector3.one;
                var b=root.GetComponent<RaidersTabletopBindings>();b.virtualTable.SetActive(true);b.virtualTable.transform.Find("Supports").gameObject.SetActive(false);b.terrainAssembly.SetActive(true);b.gameplay.gameObject.SetActive(true);b.manipulationHandles.SetActive(false);
                foreach(var lod in root.GetComponentsInChildren<LODGroup>(true)){var levels=lod.GetLODs();lod.enabled=false;foreach(var level in levels)foreach(var r in level.renderers)if(r)r.enabled=false;foreach(var r in levels[0].renderers)if(r)r.enabled=true;}
                // Multiple Camera.Render calls occur in one editor update. Unity otherwise reuses
                // the first skinning matrices, falsely showing fixed-size trains in later captures.
                foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.forceMatrixRecalculationPerRender=true;
                for(int i=0;i<4;i++){var train=b.playerLocomotives[i];train.gameObject.SetActive(true);float d=new[]{0f,3f,6f,23f}[i];TabletopRailway.Place(train,d,true,.0415f);var motion=train.GetComponent<PlayerLocomotiveMotion>();motion.SetTravelDistance(.7);PlayerVehicleTrackFollower.FitBogies(train,motion,d+TabletopRailway.EngineOffset);}
                var company=b.gameplay.Find("Company authored locomotive");company.gameObject.SetActive(true);TabletopRailway.Place(company,16,true,.0185f);var steamMotion=company.GetComponent<CompanyLocomotiveMotion>();CompanyTableFollower.FitPilotAxles(company,steamMotion,16);steamMotion.SetTravelDistance(.7);
                for(int i=0;i<6;i++){var w=b.gameplay.Find("Cargo wagon "+(i+1));w.gameObject.SetActive(true);TabletopRailway.Place(w,16-(i+1)*RaidersMatch.Spacing,false,.016f);}
                // Isolated adapter fixture supplies coupling links; it neither starts nor ticks a match.
                var adapterGo=new GameObject("Inactive evidence adapter");adapterGo.SetActive(false);SceneManager.MoveGameObjectToScene(adapterGo,root.scene);var adapter=adapterGo.AddComponent<RaidersTable>();adapter.company=company;adapter.locomotives=b.playerLocomotives;adapter.wagons=Enumerable.Range(1,6).Select(i=>b.gameplay.Find("Cargo wagon "+i)).ToArray();
                var match=new RaidersMatch();for(int i=0;i<6;i++){match.cargo[i].owner=-2;match.cargo[i].order=i+1;}adapter.UpdateCouplings(match);
                var camGo=new GameObject("Fixed scale comparison camera");SceneManager.MoveGameObjectToScene(camGo,root.scene);var camera=camGo.AddComponent<Camera>();camera.scene=root.scene;camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.10f,.095f);camera.nearClipPlane=.01f;camera.farClipPlane=40;camera.fieldOfView=65;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;camera.GetUniversalAdditionalCameraData().renderShadows=false;
                void Light(string name,Vector3 rotation,float intensity){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,root.scene);go.transform.rotation=Quaternion.Euler(rotation);var l=go.AddComponent<Light>();l.type=LightType.Directional;l.intensity=intensity;l.shadows=LightShadows.None;}
                Light("Key",new Vector3(45,-35,0),2.3f);Light("Fill",new Vector3(35,145,0),1.3f);
                var focus=company.position+Vector3.up*.020f;camera.transform.position=new Vector3(0,.665f,-.95f);camera.transform.LookAt(focus);
                var report=new StringBuilder("Actual production meshes; fixed camera, lens, lighting and selected focus. Edit Mode only. LOD0 forced equally for comparison.\n");
                report.AppendLine("camera "+camera.transform.position.ToString("F4")+" rotation "+camera.transform.eulerAngles.ToString("F4")+" FOV="+camera.fieldOfView+" focus="+focus.ToString("F4"));
                var bounds=ModelBounds(company);
                foreach(float scale in new[]{1f,TabletopViewContract.ReviewIntermediateZoom,TabletopViewContract.MaximumZoom})
                {
                    root.transform.localScale=Vector3.one*scale;root.transform.position=focus*(1-scale);
                    Capture(camera,"zoom-"+scale.ToString("0"));
                    var min=Vector2.one*float.MaxValue;var max=Vector2.one*float.MinValue;
                    for(int i=0;i<8;i++){var p=company.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));var screen=(Vector2)camera.WorldToViewportPoint(p);min=Vector2.Min(min,screen);max=Vector2.Max(max,screen);}
                    report.AppendLine(scale+"x outer="+(TabletopViewContract.OuterSize*scale).ToString("F3")+"m land="+(TabletopViewContract.UsableLandSize*scale).ToString("F3")+"m company size="+(bounds.size*TabletopViewContract.MiniatureScale*scale).ToString("F4")+"m projected envelope width="+((max.x-min.x)*100).ToString("F1")+"% height="+((max.y-min.y)*100).ToString("F1")+"%");
                }
                root.transform.localScale=Vector3.one;root.transform.position=Vector3.zero;camera.fieldOfView=45;camera.transform.position=new Vector3(0,1.85f,-.2f);camera.transform.LookAt(new Vector3(0,.16f,0));Capture(camera,"railway-fit");
                camera.transform.position=new Vector3(-.12f,.29f,-.65f);camera.transform.LookAt(company.position+Vector3.up*.015f);Capture(camera,"company-coupling-detail");
                var player=b.playerLocomotives[0];camera.transform.position=player.position+new Vector3(.12f,.08f,.15f);camera.transform.LookAt(player.position+Vector3.up*.02f);Capture(camera,"player-gauge-detail");
                File.WriteAllText(Evidence+"/measurements.txt",report.ToString());Debug.Log(report.ToString());
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
