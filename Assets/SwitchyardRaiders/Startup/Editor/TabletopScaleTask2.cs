using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SwitchyardRaiders.Startup;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    // Editable, deterministic authoring source. Does not regenerate gameplay/session wiring.
    public static partial class TabletopScaleTask2
    {
        public const string Folder="Assets/SwitchyardRaiders/RecessedTable/Railway";
        const string Launch="Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab";
        const string Presentation=TabletopIntegrationSetup.Prefab;
        static void Guard()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !Application.dataPath.EndsWith("Switchyard Raiders/Assets"))throw new Exception("Switchyard Raiders Edit Mode required.");
        }
        sealed class Geometry
        {
            readonly List<Vector3> v=new();readonly List<Vector2> uv=new();readonly List<int> ix=new();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int i=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});ix.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            }
            public void Box(Vector3 center,Vector3 size,Quaternion q)
            {
                Vector3 P(float x,float y,float z)=>center+q*Vector3.Scale(size*.5f,new Vector3(x,y,z));
                Quad(P(-1,-1,-1),P(-1,1,-1),P(1,1,-1),P(1,-1,-1));
                Quad(P(1,-1,1),P(1,1,1),P(-1,1,1),P(-1,-1,1));
                Quad(P(-1,-1,1),P(-1,1,1),P(-1,1,-1),P(-1,-1,-1));
                Quad(P(1,-1,-1),P(1,1,-1),P(1,1,1),P(1,-1,1));
                Quad(P(-1,1,-1),P(-1,1,1),P(1,1,1),P(1,1,-1));
                Quad(P(-1,-1,1),P(-1,-1,-1),P(1,-1,-1),P(1,-1,1));
            }
            public void Wheel(Vector3 center,float radius,float width,int segments)
            {
                for(int i=0;i<segments;i++)
                {
                    float a=-i*Mathf.PI*2/segments,b=-(i+1)*Mathf.PI*2/segments;
                    Vector3 P(float x,float angle)=>center+new Vector3(x,Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius);
                    Quad(P(-width*.5f,a),P(width*.5f,a),P(width*.5f,b),P(-width*.5f,b));
                    // Quads with a repeated center produce cap triangles; separate triangle below avoids degeneracy.
                    Triangle(center+Vector3.left*width*.5f,P(-width*.5f,a),P(-width*.5f,b));
                    Triangle(center+Vector3.right*width*.5f,P(width*.5f,b),P(width*.5f,a));
                }
            }
            void Triangle(Vector3 a,Vector3 b,Vector3 c){int i=v.Count;v.AddRange(new[]{a,b,c});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.up});ix.AddRange(new[]{i,i+1,i+2});}
            public Mesh Save(string name)
            {
                var mesh=new Mesh{name=name,indexFormat=v.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(ix,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                string path=Folder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!old){AssetDatabase.CreateAsset(mesh,path);return mesh;}
                old.Clear();EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);old.UploadMeshData(false);Object.DestroyImmediate(mesh);return old;
            }
        }
        static Transform Child(Transform parent,string name)
        {var child=parent.Find(name);if(child)return child;var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        static MeshRenderer Part(Transform parent,string name,Mesh mesh,Material material)
        {
            var t=Child(parent,name);t.localPosition=Vector3.zero;t.localRotation=Quaternion.identity;t.localScale=Vector3.one;
            var mf=t.GetComponent<MeshFilter>();if(!mf)mf=t.gameObject.AddComponent<MeshFilter>();mf.sharedMesh=mesh;
            var r=t.GetComponent<MeshRenderer>();if(!r)r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;return r;
        }
        static Material NewMaterial(string name,Color color,float metallic)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",metallic>.1f?.5f:.2f);AssetDatabase.CreateAsset(m,path);return m;
        }
        static void MakeMeshes()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            for(int lod=0;lod<2;lod++)
            {
                int segments=lod==0?384:192;var rails=new Geometry();var ties=new Geometry();var bed=new Geometry();
                // Rolled rail: broad foot, narrow web, chamfered crown; 1.435 m inner-face gauge at 1:100.
                float w=TabletopViewContract.RailHeadWidth*.5f;
                var profile=new[]{new Vector2(-.0012f,-.0018f),new Vector2(.0012f,-.0018f),new Vector2(.0012f,-.0013f),new Vector2(.0003f,-.0013f),new Vector2(.0003f,.0021f),new Vector2(w,.0021f),new Vector2(w,.0028f),new Vector2(w-.0002f,.003f),new Vector2(-w+.0002f,.003f),new Vector2(-w,.0028f),new Vector2(-w,.0021f),new Vector2(-.0003f,.0021f),new Vector2(-.0003f,-.0013f),new Vector2(-.0012f,-.0013f)};
                Vector3 Profile(float d,float x,float y){var p=TabletopRailway.Point(d);var radial=new Vector3(p.x,0,p.z).normalized;return p+radial*x+Vector3.up*y;}
                for(int i=0;i<segments;i++)
                {
                    float a=i*RaidersMatch.Length/segments,b=(i+1)*RaidersMatch.Length/segments;
                    foreach(int side in new[]{-1,1})for(int j=0;j<profile.Length;j++)
                    {var p=profile[j];var q=profile[(j+1)%profile.Length];float x=side*(TabletopViewContract.RailGauge+TabletopViewContract.RailHeadWidth)*.5f;
                        rails.Quad(Profile(a,x+p.x,p.y),Profile(b,x+p.x,p.y),Profile(b,x+q.x,q.y),Profile(a,x+q.x,q.y));}
                    Vector3 Ground(float d,float side){var p=Profile(d,side*(.020f+TabletopRailway.Point(d).y),0);p.y=-.004f;return p;}
                    bed.Quad(Ground(a,-1),Profile(a,-.018f,-.003f),Profile(b,-.018f,-.003f),Ground(b,-1));
                    bed.Quad(Profile(a,-.018f,-.003f),Profile(a,.018f,-.003f),Profile(b,.018f,-.003f),Profile(b,-.018f,-.003f));
                    bed.Quad(Profile(a,.018f,-.003f),Ground(a,1),Ground(b,1),Profile(b,.018f,-.003f));
                }
                for(int site=0;site<8;site++)
                {
                    var p=TabletopRailway.Point(RaidersMatch.Site(site));var outward=new Vector3(p.x,0,p.z).normalized;var center=p+outward*.057f;float height=p.y+.002f;center.y=-.004f+height*.5f;
                    bed.Box(center,new Vector3(.025f,height,.025f),Quaternion.identity);
                }
                int sleepers=lod==0?384:192;
                for(int i=0;i<sleepers;i++){float d=i*RaidersMatch.Length/sleepers;ties.Box(TabletopRailway.Point(d)+Vector3.down*.0024f,new Vector3(.028f,.0012f,.0024f),Quaternion.LookRotation(TabletopRailway.Tangent(d)));}
                rails.Save("Rails_LOD"+lod);ties.Save("Sleepers_LOD"+lod);bed.Save("Raised formation_LOD"+lod);
            }
            for(int lod=0;lod<2;lod++)
            {
                var chassis=new Geometry();chassis.Box(new Vector3(0,.008f,0),new Vector3(TabletopViewContract.WagonWidth,.004f,TabletopViewContract.WagonBodyLength),Quaternion.identity);
                for(int side=-1;side<=1;side+=2)
                {
                    chassis.Box(new Vector3(side*.011f,.0065f,0),new Vector3(.002f,.005f,.045f),Quaternion.identity);
                    chassis.Box(new Vector3(0,TabletopViewContract.CouplerHeight,side*(TabletopRailway.WagonPitch*.5f-.001f)),new Vector3(.003f,.002f,.003f),Quaternion.identity);
                    foreach(float x in new[]{-.008f,.008f})chassis.Wheel(new Vector3(x,.0045f,side*.016f),.0045f,.002f,lod==0?16:8);
                }
                chassis.Save("Freight chassis_LOD"+lod);
            }
        }
        static void Configure(Transform presentation)
        {
            var b=presentation.GetComponent<RaidersTabletopBindings>();var game=b.gameplay;
            game.localScale=Vector3.one;game.localPosition=Vector3.up*TabletopViewContract.GameplayHeight;
            var old=game.Find("Editable railway");if(old)old.gameObject.SetActive(false);
            var steel=NewMaterial("Calibrated rail steel",new Color(.34f,.39f,.39f),.7f);
            var wood=NewMaterial("Railway sleeper oak",new Color(.16f,.105f,.065f),0);
            var ballast=NewMaterial("Railway earth formation",new Color(.27f,.25f,.20f),0);
            var track=Child(game,"Calibrated railway");var lg=track.GetComponent<LODGroup>();if(!lg)lg=track.gameObject.AddComponent<LODGroup>();var levels=new LOD[2];
            for(int lod=0;lod<2;lod++)
            {
                var level=Child(track,"LOD"+lod);var renderers=new[]{Part(level,"Rail pair",AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Rails_LOD"+lod+".asset"),steel),Part(level,"Sleepers",AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Sleepers_LOD"+lod+".asset"),wood),Part(level,"Formation",AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Raised formation_LOD"+lod+".asset"),ballast)};
                levels[lod]=new LOD(lod==0?.25f:.005f,renderers);
            }
            lg.SetLODs(levels);lg.RecalculateBounds();
            foreach(Transform tr in game)if(tr.name.Contains("authored locomotive"))
            {
                tr.localScale=Vector3.one*TabletopViewContract.MiniatureScale;
                bool company=tr.name.StartsWith("Company");int player=company?0:int.Parse(tr.name.Substring(7,1))-1;
                TabletopRailway.Place(tr,company?8:(player%2)*16+(player/2)*2.2f,true,company?.0185f:.0415f);
            }
            for(int i=0;i<6;i++)
            {
                var wagon=game.Find("Cargo wagon "+(i+1));wagon.localScale=Vector3.one;
                var chassis=wagon.Find("Chassis");if(chassis)chassis.gameObject.SetActive(false);
                var lodGroup=wagon.GetComponent<LODGroup>();if(!lodGroup)lodGroup=wagon.gameObject.AddComponent<LODGroup>();var lods=new LOD[2];
                for(int lod=0;lod<2;lod++)lods[lod]=new LOD(lod==0?.04f:.002f,new Renderer[]{Part(wagon,"Running gear LOD"+lod,AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Freight chassis_LOD"+lod+".asset"),steel)});
                lodGroup.SetLODs(lods);lodGroup.RecalculateBounds();
                var cargo=wagon.Find("Cargo");cargo.localPosition=new Vector3(0,.022f,0);cargo.localScale=new Vector3(.024f,.024f,.043f);
                foreach(int sign in new[]{-1,1}){var anchor=Child(wagon,sign<0?"Coupler_Rear":"Coupler_Front");anchor.localPosition=new Vector3(0,TabletopViewContract.CouplerHeight,sign*(TabletopRailway.WagonPitch*.5f+.0005f));}
                var link=Child(wagon,"Coupling link");var line=link.GetComponent<LineRenderer>();if(!line)line=link.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;line.widthMultiplier=.0013f;line.numCapVertices=3;line.sharedMaterial=steel;line.enabled=false;
                TabletopRailway.Place(wagon,8-(i+1)*RaidersMatch.Spacing,false,.016f);
            }
            for(int i=0;i<8;i++)
            {
                var site=game.Find("Emplacement "+(i+1));var p=TabletopRailway.Point(RaidersMatch.Site(i));var outward=new Vector3(p.x,0,p.z).normalized;
                site.localPosition=p+outward*.057f;site.localScale=Vector3.one;
                // Primitive devices are re-authored at about 1.5 x 2.2 x 2.8 full-scale metres.
                var head=site.Find("Decoupler/Aiming head");head.localScale=new Vector3(.015f,.022f,.028f);head.localPosition=Vector3.up*.012f;
                site.Find("Site marker").localScale=new Vector3(.025f,.0035f,.025f);
                var background=site.Find("Decoupler/Window background");background.localPosition=Vector3.up*.0325f;background.localScale=new Vector3(.030f,.0025f,.004f);
                var bar=site.Find("Decoupler/Firing window");bar.localPosition=Vector3.up*.0325f;bar.localScale=new Vector3(0,.003f,.004f);
                var number=site.Find("Emplacement number");number.localPosition=new Vector3(0,.006f,.0175f);number.GetComponent<TextMesh>().characterSize=.007f;
            }
            for(int i=0;i<2;i++){var pad=game.Find("Player "+(i+1)+" unloading area");pad.localPosition=TabletopRailway.Point(RaidersMatch.Home(i))+Vector3.down*.002f;pad.localScale=new Vector3(.24f,.003f,.07f);}
            foreach(var tr in presentation.GetComponentsInChildren<Transform>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(tr);
        }
        static void Humans(RaidersTable table)
        {
            var seats=table.seats[0].parent;seats.SetParent(table.presentation.parent,false);seats.name="Human scale player seats";seats.localScale=Vector3.one;table.humanPresentation=seats;
            table.presentation.GetComponent<RaidersTabletopBindings>().seats=Array.Empty<Transform>();
            EditorUtility.SetDirty(table);PrefabUtility.RecordPrefabInstancePropertyModifications(table);
        }
        [MenuItem("Switchyard Raiders/Demeo Rework/4 - Calibrate railway and miniature scale")]
        public static void Build()
        {
            Guard();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/SwitchyardRaiders/Startup/Scenes/SR_Launch.unity"||scene.isDirty)throw new Exception("Saved SR_Launch required; do not overwrite another tab's unsaved work.");
            MakeMeshes();
            foreach(string path in new[]{Launch,Presentation})
            {
                var go=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var table=go.GetComponentInChildren<RaidersTable>(true);var p=table?table.presentation:go.transform;Configure(p);
                    if(table)Humans(table);
                    else
                    {
                        var seats=p.Find("Player seats");if(seats){PrefabUtility.SaveAsPrefabAsset(seats.gameObject,Folder+"/SR_HumanSeats.prefab");Object.DestroyImmediate(seats.gameObject);}
                        p.GetComponent<RaidersTabletopBindings>().seats=Array.Empty<Transform>();
                    }
                    PrefabUtility.SaveAsPrefabAsset(go,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(go);}
            }
            var live=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RaidersTable>(true)).Single();
            Undo.RegisterFullObjectHierarchyUndo(live.launch.gameObject,"Task 2 calibrated railway");Configure(live.presentation);Humans(live);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Calibrated 1:100 railway, locomotives, freight and device placements; human seats outside zoom branch. No simulation or rig replacement.");
        }
    }
}
