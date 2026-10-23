using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace SwitchyardRaiders.PlayTable.Editor
{
    // Asset construction only. No runtime interaction or placement behaviour.
    public static class PlayTableAssetBuilder
    {
        const string Root = "Assets/SwitchyardRaiders/PlayTable";
        const string Report = "Deliverables/PlayTable";
        static readonly string[] Materials = { "Oak", "Felt", "Stainless", "DarkMetal", "Brass" };

        static void Guard()
        {
            if (!Application.dataPath.Replace('\\','/').EndsWith("/Switchyard Raiders/Assets"))
                throw new InvalidOperationException("Wrong Unity project.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("This builder requires edit mode.");
        }

        [MenuItem("Switchyard Raiders/Play Table/Build isolated asset prefab")]
        public static void Build()
        {
            SwitchyardRaiders.Editor.RecessedTableTask1.BuildAssets();
        }

        // Historical source retained for recovery; current menu uses the shared contract.
        private static void BuildLegacy()
        {
            Guard();
            foreach (var dir in new[] {"Materials", "Meshes", "Prefabs", "Validation"})
                Directory.CreateDirectory(Root + "/" + dir);
            AssetDatabase.Refresh();
            var materials = MakeMaterials();
            var cube = MakeStudioCube();
            var previewScene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("TableRoot");
            SceneManager.MoveGameObjectToScene(root, previewScene);
            try
            {
                var top = Child(root.transform, "TabletopPivot", new Vector3(0,1.025f,0));
                var mount=Child(top,"TownMount", new Vector3(0,-.007f,0));
                var footprint = Child(mount,"TownFootprint", Vector3.zero);
                Child(footprint,"Corner_FrontLeft", new Vector3(-.78f,0,-.555f));
                Child(footprint,"Corner_FrontRight", new Vector3(.78f,0,-.555f));
                Child(footprint,"Corner_BackLeft", new Vector3(-.78f,0,.555f));
                Child(footprint,"Corner_BackRight", new Vector3(.78f,0,.555f));
                var supports = Child(root.transform,"Supports",Vector3.zero);
                var lods = new List<LOD>();
                for (int level=0; level<2; level++)
                {
                    string path=Root+"/Art/SR_PlayTable_LOD"+level+".fbx";
                    var imp=(ModelImporter)AssetImporter.GetAtPath(path);
                    if(imp==null)throw new InvalidOperationException("Missing FBX "+path);
                    imp.importAnimation=false; imp.importCameras=false; imp.importLights=false;
                    imp.materialImportMode=ModelImporterMaterialImportMode.None;
                    imp.importNormals=ModelImporterNormals.Import;
                    imp.importTangents=ModelImporterTangents.CalculateMikk;
                    imp.globalScale=1; imp.useFileScale=true; imp.bakeAxisConversion=true;
                    imp.isReadable=true; imp.meshCompression=ModelImporterMeshCompression.Off;
                    imp.addCollider=false; imp.preserveHierarchy=true; imp.SaveAndReimport();
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var input = model.GetComponentsInChildren<MeshFilter>(true);
                    var groups = input.GroupBy(m=>Group(m.name)).ToArray();
                    var renderers = new List<Renderer>();
                    foreach(var group in groups)
                    {
                        var bits=group.Key.Split('/');
                        Transform anchor;
                        if(bits[0]=="Top") anchor=ChildOnce(top,bits[1],Vector3.zero);
                        else
                        {
                            var legName=bits[1];
                            float x=legName.Contains("L") ? -.83f : .83f;
                            float z=legName.Contains("F") ? -.58f : .58f;
                            var leg=ChildOnce(supports,"Leg_"+legName,new Vector3(x,0,z));
                            anchor=ChildOnce(leg,bits[2],Vector3.zero);
                        }
                        var visual=Child(anchor,"LOD"+level,Vector3.zero).gameObject;
                        var byMat=group.GroupBy(m=>MaterialName(m.name)).ToArray();
                        var intermediate=new List<Mesh>();
                        var slots=new List<Material>();
                        foreach(var mg in byMat)
                        {
                            var combined=new Mesh();
                            combined.indexFormat=IndexFormat.UInt32;
                            combined.CombineMeshes(mg.Select(m=>new CombineInstance {
                                mesh=m.sharedMesh, subMeshIndex=0,
                                transform=anchor.worldToLocalMatrix * m.transform.localToWorldMatrix
                            }).ToArray(),true,true,false);
                            intermediate.Add(combined); slots.Add(materials[mg.Key]);
                        }
                        var mesh=new Mesh {name=group.Key.Replace('/','_')+"_LOD"+level};
                        mesh.indexFormat=intermediate.Sum(m=>m.vertexCount)<65536?IndexFormat.UInt16:IndexFormat.UInt32;
                        mesh.CombineMeshes(intermediate.Select(m=>new CombineInstance {mesh=m,transform=Matrix4x4.identity}).ToArray(),false,false,false);
                        mesh.RecalculateBounds(); mesh.RecalculateTangents();
                        string meshPath=Root+"/Meshes/"+mesh.name+".asset";
                        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if(existing){EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
                        else AssetDatabase.CreateAsset(mesh,meshPath);
                        foreach(var m in intermediate)UnityEngine.Object.DestroyImmediate(m);
                        visual.AddComponent<MeshFilter>().sharedMesh=mesh;
                        var mr=visual.AddComponent<MeshRenderer>(); mr.sharedMaterials=slots.ToArray();
                        mr.shadowCastingMode=ShadowCastingMode.Off;
                        mr.receiveShadows=true; mr.lightProbeUsage=LightProbeUsage.BlendProbes;
                        mr.reflectionProbeUsage=ReflectionProbeUsage.Simple;
                        renderers.Add(mr);
                    }
                    lods.Add(new LOD(level==0?.30f:.015f,renderers.ToArray()));
                    imp.isReadable=false;imp.SaveAndReimport();
                }
                var lodGroup=root.AddComponent<LODGroup>(); lodGroup.SetLODs(lods.ToArray());
                lodGroup.fadeMode=LODFadeMode.None; lodGroup.RecalculateBounds();
                AddColliders(root.transform,top,supports);
                var probe=Child(root.transform,"StaticMetalReflection",new Vector3(0,.85f,0)).gameObject.AddComponent<ReflectionProbe>();
                probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=cube;
                probe.size=new Vector3(2.3f,2.2f,1.8f);probe.boxProjection=false;probe.intensity=.8f;
                probe.blendDistance=.15f;probe.importance=1;
                string prefab=Root+"/Prefabs/SR_PlayTable.prefab";
                PrefabUtility.SaveAsPrefabAsset(root,prefab);
                CreateProxy();
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.ClosePreviewScene(previewScene); }
            Validate();
        }

        static Transform Child(Transform parent,string name,Vector3 pos)
        {
            var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);
            go.transform.SetParent(parent,false);go.transform.localPosition=pos;return go.transform;
        }
        static Transform ChildOnce(Transform p,string n,Vector3 pos) => p.Find(n) ? p.Find(n) : Child(p,n,pos);
        static string Group(string name)
        {
            if(name.StartsWith("Rail_")||name.StartsWith("Apron_"))return "Top/WoodSurroundAndAprons";
            if(name.StartsWith("CupHolder_"))return "Top/StainlessCupInserts";
            if(name.StartsWith("FeltInset"))return "Top/FeltInset";
            if(name.StartsWith("InsetFoundation"))return "Top/InsetFoundation";
            string leg=new[]{"FL","BL","FR","BR"}.Single(n=>name.Contains("_"+n+"_")||name.Contains("_"+n+"- ")||name.Contains("_"+n+"-0")||name.Contains("_"+n+"0"));
            string part=name.StartsWith("WoodLeg")?"WoodLeg":name.StartsWith("Extension")?"Extension":name.StartsWith("Foot_")?"LevellingFoot":name.StartsWith("Collar_")?"AdjustmentCollar":"MountingBracket";
            return "Support/"+leg+"/"+part;
        }
        static string MaterialName(string n) => n.StartsWith("Rail_")||n.StartsWith("Apron_")||n.StartsWith("WoodLeg")?"Oak":n.StartsWith("FeltInset")?"Felt":n.StartsWith("CupHolder")?"Stainless":n.StartsWith("Collar_")||n.StartsWith("Bracket_Bolt")?"Brass":"DarkMetal";

        static Dictionary<string,Material> MakeMaterials()
        {
            var result=new Dictionary<string,Material>();
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(!shader)throw new InvalidOperationException("URP Lit unavailable");
            foreach(string name in Materials)
            {
                foreach(string kind in new[]{"BaseColor","Normal","Mask"})
                {
                    var path=Root+"/Art/Textures/"+name+"_"+kind+".png";
                    var ti=(TextureImporter)AssetImporter.GetAtPath(path);
                    ti.textureType=kind=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                    ti.sRGBTexture=kind=="BaseColor";
                    ti.alphaSource=kind=="Mask"?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
                    ti.mipmapEnabled=true;ti.streamingMipmaps=true;ti.isReadable=false;
                    ti.wrapMode=TextureWrapMode.Repeat;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=2;
                    int size=name=="Oak"?1024:name=="Felt"?512:256;
                    ti.maxTextureSize=size;
                    ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=size,format=TextureImporterFormat.ASTC_6x6,compressionQuality=100});
                    ti.SaveAndReimport();
                }
                string matPath=Root+"/Materials/SR_Table_"+name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}
                mat.shader=shader;
                Texture2D Tex(string kind)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/"+name+"_"+kind+".png");
                mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",Tex("BaseColor"));
                mat.SetTexture("_BumpMap",Tex("Normal"));mat.SetFloat("_BumpScale",1);
                mat.SetTexture("_MetallicGlossMap",Tex("Mask"));mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",1);
                mat.SetFloat("_SmoothnessTextureChannel",0);mat.SetFloat("_Surface",0);mat.SetFloat("_Cull",2);
                mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.enableInstancing=true;EditorUtility.SetDirty(mat);result[name]=mat;
            }
            return result;
        }
        static Cubemap MakeStudioCube()
        {
            string path=Root+"/Materials/NeutralStudioReflection.asset";
            var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            bool fresh=!cube;
            if(fresh)cube=new Cubemap(64,TextureFormat.RGBA32,true){name="NeutralStudioReflection"};
            foreach(CubemapFace f in new[]{CubemapFace.PositiveX,CubemapFace.NegativeX,CubemapFace.PositiveY,CubemapFace.NegativeY,CubemapFace.PositiveZ,CubemapFace.NegativeZ})
            {
                var colors=new Color[64*64];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {
                    float u=(x+.5f)/64,v=(y+.5f)/64;
                    float panel=Mathf.Exp(-Mathf.Pow((u-.28f)/.10f,2))*Mathf.Clamp01((v-.15f)*4);
                    float val=f==CubemapFace.PositiveY?.75f:f==CubemapFace.NegativeY?.08f:.15f+.35f*v+.45f*panel;
                    colors[y*64+x]=new Color(val*.96f,val*.98f,val,1);
                }
                cube.SetPixels(colors,f);
            }
            cube.Apply(true,false);if(fresh)AssetDatabase.CreateAsset(cube,path);else EditorUtility.SetDirty(cube);
            return cube;
        }
        static void Box(Transform parent,string name,Vector3 worldCenter,Vector3 size)
        {
            var t=Child(parent,name,parent.InverseTransformPoint(worldCenter));
            var c=t.gameObject.AddComponent<BoxCollider>();c.size=size;
        }
        static void AddColliders(Transform root,Transform top,Transform supports)
        {
            var coll=Child(top,"CompoundColliders",Vector3.zero);
            Box(coll,"InsetSupport",new Vector3(0,1.0f,0),new Vector3(1.56f,.036f,1.11f));
            // Seven boxes per rail leave two square 110 mm openings around the real bores.
            int id=0;
            for(int axis=0;axis<2;axis++)foreach(int sign in new[]{-1,1})
            {
                float len=axis==0?1.9f:1.11f,width=axis==0?.145f:.17f,fixedPos=sign*(axis==0?.6275f:.865f),h=axis==0?.64f:.36f;
                float[] edges={-len/2,-h-.055f,-h+.055f,h-.055f,h+.055f,len/2};
                for(int j=0;j<3;j++)
                {
                    float a=edges[j*2],b=edges[j*2+1];
                    var cen=axis==0?new Vector3((a+b)/2,1.025f,fixedPos):new Vector3(fixedPos,1.025f,(a+b)/2);
                    var size=axis==0?new Vector3(b-a,.05f,width):new Vector3(width,.05f,b-a);
                    Box(coll,"RailSpan_"+id++,cen,size);
                }
                foreach(float hole in new[]{-h,h})foreach(int strip in new[]{-1,1})
                {
                    float sw=(width-.11f)/2,sp=fixedPos+strip*(.055f+sw/2);
                    Box(coll,"RailLip_"+id++,axis==0?new Vector3(hole,1.025f,sp):new Vector3(sp,1.025f,hole),axis==0?new Vector3(.11f,.05f,sw):new Vector3(sw,.05f,.11f));
                }
            }
            foreach(Transform leg in supports)
            {
                float x=leg.position.x,z=leg.position.z,sx=Mathf.Sign(x),sz=Mathf.Sign(z);
                Box(leg.Find("WoodLeg"),"Collider",new Vector3(x+sx*.011f,.642f,z+sz*.009f),new Vector3(.13f,.714f,.13f));
                Box(leg.Find("Extension"),"Collider",new Vector3(x+sx*.022f,.211f,z+sz*.018f),new Vector3(.072f,.342f,.072f));
                Box(leg.Find("LevellingFoot"),"Collider",new Vector3(x+sx*.022f,.0225f,z+sz*.018f),new Vector3(.084f,.045f,.084f));
            }
        }
        static void CreateProxy()
        {
            var s=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(go,s);
                go.name="TerrainFootprint_PROXY_ONLY_1500x1050";
                go.transform.position=new Vector3(0,.06f,0);go.transform.localScale=new Vector3(1.5f,.12f,1.05f);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/SR_Table_Felt.mat");
                var wrapper=new GameObject("TerrainFitProxy_NOT_FOR_PRODUCTION");SceneManager.MoveGameObjectToScene(wrapper,s);go.transform.SetParent(wrapper.transform,true);
                PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"/Validation/TerrainFitProxy.prefab");
            }
            finally{EditorSceneManager.ClosePreviewScene(s);}
        }
        public static void Validate()
        {
            Guard();
            var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/SR_PlayTable.prefab");
            try
            {
                var lines=new List<string>();
                var lods=root.GetComponent<LODGroup>().GetLODs();
                foreach(var pair in lods.Select((lod,i)=>(lod,i)))
                {
                    int tris=0,vertices=0,slots=0;Bounds b=new Bounds();bool first=true;
                    foreach(var r in pair.lod.renderers)
                    {
                        var mesh=r.GetComponent<MeshFilter>().sharedMesh;
                        for(int sub=0;sub<mesh.subMeshCount;sub++)tris+=(int)mesh.GetIndexCount(sub)/3;
                        vertices+=mesh.vertexCount;slots+=r.sharedMaterials.Length;
                        if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);
                        if(r.sharedMaterials.Any(m=>!m||m.shader.name!="Universal Render Pipeline/Lit"))throw new Exception("Material assignment failed");
                    }
                    if(Mathf.Abs(b.size.x-1.9f)>.003f||Mathf.Abs(b.size.z-1.4f)>.003f||b.min.y<-.002f||Mathf.Abs(b.max.y-1.053f)>.003f)throw new Exception("Unexpected imported bounds "+b);
                    lines.Add($"LOD{pair.i}: {tris} triangles; {vertices} imported vertices; {pair.lod.renderers.Length} renderers; {slots} material slots; bounds {b}");
                }
                foreach(string n in new[]{"TabletopPivot","TownMount","TownFootprint","Supports"})if(!root.GetComponentsInChildren<Transform>(true).Any(t=>t.name==n))throw new Exception("Missing "+n);
                if(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name.Contains("PROXY")))throw new Exception("Proxy leaked into production");
                if(root.GetComponentsInChildren<Transform>(true).Any(t=>Vector3.Distance(t.localScale,Vector3.one)>.0001f||Quaternion.Angle(t.localRotation,Quaternion.identity)>.01f))throw new Exception("Unclean transforms");
                var colliders=root.GetComponentsInChildren<Collider>(true);
                if(colliders.Any(c=>!(c is BoxCollider)))throw new Exception("Unexpected collider type");
                lines.Add("41 compound BoxColliders expected; actual="+colliders.Length+". Cup inserts, collars and bracket details have no colliders.");
                lines.Add("TownMount=(0,1.018,0); clear inset=1.56 x 1.11 m; proxy=1.50 x 1.05 m; clearance=0.03 m per side.");
                lines.Add("All prefab transforms: scale one, rotation identity. +Y up, +Z depth, +X width. Floor y=0.");
                lines.Add("5 reusable URP Lit materials; 15 PNGs: Oak 1024, Felt 512, 3 metals 256; Android ASTC 6x6; mipmapped. Custom 64px neutral studio cubemap, no real-time captures.");
                lines.Add("Play Mode="+EditorApplication.isPlaying+". No runtime testing, builds, device operations or scene replacement.");
                Directory.CreateDirectory(Report);File.WriteAllLines(Report+"/unity_validation.txt",lines);
                Debug.Log(string.Join("\n",lines));
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
