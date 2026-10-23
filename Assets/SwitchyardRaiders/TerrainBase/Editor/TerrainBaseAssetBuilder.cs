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

namespace SwitchyardRaiders.TerrainBase.Editor
{
    // Editor-only asset construction and isolated review; no runtime behaviour.
    public static class TerrainBaseAssetBuilder
    {
        const string Root="Assets/SwitchyardRaiders/TerrainBase";
        const string Output="Deliverables/TerrainBase";
        const string Prefab=Root+"/Prefabs/SR_TerrainBase.prefab";
        static readonly string[] Parts={"TerrainTop","ShellPanels","StructuralBraces","CornerPillars","MetalFittings","Underside"};
        static readonly string[] Mats={"EarthGreen","CharcoalMetal","Stone","Brass"};
        static void Guard()
        {
            if(!Application.dataPath.Replace('\\','/').EndsWith("/Switchyard Raiders/Assets")||EditorApplication.isPlayingOrWillChangePlaymode)
                throw new Exception("Requires Switchyard Raiders in edit mode.");
        }
        static GameObject NewIn(Scene scene,string name){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;}
        static GameObject Child(Transform parent,string name){var go=NewIn(parent.gameObject.scene,name);go.transform.SetParent(parent,false);return go;}
        static string MatFor(string part)=>part=="TerrainTop"?"EarthGreen":part=="CornerPillars"?"Stone":part=="MetalFittings"?"Brass":"CharcoalMetal";

        [MenuItem("Switchyard Raiders/Terrain Base/Build isolated prefab")]
        public static void Build()
        {
            SwitchyardRaiders.Editor.RecessedTableTask1.BuildAssets();
        }

        private static void BuildLegacy()
        {
            Guard();foreach(string dir in new[]{"Materials","Meshes","Prefabs"})Directory.CreateDirectory(Root+"/"+dir);
            AssetDatabase.Refresh();var materials=MakeMaterials();
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=NewIn(scene,"TerrainRoot");var anchors=Parts.ToDictionary(n=>n,n=>Child(root.transform,n));
                var shared=new List<Renderer>();var lods=new List<LOD>();
                for(int level=0;level<2;level++)
                {
                    string file=Root+"/Art/SR_TerrainBase_LOD"+level+".fbx";
                    var imp=(ModelImporter)AssetImporter.GetAtPath(file);if(imp==null)throw new Exception("Missing "+file);
                    imp.importAnimation=false;imp.importCameras=false;imp.importLights=false;imp.addCollider=false;
                    imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importNormals=ModelImporterNormals.Import;
                    imp.importTangents=ModelImporterTangents.CalculateMikk;imp.globalScale=1;imp.useFileScale=true;
                    imp.bakeAxisConversion=true;imp.preserveHierarchy=true;imp.meshCompression=ModelImporterMeshCompression.Off;
                    imp.isReadable=true;imp.SaveAndReimport();
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(file);var renderers=new List<Renderer>();
                    foreach(string part in Parts)
                    {
                        bool common=part=="TerrainTop"||part=="Underside";if(common&&level==1)continue;
                        var mf=model.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name.StartsWith(part+"_"));
                        if(mf.sharedMesh.subMeshCount!=1)throw new Exception("Expected one material per part "+part);
                        var mesh=new Mesh{name=part+(common?"":"_LOD"+level)};
                        mesh.indexFormat=mf.sharedMesh.vertexCount<65536?IndexFormat.UInt16:IndexFormat.UInt32;
                        mesh.CombineMeshes(new[]{new CombineInstance{mesh=mf.sharedMesh,transform=mf.transform.localToWorldMatrix}},true,true,false);
                        mesh.RecalculateBounds();mesh.RecalculateTangents();
                        string meshPath=Root+"/Meshes/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,meshPath);
                        var visual=common?anchors[part]:Child(anchors[part].transform,"LOD"+level);
                        visual.AddComponent<MeshFilter>().sharedMesh=mesh;var r=visual.AddComponent<MeshRenderer>();
                        r.sharedMaterial=materials[MatFor(part)];r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
                        r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.Simple;
                        if(common)shared.Add(r);else renderers.Add(r);
                    }
                    renderers.AddRange(shared);lods.Add(new LOD(level==0?.35f:.01f,renderers.ToArray()));
                    imp.isReadable=false;imp.SaveAndReimport();
                }
                var group=root.AddComponent<LODGroup>();group.SetLODs(lods.ToArray());group.fadeMode=LODFadeMode.None;group.RecalculateBounds();
                var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,.075f,0);collider.size=new Vector3(1.5f,.15f,1.05f);
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
            Review();
        }
        static Dictionary<string,Material> MakeMaterials()
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)throw new Exception("URP Lit unavailable");
            var result=new Dictionary<string,Material>();
            foreach(string name in Mats)
            {
                string path=Root+"/Materials/SR_Terrain_"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;
                if(name=="EarthGreen")
                {
                    mat.SetColor("_BaseColor",new Color(.28f,.34f,.245f,1));mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.08f);
                }
                else
                {
                    foreach(string kind in new[]{"BaseColor","Normal","Mask"})
                    {
                        string texturePath=Root+"/Art/Textures/"+name+"_"+kind+".png";
                        var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);int size=name=="Brass"?256:512;
                        ti.textureType=kind=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=kind=="BaseColor";
                        ti.alphaSource=kind=="Mask"?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
                        ti.mipmapEnabled=true;ti.streamingMipmaps=true;ti.isReadable=false;ti.wrapMode=TextureWrapMode.Repeat;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=2;ti.maxTextureSize=size;
                        ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=size,format=TextureImporterFormat.ASTC_6x6,compressionQuality=100});ti.SaveAndReimport();
                    }
                    Texture2D Tex(string kind)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/"+name+"_"+kind+".png");
                    mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",Tex("BaseColor"));mat.SetTexture("_BumpMap",Tex("Normal"));mat.SetFloat("_BumpScale",1);
                    mat.SetTexture("_MetallicGlossMap",Tex("Mask"));mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",1);mat.SetFloat("_SmoothnessTextureChannel",0);
                    mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                mat.SetFloat("_Surface",0);mat.SetFloat("_Cull",2);mat.enableInstancing=true;EditorUtility.SetDirty(mat);result[name]=mat;
            }
            return result;
        }
        [MenuItem("Switchyard Raiders/Terrain Base/Validate and render isolated previews")]
        public static void Review()
        {
            SwitchyardRaiders.Editor.RecessedTableTask1.Review();
        }

        private static void ReviewLegacy()
        {
            Guard();var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                Validate(root);
                var camera=NewIn(root.scene,"ReviewCamera").AddComponent<Camera>();camera.scene=root.scene;camera.enabled=false;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.28f,.30f,.29f,1);camera.nearClipPlane=.01f;camera.farClipPlane=20;
                camera.orthographic=true;camera.allowHDR=false;camera.allowMSAA=false;var data=camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing=false;data.renderShadows=false;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
                void Light(string name,Vector3 rotation,float intensity,Color color){var o=NewIn(root.scene,name);o.transform.rotation=Quaternion.Euler(rotation);var l=o.AddComponent<Light>();l.type=LightType.Directional;l.intensity=intensity;l.color=color;l.shadows=LightShadows.None;}
                Light("Key",new Vector3(45,-35,0),2,new Color(1,.94f,.86f));Light("Fill",new Vector3(35,130,0),.8f,new Color(.84f,.91f,1));
                var lod=root.GetComponent<LODGroup>();lod.ForceLOD(0);
                void Shot(string name,Vector3 position,float size)
                {
                    camera.transform.position=position;camera.transform.LookAt(new Vector3(0,.075f,0));camera.orthographicSize=size;
                    var rt=new RenderTexture(1600,1100,24,RenderTextureFormat.ARGB32);rt.Create();var prev=RenderTexture.active;var tex=new Texture2D(1600,1100,TextureFormat.RGB24,false);
                    try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1100),0,0);tex.Apply();Directory.CreateDirectory(Output+"/Previews");File.WriteAllBytes(Output+"/Previews/Unity_"+name+".png",tex.EncodeToPNG());}
                    finally{camera.targetTexture=null;RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
                }
                Shot("ThreeQuarter",new Vector3(2,1.7f,-2.6f),.74f);Shot("Top",new Vector3(0,3,-.0001f),.61f);Shot("Side",new Vector3(0,.075f,-3),.61f);
                Shot("Underside",new Vector3(1.8f,-1.6f,-2),.74f);lod.ForceLOD(1);Shot("LOD1",new Vector3(2,1.7f,-2.6f),.74f);
                Debug.Log("Terrain base isolated previews complete; existing scenes untouched.");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void Validate(GameObject root)
        {
            var lines=new List<string>();
            foreach(string part in Parts)if(!root.transform.Find(part))throw new Exception("Missing "+part);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.localPosition.sqrMagnitude>1e-9f||Vector3.Distance(t.localScale,Vector3.one)>1e-5f||Quaternion.Angle(t.localRotation,Quaternion.identity)>.001f)throw new Exception("Unexpected transform "+t.name);
            var lods=root.GetComponent<LODGroup>().GetLODs();
            for(int level=0;level<lods.Length;level++)
            {
                var rs=lods[level].renderers;var bounds=rs[0].bounds;int tris=0,verts=0;
                var triangles=new List<(Vector3 a,Vector3 b,Vector3 c)>();
                foreach(var r in rs)
                {
                    bounds.Encapsulate(r.bounds);var mesh=r.GetComponent<MeshFilter>().sharedMesh;var indices=mesh.triangles;var v=mesh.vertices;
                    tris+=indices.Length/3;verts+=v.Length;
                    if(r.sharedMaterials.Length!=1||!r.sharedMaterial||r.sharedMaterial.shader.name!="Universal Render Pipeline/Lit")throw new Exception("Bad material "+r.name);
                    if(mesh.normals.Length!=v.Length||mesh.normals.Any(n=>float.IsNaN(n.x)||n.sqrMagnitude<.9f))throw new Exception("Bad normals");
                    for(int i=0;i<indices.Length;i+=3){var a=v[indices[i]];var b=v[indices[i+1]];var c=v[indices[i+2]];if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-17f)throw new Exception("Degenerate triangle "+r.name);triangles.Add((a,b,c));}
                }
                if(Vector3.Distance(bounds.size,new Vector3(1.5f,.15f,1.05f))>.0001f||Mathf.Abs(bounds.min.y)>.00001f)throw new Exception("Unexpected bounds "+bounds);
                float Hit(float x,float z,bool below)
                {
                    var origin=new Vector3(x,below?-1:1,z);var d=below?Vector3.up:Vector3.down;float near=float.PositiveInfinity;
                    foreach(var t in triangles){var e1=t.b-t.a;var e2=t.c-t.a;var h=Vector3.Cross(d,e2);float det=Vector3.Dot(e1,h);if(Mathf.Abs(det)<1e-10f)continue;float inv=1/det;var s=origin-t.a;float u=inv*Vector3.Dot(s,h);if(u<0||u>1)continue;var q=Vector3.Cross(s,e1);float vv=inv*Vector3.Dot(d,q);if(vv<0||u+vv>1)continue;float dist=inv*Vector3.Dot(e2,q);if(dist>0)near=Mathf.Min(near,dist);}
                    return below?-1+near:1-near;
                }
                int samples=0;
                for(float x=-.742f;x<.75f;x+=.031f)for(float z=-.517f;z<.525f;z+=.031f)
                {if(Mathf.Abs(Hit(x,z,false)-.15f)>.0015f||Mathf.Abs(Hit(x,z,true))>.0001f)throw new Exception("Top/underside coverage failed "+x+","+z);samples++;}
                lines.Add($"LOD{level}: {tris} triangles; {verts} imported vertices; {rs.Length} renderers; bounds {bounds}; {samples} top and underside coverage samples passed; valid normals; no degenerate triangles.");
            }
            var top=root.transform.Find("TerrainTop").GetComponent<MeshFilter>().sharedMesh;
            if(top.triangles.Length/3!=1152||top.vertices.Any(v=>Mathf.Abs(v.y-.15f)>1e-6f)||top.normals.Any(n=>Vector3.Dot(n,Vector3.up)<.999f))throw new Exception("Top flatness, normals or topology failed");
            var cols=root.GetComponentsInChildren<Collider>();if(cols.Length!=1||!(cols[0] is BoxCollider))throw new Exception("Collider setup failed");
            if(root.GetComponentsInChildren<MonoBehaviour>(true).Length>0)throw new Exception("Unexpected runtime script");
            var table=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SwitchyardRaiders/PlayTable/Prefabs/SR_PlayTable.prefab");
            if(table){var mount=table.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="TownMount");var corners=table.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Corner_")).ToArray();float w=corners.Max(t=>t.position.x)-corners.Min(t=>t.position.x);float d=corners.Max(t=>t.position.z)-corners.Min(t=>t.position.z);lines.Add($"Existing table mount height={mount.position.y:F3} m; placement clearance per side={(w-1.5f)/2:F3} m X and {(d-1.05f)/2:F3} m Z. Read-only fit verification.");}
            lines.Add("Origin underside centre (0,0,0); Unity +Y up; length +X; width +Z. All transforms identity. Top y=0.150 m; nothing raised above it.");
            lines.Add("Blender top: 576 regular 50 mm quads; FBX triangulates to 1152 triangles. Four corner boundary cutouts are filled by flush stone caps; no interior holes.");
            lines.Add("4 reusable URP Lit materials; top constant muted earth green, no textures; 9 shell PNG maps: charcoal and stone 512px, brass 256px; Android ASTC 6x6, mipmaps. 6 renderers per LOD; top and underside shared between LODs.");
            lines.Add("One root BoxCollider 1.50 x 0.15 x 1.05 m, centre (0,0.075,0). No detail colliders or Rigidbody. No runtime scripts.");
            lines.Add("Play Mode=false. No player build or headset validation. No current scene replacement. No PreviewRenderUtility.");
            File.WriteAllLines(Output+"/unity_validation.txt",lines);Debug.Log(string.Join("\n",lines));
        }
    }
}
