using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SwitchyardRaiders.Startup;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static partial class RecessedTableTask1
    {
        struct Triangle {public Vector3 a,b,c;public bool land;}
        static List<Triangle> Triangles(GameObject root,bool land)
        {
            var triangles=new List<Triangle>();
            foreach(var r in root.GetComponent<LODGroup>().GetLODs()[0].renderers)
            {
                var m=r.GetComponent<MeshFilter>().sharedMesh;var v=m.vertices;var ix=m.triangles;
                for(int i=0;i<ix.Length;i+=3)triangles.Add(new Triangle{a=r.transform.TransformPoint(v[ix[i]]),b=r.transform.TransformPoint(v[ix[i+1]]),c=r.transform.TransformPoint(v[ix[i+2]]),land=land});
            }
            return triangles;
        }
        static bool Hit(Ray ray,Triangle t,out float distance)
        {
            distance=0;var e1=t.b-t.a;var e2=t.c-t.a;var p=Vector3.Cross(ray.direction,e2);float det=Vector3.Dot(e1,p);
            if(Mathf.Abs(det)<1e-10f)return false;float inv=1/det;var s=ray.origin-t.a;float u=Vector3.Dot(s,p)*inv;
            if(u<0||u>1)return false;var q=Vector3.Cross(s,e1);float v=Vector3.Dot(ray.direction,q)*inv;if(v<0||u+v>1)return false;
            distance=Vector3.Dot(e2,q)*inv;return distance>1e-5f;
        }
        static Bounds MeshBounds(GameObject go,int level)
        {
            bool first=true;var bounds=new Bounds();
            foreach(var r in go.GetComponent<LODGroup>().GetLODs()[level].renderers)
            {
                var m=r.GetComponent<MeshFilter>().sharedMesh;
                foreach(var v in m.vertices){var p=r.transform.TransformPoint(v);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
            }
            return bounds;
        }
        [MenuItem("Switchyard Raiders/Demeo Rework/3 - Validate and render actual assets")]
        public static void Review()
        {
            Guard();Directory.CreateDirectory(Output+"/Evidence");var report=new StringBuilder();int checks=0;
            void Check(bool condition,string text){if(!condition){report.AppendLine("FAIL "+text);File.WriteAllText(Output+"/validation-task1.txt",report.ToString());throw new Exception("Task1 validation: "+text);}checks++;report.AppendLine("PASS "+text);}
            var table=PrefabUtility.LoadPrefabContents(Table);
            try
            {
                var land=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Land),table.scene);
                land.transform.position=table.transform.Find("TabletopPivot/TownMount").position;
                table.GetComponent<LODGroup>().ForceLOD(0);land.GetComponent<LODGroup>().ForceLOD(0);
                for(int level=0;level<2;level++)
                {
                    var tb=MeshBounds(table,level);var lb=MeshBounds(land,level);
                    report.AppendLine("LOD"+level+" table bounds "+tb.ToString("F6")+" land bounds "+lb.ToString("F6"));
                    Check(Mathf.Abs(tb.size.x-TabletopViewContract.OuterSize)<.0001f && Mathf.Abs(tb.size.z-TabletopViewContract.OuterSize)<.0001f,"LOD"+level+" square exterior matches contract");
                    Check(Mathf.Abs(lb.size.x-TabletopViewContract.SlabSize)<.0001f && Mathf.Abs(lb.size.z-TabletopViewContract.SlabSize)<.0001f && Mathf.Abs(lb.size.y-.15f)<.0001f,"LOD"+level+" reauthored closed land bounds");
                    Check(Mathf.Abs(lb.max.y-1.035f)<.0001f && lb.max.y<1.037f,"LOD"+level+" terrain datum below returned lip with 2mm clearance");
                    foreach(var asset in new[]{table,land})
                    {
                        int tris=0,verts=0,badNormals=0,degenerate=0;
                        foreach(var r in asset.GetComponent<LODGroup>().GetLODs()[level].renderers)
                        {
                            var m=r.GetComponent<MeshFilter>().sharedMesh;var v=m.vertices;var ns=m.normals;var uv=m.uv;var ix=m.triangles;
                            Check(ns.Length==v.Length && uv.Length==v.Length && m.tangents.Length==v.Length,"Normals, UVs and tangents present: "+m.name);
                            Check(r.sharedMaterials.Length==m.subMeshCount && r.sharedMaterials.All(x=>x&&x.shader&&x.shader.isSupported),"Valid materials: "+m.name);
                            for(int i=0;i<ix.Length;i+=3){var n=Vector3.Cross(v[ix[i+1]]-v[ix[i]],v[ix[i+2]]-v[ix[i]]);if(n.sqrMagnitude<1e-20f)degenerate++;else if(Vector3.Dot(n.normalized,(ns[ix[i]]+ns[ix[i+1]]+ns[ix[i+2]]).normalized)<-.05f)badNormals++;}
                            Check(v.All(x=>float.IsFinite(x.x)&&float.IsFinite(x.y)&&float.IsFinite(x.z)) && uv.All(x=>float.IsFinite(x.x)&&float.IsFinite(x.y)),"Finite vertices and UVs: "+m.name);
                            tris+=ix.Length/3;verts+=v.Length;
                        }
                        report.AppendLine(asset.name+" LOD"+level+": "+tris+" triangles, "+verts+" imported vertices, "+badNormals+" reversed normals, "+degenerate+" degenerate triangles");
                        Check(badNormals==0 && degenerate==0,asset.name+" LOD"+level+" coherent topology and normals");
                    }
                }
                Check(table.GetComponentsInChildren<Transform>(true).All(t=>Vector3.Distance(t.localScale,Vector3.one)<1e-5f),"Table has unit scales; no nonuniform mesh stretching");
                Check(Mathf.Abs(land.transform.position.y-TabletopViewContract.TerrainMountHeight)<.0001f,"TownMount and land bottom coincide");
                // Visibility checks against actual imported triangles, not renderer AABB occlusion.
                var triangles=Triangles(table,false);triangles.AddRange(Triangles(land,true));
                int blocked=0;
                for(int side=0;side<4;side++)for(int row=0;row<9;row++)for(int col=0;col<13;col++)
                {
                    float along=Mathf.Lerp(-.52f,.52f,col/12f),height=.886f+row*.0185f;bool x=side<2;float sign=side%2==0?-1:1;
                    var target=new Vector3(x?sign*.529f:along,height,x?along:sign*.529f);
                    var eye=target+new Vector3(x?sign*2:0,0,x?0:sign*2);var ray=new Ray(eye,(target-eye).normalized);
                    float nearest=float.MaxValue;bool hitsLand=true;
                    foreach(var tri in triangles)if(Hit(ray,tri,out float d)&&d<nearest){nearest=d;hitsLand=tri.land;}
                    if(!hitsLand)blocked++;
                }
                Check(blocked==468,"468 horizontal side samples hit the structural tray before the slab");
                // Top rays must reach land freely across the useful surface.
                int clear=0;
                for(int x=-5;x<=5;x++)for(int z=-5;z<=5;z++)
                {
                    var ray=new Ray(new Vector3(x*.10f,2,z*.10f),Vector3.down);float nearest=float.MaxValue;bool hitsLand=false;
                    foreach(var tri in triangles)if(Hit(ray,tri,out float d)&&d<nearest){nearest=d;hitsLand=tri.land;}
                    if(hitsLand)clear++;
                }
                Check(clear==121,"121 top samples remain open for playable land and future scenery");
                // The actual adopted scene/prefab mount and train compensation contract.
                foreach(string path in new[]{Launch,Presentation})
                {
                    var p=PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        var b=p.GetComponentInChildren<RaidersTabletopBindings>(true);
                        Check(b && b.terrainMount && b.virtualTable && b.gameplay,path+" internal references retained");
                        Check(Mathf.Abs(b.virtualTable.transform.localPosition.y+TabletopViewContract.TerrainMountHeight)<1e-5f,path+" table mount correction");
                        Check(Mathf.Abs(b.gameplay.localScale.x-TabletopViewContract.GameplayScale)<1e-5f,path+" temporary railway fits land");
                        foreach(var tr in b.playerLocomotives)Check(Mathf.Abs(tr.localScale.x*b.gameplay.localScale.x-TabletopViewContract.PreservedTrainScale)<1e-6f,path+" train display scale preserved");
                    }finally{PrefabUtility.UnloadPrefabContents(p);}
                }
                var camObj=new GameObject("Task1 isolated review camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camObj,table.scene);
                var camera=camObj.AddComponent<Camera>();camera.scene=table.scene;camera.enabled=false;camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.23f,.25f,.27f);camera.allowHDR=false;camera.allowMSAA=false;
                var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
                void Light(string name,Vector3 euler,float intensity,Color color){var go=new GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,table.scene);go.transform.rotation=Quaternion.Euler(euler);var l=go.AddComponent<Light>();l.type=LightType.Directional;l.intensity=intensity;l.color=color;}
                Light("Key",new Vector3(45,-35,0),2,new Color(1,.94f,.86f));Light("Fill",new Vector3(35,130,0),.8f,new Color(.84f,.91f,1));
                void Shot(string name,Vector3 position,Vector3 target,float size,bool perspective=false)
                {
                    camera.orthographic=!perspective;camera.orthographicSize=size;camera.fieldOfView=65;camera.transform.position=position;camera.transform.LookAt(target);
                    var rt=new RenderTexture(1600,1200,24,RenderTextureFormat.ARGB32);rt.Create();var old=RenderTexture.active;var tex=new Texture2D(1600,1200,TextureFormat.RGB24,false);
                    try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1200),0,0);tex.Apply();File.WriteAllBytes(Output+"/Evidence/"+name+".png",tex.EncodeToPNG());}
                    finally{camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);}
                }
                Shot("01-front",new Vector3(0,.6f,-3),new Vector3(0,.6f,0),.70f);
                Shot("02-side",new Vector3(3,.6f,0),new Vector3(0,.6f,0),.70f);
                Shot("03-top",new Vector3(0,3,-.0001f),new Vector3(0,1,0),.73f);
                Shot("04-seated",new Vector3(0,1.55f,-TabletopViewContract.PlacementDistance),new Vector3(0,1.02f,0),.8f,true);
                Shot("05-assembled",new Vector3(1.9f,2.2f,-2.5f),new Vector3(0,.65f,0),1.0f);
                land.SetActive(false);Shot("06-empty-structural-well",new Vector3(1.4f,2.1f,-1.6f),new Vector3(0,.95f,0),.74f);land.SetActive(true);
                table.transform.Find("Supports").gameObject.SetActive(false);Shot("07-MR-tray-geometry",new Vector3(1.5f,1.8f,-1.8f),new Vector3(0,.96f,0),.75f);
                table.transform.Find("Supports").gameObject.SetActive(true);table.GetComponent<LODGroup>().ForceLOD(1);land.GetComponent<LODGroup>().ForceLOD(1);
                Shot("08-LOD1",new Vector3(1.9f,2.2f,-2.5f),new Vector3(0,.65f,0),1.0f);
                report.AppendLine(checks+" checks passed. Edit Mode asset renders, not headset observations. No Play Mode, build, installation or PreviewRenderUtility.");
                File.WriteAllText(Output+"/validation-task1.txt",report.ToString());Debug.Log(report.ToString());
            }
            finally{PrefabUtility.UnloadPrefabContents(table);}
        }
    }
}
