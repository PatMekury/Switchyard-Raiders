using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using SwitchyardRaiders.Startup;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    // Targeted migration: never invokes the broad launch/integration builders.
    public static partial class RecessedTableTask1
    {
        const string Root="Assets/SwitchyardRaiders/RecessedTable";
        const string Table="Assets/SwitchyardRaiders/PlayTable/Prefabs/SR_PlayTable.prefab";
        const string Land="Assets/SwitchyardRaiders/TerrainBase/Prefabs/SR_TerrainBase.prefab";
        const string Launch="Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab";
        const string Presentation="Assets/SwitchyardRaiders/Startup/Prefabs/SR_TabletopPresentation.prefab";
        const string Output="Deliverables/DemeoRework";
        static void Guard()
        {
            if(!Application.dataPath.Replace('\\','/').EndsWith("/Switchyard Raiders/Assets") || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires Switchyard Raiders in Edit Mode.");
        }
        static Transform Child(Transform p,string name,Vector3 position)
        {
            var t=p.Find(name);
            if(!t){var go=new GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,p.gameObject.scene);go.transform.SetParent(p,false);t=go.transform;}
            t.localPosition=position;return t;
        }
        static MeshFilter[] Import(string name,int level)
        {
            string path=Root+"/Art/"+name+"_LOD"+level+".fbx";
            var imp=(ModelImporter)AssetImporter.GetAtPath(path);
            if(!imp)throw new Exception("Missing "+path);
            imp.importAnimation=false;imp.importCameras=false;imp.importLights=false;imp.addCollider=false;
            imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importNormals=ModelImporterNormals.Import;
            imp.importTangents=ModelImporterTangents.CalculateMikk;imp.globalScale=1;imp.useFileScale=true;imp.bakeAxisConversion=true;
            imp.preserveHierarchy=true;imp.meshCompression=ModelImporterMeshCompression.Off;imp.isReadable=true;imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>(true);
        }
        static string Group(string n)
        {
            if(n.StartsWith("Rail_")||n.StartsWith("Apron_"))return "TabletopPivot/WoodSurroundAndAprons";
            if(n.StartsWith("CupHolder_"))return "TabletopPivot/StainlessCupInserts";
            if(n.StartsWith("FeltInset"))return "TabletopPivot/FeltInset";
            if(n.StartsWith("InsetFoundation"))return "TabletopPivot/InsetFoundation";
            string leg=new[]{"FL","BL","FR","BR"}.Single(x=>n.Contains("_"+x+"_")||n.Contains("_"+x+"-")||n.Contains("_"+x+"0"));
            string part=n.StartsWith("WoodLeg")?"WoodLeg":n.StartsWith("Extension")?"Extension":n.StartsWith("Foot_")?"LevellingFoot":n.StartsWith("Collar_")?"AdjustmentCollar":"MountingBracket";
            return "Supports/Leg_"+leg+"/"+part;
        }
        static string MaterialName(string n)=>n.StartsWith("Rail_")||n.StartsWith("Apron_")||n.StartsWith("WoodLeg")?"Oak":n.StartsWith("FeltInset")?"Felt":n.StartsWith("CupHolder")?"Stainless":n.StartsWith("Collar_")||n.StartsWith("Bracket_Bolt")?"Brass":"DarkMetal";
        static Mesh SaveMesh(Mesh mesh,string name)
        {
            mesh.name=name;mesh.RecalculateBounds();mesh.RecalculateTangents();
            string path=Root+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){old.Clear();EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);old.UploadMeshData(false);Object.DestroyImmediate(mesh);return old;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static void Box(Transform p,string name,Vector3 center,Vector3 size)
        {var c=Child(p,name,Vector3.zero).gameObject.AddComponent<BoxCollider>();c.center=center;c.size=size;}
        [MenuItem("Switchyard Raiders/Demeo Rework/1 - Import recessed assets")]
        public static void BuildAssets()
        {
            Guard();Directory.CreateDirectory(Root+"/Meshes");AssetDatabase.Refresh();
            var table=PrefabUtility.LoadPrefabContents(Table);
            try
            {
                var pivot=table.transform.Find("TabletopPivot");pivot.localPosition=new Vector3(0,1.025f,0);
                var mount=pivot.Find("TownMount");mount.localPosition=new Vector3(0,TabletopViewContract.TerrainMountHeight-1.025f,0);
                var footprint=mount.Find("TownFootprint");footprint.localPosition=Vector3.zero;
                foreach(Transform corner in footprint)corner.localPosition=new Vector3(corner.name.Contains("Left")?-.525f:.525f,0,corner.name.Contains("Front")?-.525f:.525f);
                foreach(Transform leg in table.transform.Find("Supports"))leg.localPosition=new Vector3(leg.name.Contains("L")?-.55f:.55f,0,leg.name.Contains("F")?-.55f:.55f);
                var lods=new List<LOD>();
                for(int level=0;level<2;level++)
                {
                    var rs=new List<Renderer>();
                    foreach(var group in Import("SR_RecessedTable",level).GroupBy(x=>Group(x.name)))
                    {
                        var anchor=table.transform.Find(group.Key);if(!anchor)throw new Exception("Missing reusable anchor "+group.Key);
                        var visual=anchor.Find("LOD"+level);var meshes=new List<Mesh>();var mats=new List<Material>();
                        foreach(var mg in group.GroupBy(x=>MaterialName(x.name)))
                        {
                            var m=new Mesh{indexFormat=IndexFormat.UInt32};
                            m.CombineMeshes(mg.Select(x=>new CombineInstance{mesh=x.sharedMesh,transform=anchor.worldToLocalMatrix*x.transform.localToWorldMatrix}).ToArray(),true,true,false);
                            meshes.Add(m);mats.Add(AssetDatabase.LoadAssetAtPath<Material>("Assets/SwitchyardRaiders/PlayTable/Materials/SR_Table_"+mg.Key+".mat"));
                        }
                        var merged=new Mesh{indexFormat=IndexFormat.UInt32};merged.CombineMeshes(meshes.Select(x=>new CombineInstance{mesh=x,transform=Matrix4x4.identity}).ToArray(),false,false,false);
                        visual.GetComponent<MeshFilter>().sharedMesh=SaveMesh(merged,group.Key.Replace('/','_')+"_LOD"+level);
                        var renderer=visual.GetComponent<MeshRenderer>();renderer.sharedMaterials=mats.ToArray();rs.Add(renderer);
                        foreach(var m in meshes)Object.DestroyImmediate(m);
                    }
                    lods.Add(new LOD(level==0?.30f:.005f,rs.ToArray()));
                }
                var lg=table.GetComponent<LODGroup>();lg.SetLODs(lods.ToArray());lg.RecalculateBounds();
                foreach(var c in table.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
                Box(pivot,"Recess floor collider",new Vector3(0,.865f-1.025f,0),new Vector3(1.10f,.03f,1.10f));
                for(int i=0;i<4;i++)
                {
                    bool x=i<2;float sign=i%2==0?-1:1;
                    Box(pivot,"Well wall "+i,new Vector3(x?sign*.55f:0,.9425f-1.025f,x?0:sign*.55f),x?new Vector3(.036f,.185f,1.064f):new Vector3(1.16f,.185f,.036f));
                    Box(pivot,"Rim "+i,new Vector3(x?sign*.5875f:0,0,x?0:sign*.5875f),x?new Vector3(.125f,.05f,1.05f):new Vector3(1.30f,.05f,.125f));
                }
                foreach(Transform leg in table.transform.Find("Supports"))Box(leg,"Support collider",new Vector3(0,.425f,0),new Vector3(.12f,.85f,.12f));
                var probe=table.GetComponentInChildren<ReflectionProbe>(true);if(probe)probe.size=new Vector3(1.6f,1.4f,1.6f);
                PrefabUtility.SaveAsPrefabAsset(table,Table);
            }
            finally{PrefabUtility.UnloadPrefabContents(table);}
            var land=PrefabUtility.LoadPrefabContents(Land);
            try
            {
                foreach(Transform t in land.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(t.gameObject);
                var lods=new List<LOD>();
                for(int i=0;i<2;i++)
                {
                    var model=Import("SR_RecessedLand",i).Single();var mesh=new Mesh();mesh.CombineMeshes(new[]{new CombineInstance{mesh=model.sharedMesh,transform=model.transform.localToWorldMatrix}},true,true,false);
                    var go=Child(land.transform,"LandGrid_LOD"+i,Vector3.zero).gameObject;
                    go.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,"LandGrid_LOD"+i);var r=go.AddComponent<MeshRenderer>();
                    r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/SwitchyardRaiders/TerrainBase/Materials/SR_Terrain_EarthGreen.mat");
                    lods.Add(new LOD(i==0?.3f:.005f,new Renderer[]{r}));
                }
                var group=land.GetComponent<LODGroup>();group.SetLODs(lods.ToArray());group.RecalculateBounds();
                var col=land.GetComponent<BoxCollider>();col.center=new Vector3(0,.075f,0);col.size=new Vector3(TabletopViewContract.SlabSize,.15f,TabletopViewContract.SlabSize);
                Child(land.transform,"TerrainDatum",new Vector3(0,TabletopViewContract.TerrainDatum,0));
                PrefabUtility.SaveAsPrefabAsset(land,Land);
            }
            finally{PrefabUtility.UnloadPrefabContents(land);}
            AssetDatabase.SaveAssets();Debug.Log("Recessed table and land imported. Existing source versions retained in Task1 Baseline.");
        }
        static void Configure(Transform presentation)
        {
            var table=presentation.Find("Virtual table");table.localPosition=new Vector3(0,-TabletopViewContract.TerrainMountHeight,0);
            var mount=presentation.Find("Terrain mounting surface");mount.localPosition=Vector3.zero;
            var game=mount.Find("Gameplay railway and controls");game.localPosition=new Vector3(0,TabletopViewContract.GameplayHeight,0);game.localScale=Vector3.one*TabletopViewContract.GameplayScale;
            foreach(Transform child in game)
                if(child.name.Contains("authored locomotive"))child.localScale=Vector3.one*(TabletopViewContract.PreservedTrainScale/TabletopViewContract.GameplayScale);
            var seats=presentation.Find("Player seats");if(seats)for(int i=0;i<4;i++)seats.GetChild(i).localPosition=TabletopViewMath.SeatPosition(i,4);
            var handles=presentation.Find("Manipulation handles");foreach(Transform h in handles)h.localPosition=new Vector3(h.name.StartsWith("Left")?-.5875f:.5875f,TabletopViewContract.RimHeight,-.30f);
            foreach(var tr in presentation.GetComponentsInChildren<Transform>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(tr);
        }
        static void ConfigureTable(RaidersTable t)
        {
            Configure(t.presentation);
            if(t.placementPreview){float h=TabletopViewContract.OuterSize*.5f;t.placementPreview.SetPositions(new[]{new Vector3(-h,0,-h),new Vector3(h,0,-h),new Vector3(h,0,h),new Vector3(-h,0,h)});PrefabUtility.RecordPrefabInstancePropertyModifications(t.placementPreview);}
            // Refresh only collider dimensions; retain the other tab's contact-grab wiring.
        }
        [MenuItem("Switchyard Raiders/Demeo Rework/2 - Migrate presentation dimensions")]
        public static void Integrate()
        {
            Guard();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/SwitchyardRaiders/Startup/Scenes/SR_Launch.unity")throw new Exception("Open the intended SR_Launch scene before migration.");
            if(scene.isDirty)throw new Exception("Scene has unsaved changes; save or resolve them before this targeted migration.");
            foreach(string path in new[]{Launch,Presentation})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try{var t=root.GetComponentInChildren<RaidersTable>(true);if(t)ConfigureTable(t);else Configure(root.transform);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var table=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<RaidersTable>(true)).Single();
            Undo.RegisterFullObjectHierarchyUndo(table.launch.gameObject,"Recessed table dimensions");ConfigureTable(table);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("Task1 targeted migration saved; gameplay, runners, rig, session and grab wiring preserved.");
        }
    }
}
