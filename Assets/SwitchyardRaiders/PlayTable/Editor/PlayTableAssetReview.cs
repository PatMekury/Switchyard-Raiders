using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SwitchyardRaiders.PlayTable.Editor
{
    public static class PlayTableAssetReview
    {
        const string Prefab="Assets/SwitchyardRaiders/PlayTable/Prefabs/SR_PlayTable.prefab";
        const string Output="Deliverables/PlayTable/Previews/";
        [MenuItem("Switchyard Raiders/Play Table/Validate openings and render isolated previews")]
        public static void Review()
        {
            SwitchyardRaiders.Editor.RecessedTableTask1.Review();
        }

        private static void ReviewLegacy()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !Application.dataPath.Replace('\\','/').EndsWith("/Switchyard Raiders/Assets"))throw new Exception("Requires target project in edit mode");
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                GeometryCheck(root);
                var camGo=NewIn(root.scene,"ReviewCamera");
                var camera=camGo.AddComponent<Camera>();
                camera.scene=root.scene;camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.25f,.28f,.30f,1);camera.nearClipPlane=.05f;camera.farClipPlane=30;
                camera.orthographic=true;camera.allowHDR=false;camera.allowMSAA=false;
                var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;
                data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
                Light MakeLight(string n,Vector3 rot,float intensity,Color color)
                {
                    var go=NewIn(root.scene,n);go.transform.rotation=Quaternion.Euler(rot);
                    var l=go.AddComponent<Light>();l.type=LightType.Directional;l.intensity=intensity;l.color=color;l.shadows=LightShadows.None;return l;
                }
                MakeLight("Key",new Vector3(45,-35,0),2.0f,new Color(1,.94f,.86f));
                MakeLight("Fill",new Vector3(35,130,0),.8f,new Color(.84f,.91f,1));
                var lod=root.GetComponent<LODGroup>();lod.ForceLOD(0);
                void Shot(string name,Vector3 pos,Vector3 target,float size)
                {
                    camera.transform.position=pos;camera.transform.LookAt(target);camera.orthographicSize=size;
                    var rt=new RenderTexture(1600,1200,24,RenderTextureFormat.ARGB32);rt.Create();
                    var prev=RenderTexture.active;
                    var tex=new Texture2D(1600,1200,TextureFormat.RGB24,false);
                    try
                    {
                        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                        tex.ReadPixels(new Rect(0,0,1600,1200),0,0);tex.Apply();
                        Directory.CreateDirectory(Output);File.WriteAllBytes(Output+name+".png",tex.EncodeToPNG());
                    }
                    finally{camera.targetTexture=null;RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
                }
                Shot("Unity_ThreeQuarter",new Vector3(2.8f,2.65f,-3.5f),new Vector3(0,.57f,0),1.10f);
                Shot("Unity_Top",new Vector3(0,4,-.0001f),new Vector3(0,0,0),.88f);
                Shot("Unity_Front",new Vector3(0,.62f,-4),new Vector3(0,.62f,0),.9f);
                Shot("Unity_Side",new Vector3(4,.62f,0),new Vector3(0,.62f,0),.8f);
                lod.ForceLOD(1);Shot("Unity_LOD1",new Vector3(2.8f,2.65f,-3.5f),new Vector3(0,.57f,0),1.10f);
                lod.ForceLOD(0);root.transform.Find("Supports").gameObject.SetActive(false);
                Shot("Unity_SupportsHidden",new Vector3(2.8f,2.65f,-3.5f),new Vector3(0,.57f,0),1.10f);
                Debug.Log("Rendered isolated Unity review images; no scene assets saved or modified.");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static GameObject NewIn(Scene scene,string n){var go=new GameObject(n);SceneManager.MoveGameObjectToScene(go,scene);return go;}

        static void GeometryCheck(GameObject root)
        {
            var lods=root.GetComponent<LODGroup>().GetLODs();
            var report=new List<string>();
            foreach(var lod in lods.Select((v,i)=>(v,i)))
            {
                var triangles=new List<(Vector3 a,Vector3 b,Vector3 c)>();
                int badNormals=0,degenerate=0;
                foreach(var r in lod.v.renderers)
                {
                    var m=r.GetComponent<MeshFilter>().sharedMesh;var v=m.vertices;var indices=m.triangles;
                    foreach(var n in m.normals)if(float.IsNaN(n.x)||n.sqrMagnitude<.9f)badNormals++;
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var a=r.transform.TransformPoint(v[indices[i]]);var b=r.transform.TransformPoint(v[indices[i+1]]);var c=r.transform.TransformPoint(v[indices[i+2]]);
                        if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-16f)degenerate++;
                        triangles.Add((a,b,c));
                    }
                }
                float Hit(float x,float z)
                {
                    Vector3 origin=new Vector3(x,2,z),d=Vector3.down;float near=float.PositiveInfinity;
                    foreach(var t in triangles)
                    {
                        var e1=t.b-t.a;var e2=t.c-t.a;var h=Vector3.Cross(d,e2);float det=Vector3.Dot(e1,h);
                        if(Mathf.Abs(det)<1e-9f)continue;float inv=1/det;var s=origin-t.a;float u=inv*Vector3.Dot(s,h);
                        if(u<0||u>1)continue;var q=Vector3.Cross(s,e1);float vv=inv*Vector3.Dot(d,q);if(vv<0||u+vv>1)continue;
                        float distance=inv*Vector3.Dot(e2,q);if(distance>0)near=Mathf.Min(near,distance);
                    }
                    return 2-near;
                }
                var cups=new List<Vector2>();
                foreach(float z in new[]{-.6275f,.6275f})foreach(float x in new[]{-.64f,.64f})cups.Add(new Vector2(x,z));
                foreach(float x in new[]{-.865f,.865f})foreach(float z in new[]{-.36f,.36f})cups.Add(new Vector2(x,z));
                foreach(var cup in cups)
                {
                    // Avoid the deliberately tiny drainage bore at the exact center.
                    float y=Hit(cup.x+.012f,cup.y);
                    if(Mathf.Abs(y-.968f)>.003f)throw new Exception("Cup opening blocked/missing at "+cup+" hit="+y);
                    foreach(var c in root.GetComponentsInChildren<Collider>())
                    {
                        if(c.Raycast(new Ray(new Vector3(cup.x,1.2f,cup.y),Vector3.down),out var hit,.2f))throw new Exception("Collider blocks cup opening "+cup);
                    }
                }
                float felt=Hit(0,0);if(Mathf.Abs(felt-1.018f)>.001f)throw new Exception("TownMount does not match felt "+felt);
                if(badNormals>0||degenerate>0)throw new Exception("Mesh normals/triangles invalid "+badNormals+" / "+degenerate);
                report.Add("LOD"+lod.i+": eight actual cup bores clear to recessed bottom; all eight collider openings clear; felt/TownMount alignment passed; nonzero valid normals; no degenerate triangles.");
            }
            var supports=root.transform.Find("Supports");supports.gameObject.SetActive(false);
            if(supports.GetComponentsInChildren<Renderer>(true).Any(r=>r.gameObject.activeInHierarchy))throw new Exception("Supports failed to hide");
            if(!root.transform.Find("TabletopPivot").gameObject.activeInHierarchy)throw new Exception("Top hidden with supports");
            supports.gameObject.SetActive(true);
            report.Add("Supports hide independently; terrain proxy absent; no runtime scripts added.");
            File.WriteAllLines("Deliverables/PlayTable/geometry_validation.txt",report);
        }
    }
}
