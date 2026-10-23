using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;
using SwitchyardRaiders.Startup;
using SwitchyardRaiders.CompanyTrain;
using SwitchyardRaiders.PlayerVehicles;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static partial class TabletopScaleTask2
    {
        static void Field(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
        static void Invoke(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);
        static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        [MenuItem("Switchyard Raiders/Demeo Rework/6 - Validate scale bounds and train fit")]
        public static void Validate()
        {
            Guard();var log=new StringBuilder();int count=0;
            void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;log.AppendLine("PASS "+message);}
            var fixture=new GameObject("Inactive Task2 bounds fixture");fixture.SetActive(false);
            try
            {
                Transform Node(string name){var g=new GameObject(name);g.transform.SetParent(fixture.transform,false);return g.transform;}
                var table=fixture.AddComponent<RaidersTable>();var mode=fixture.AddComponent<RoomChoiceMenu>();var session=fixture.AddComponent<RaidersSession>();var launch=fixture.AddComponent<LaunchSequence>();var input=fixture.AddComponent<RaidersTableManipulation>();
                table.roomMenu=mode;table.session=session;table.launch=launch;table.manipulation=input;input.table=table;launch.headset=Node("Tracked head");launch.headset.position=new Vector3(0,1.4f,0);
                table.presentation=Node("View");table.root=Node("Gameplay");table.virtualTable=Node("Table").gameObject;table.terrainAssembly=Node("Land").gameObject;table.humanPresentation=Node("Human seats");
                Field(mode,"<IsVirtual>k__BackingField",true);Field(mode,"<HasSelection>k__BackingField",true);Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Playing);table.ModeChosen();
                float time=0;float distance=.4f;
                void Step(bool closed=true){time+=.11f;input.ProcessHands(new RaidersTableManipulation.HandSample(true,closed,Vector3.left*distance*.5f),new RaidersTableManipulation.HandSample(true,closed,Vector3.right*distance*.5f),time);}
                void Hands(float d){distance=d;}
                Step(false);Step();Step();Hands(10);Step();Check(table.RelativeZoom==6,"Upper bound clamps huge overshoot to 6x");
                var held=table.presentation.position;Step();Check(Vector3.Distance(held,table.presentation.position)<1e-5f,"Stationary overshoot does not drift pivot");
                Hands(9.9f);Step();Check(table.RelativeZoom<6&&table.RelativeZoom>5.9f,"Immediate reverse from maximum");
                Hands(.08f);Step();Check(table.RelativeZoom==1,"Lower bound is exactly normal");
                Hands(.09f);Step();Check(table.RelativeZoom>1&&table.RelativeZoom<1.2f,"Immediate reverse from minimum");
                Step(false);held=table.presentation.position;float scale=table.RelativeZoom;Step();Step();Step();Check(Vector3.Distance(held,table.presentation.position)<1e-5f&&Mathf.Abs(table.RelativeZoom-scale)<1e-5f,"Regrab retains accepted view");
                table.ResetView();Check(table.RelativeZoom==1,"Reset restores exactly the lower bound");
                table.presentation.localScale=Vector3.one*.2f;table.PrepareModeChange();table.ModeChosen();Check(table.RelativeZoom==1,"Legacy below-normal saved VR state is clamped on mode restoration");
                table.presentation.localScale=Vector3.one*99;table.PrepareModeChange();table.ModeChosen();Check(table.RelativeZoom==6,"Legacy excessive saved VR state is clamped on restoration");
                Check(TabletopViewMath.ClampScale(float.NaN,1)==1&&TabletopViewMath.ClampScale(float.PositiveInfinity,1)==1,"Nonfinite external scale is sanitized before transform assignment");
                Check(!table.AcceptView(Vector3.zero,Quaternion.identity,.99f)&&!table.AcceptView(Vector3.zero,Quaternion.identity,6.01f),"View acceptance rejects both out-of-range requests");
                Field(mode,"<IsVirtual>k__BackingField",false);Field(table,"homeScale",.6f);table.presentation.localScale=Vector3.one*.2f;table.EnforceViewBounds();Check(Mathf.Abs(table.RelativeZoom-1)<1e-5f,"MR minimum is 1x relative to confirmed placement");
                table.presentation.localScale=Vector3.one*100;table.EnforceViewBounds();Check(Mathf.Abs(table.RelativeZoom-6)<1e-5f,"MR maximum is relative, not absolute 6 world metres");
                Field(mode,"<IsVirtual>k__BackingField",true);table.ModeChosen();Check(table.RelativeZoom==6,"Returning from MR preserves the valid personal VR inspection scale");
                Check(launch.headset.position==new Vector3(0,1.4f,0)&&table.humanPresentation.lossyScale==Vector3.one,"Head and human seats retain human scale through mode/zoom changes");
            }
            finally{Object.DestroyImmediate(fixture);}
            var root=PrefabUtility.LoadPrefabContents(Launch);
            try
            {
                var t=root.GetComponentInChildren<RaidersTable>(true);t.presentation.localScale=Vector3.one;t.presentation.position=Vector3.zero;t.presentation.rotation=Quaternion.identity;
                Check(t.root.localScale==Vector3.one,"Gameplay branch has no compensating parent scale");
                Check(t.seats.All(s=>!s.IsChildOf(t.presentation))&&t.humanPresentation.localScale==Vector3.one,"All remote avatar seats are outside the miniature scale branch");
                Check(!t.launch.xrRigRoot.transform.IsChildOf(t.presentation),"Tracked rig remains outside scale ownership");
                Check(t.root.Find("Calibrated railway")&&!t.root.Find("Editable railway").gameObject.activeSelf,"Gauge-correct railway replaces the recoverable ribbon");
                var m=new RaidersMatch();m.Reset(false,4);Field(t.session,"<Match>k__BackingField",m);
                var steam=t.company.GetComponent<CompanyLocomotiveMotion>();var sf=t.company.GetComponent<CompanyTableFollower>();sf.table=t;Invoke(sf,"Awake");
                var followers=t.locomotives.Select(tr=>tr.GetComponent<PlayerVehicleTrackFollower>()).ToArray();foreach(var f in followers){f.table=t;Invoke(f,"Awake");}
                var engines=t.locomotives.Concat(new[]{t.company}).ToArray();
                foreach(var train in engines)
                {
                    var b=ModelBounds(train);Check(Mathf.Abs(train.localScale.x-.01f)<1e-6f,train.name+" common 1:100 scale");
                    log.AppendLine(train.name+" authored bounds="+b.size.ToString("F5")+" normal displayed="+(b.size*.01f).ToString("F5"));
                    Check(b.size.z*.01f>.147f&&b.size.z*.01f<.149f,train.name+" measured length ~148mm");
                    Check(train.GetComponent<LODGroup>().GetLODs().Length>=3,train.name+" authored LOD hierarchy retained");
                }
                float maxEdge=0,maxPilotRadial=0,maxPilotVertical=0,maxPlayerRadial=0,maxPlayerVertical=0,maxLink=0,minBodyGap=1;
                var engineBounds=engines.Select(ModelBounds).ToArray();
                for(int sample=0;sample<256;sample++)
                {
                    float d=sample*RaidersMatch.Length/256;m.companyDistance=d;TabletopRailway.Place(t.company,d,true,.0185f);sf.Evaluate();
                    for(int i=0;i<4;i++){m.players[i].distance=d;TabletopRailway.Place(t.locomotives[i],d,true,.0415f);followers[i].Evaluate();}
                    foreach(var axle in t.locomotives[0].GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("Wheel_")))
                    {
                        var p=t.root.InverseTransformPoint(axle.position);float at=Mathf.Atan2(p.x,p.z)*RaidersMatch.Length/(2*Mathf.PI);var rail=TabletopRailway.Point(at);
                        maxPlayerRadial=Mathf.Max(maxPlayerRadial,Mathf.Abs(new Vector2(p.x,p.z).magnitude-TabletopViewContract.RailRadius));maxPlayerVertical=Mathf.Max(maxPlayerVertical,Mathf.Abs(p.y-.006f-TabletopViewContract.RailHeadHeight-rail.y));
                    }
                    for(int k=0;k<engines.Length;k++)for(int corner=0;corner<8;corner++)
                    {
                        var b=engineBounds[k];var p=t.root.InverseTransformPoint(engines[k].TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));maxEdge=Mathf.Max(maxEdge,Mathf.Abs(p.x),Mathf.Abs(p.z));
                    }
                    for(int i=0;i<4;i++)
                    {
                        var axle=Find(t.company,"Pilot"+i);var p=t.root.InverseTransformPoint(axle.position);float angle=Mathf.Atan2(p.x,p.z);float at=angle*RaidersMatch.Length/(2*Mathf.PI);var rail=TabletopRailway.Point(at);
                        maxPilotRadial=Mathf.Max(maxPilotRadial,Mathf.Abs(new Vector2(p.x,p.z).magnitude-TabletopViewContract.RailRadius));maxPilotVertical=Mathf.Max(maxPilotVertical,Mathf.Abs(p.y-.005f-TabletopViewContract.RailHeadHeight-rail.y));
                    }
                    for(int i=0;i<6;i++){m.cargo[i].owner=-2;m.cargo[i].order=i+1;m.cargo[i].distance=RaidersMatch.Wrap(d-(i+1)*RaidersMatch.Spacing);TabletopRailway.Place(t.wagons[i],m.cargo[i].distance,false,.016f);}
                    t.UpdateCouplings(m);
                    for(int i=0;i<6;i++){var line=t.wagons[i].GetComponentInChildren<LineRenderer>(true);maxLink=Mathf.Max(maxLink,Vector3.Distance(line.GetPosition(0),line.GetPosition(1)));}
                    for(int owner=0;owner<4;owner++){foreach(var c in m.cargo)c.owner=owner;t.UpdateCouplings(m);var link=t.wagons[0].GetComponentInChildren<LineRenderer>(true);maxLink=Mathf.Max(maxLink,Vector3.Distance(link.GetPosition(0),link.GetPosition(1)));}
                    // Conservative longitudinal clearance: engine full envelope, not just its shorter body.
                    float gap=Vector3.Distance(t.company.localPosition,t.wagons[0].localPosition)-TabletopViewContract.LocomotiveHalfLength-TabletopViewContract.WagonBodyLength*.5f;minBodyGap=Mathf.Min(minBodyGap,gap);
                }
                Check(maxEdge<TabletopViewContract.UsableLandSize*.5f-.02f,"All five oriented engine envelopes clear the land boundary through 256 samples");
                Check(maxPilotRadial<.00005f&&maxPilotVertical<.00005f,"Steam pilot axles follow track radius and railhead height through full loop");
                Check(maxPlayerRadial<.001f&&maxPlayerVertical<.001f,"Articulated player wheelsets stay within 1mm of rail center and height");
                Check(maxLink<.009f,"Coupling bridges remain below 9mm through curve and grade");
                Check(minBodyGap>.001f,"First freight body clears complete engine length throughout the curve");
                Check(TabletopRailway.WagonPitch-TabletopViewContract.WagonBodyLength>.006f,"Adjacent wagon body spacing retains at least 6mm clearance");
                log.AppendLine($"Sweep: maximum engine x/z envelope {maxEdge:F6}m; pilot radial/height errors {maxPilotRadial:F7}/{maxPilotVertical:F7}m; player axle errors {maxPlayerRadial:F7}/{maxPlayerVertical:F7}m; maximum coupling link {maxLink:F6}m; minimum engine/wagon envelope clearance {minBodyGap:F6}m.");
                // Reverse and stop use actual adapters, then moving/scaling the whole view must add no travel.
                var pMotion=t.locomotives[0].GetComponent<PlayerLocomotiveMotion>();double companyBefore=steam.TravelledMetres,playerBefore=pMotion.Distance;
                m.companyDistance=RaidersMatch.Wrap(m.companyDistance-.1f);m.players[0].distance=RaidersMatch.Wrap(m.players[0].distance-.1f);sf.Evaluate();followers[0].Evaluate();Check(steam.TravelledMetres<companyBefore&&pMotion.Distance<playerBefore,"Both actual adapters integrate signed reverse travel");
                companyBefore=steam.TravelledMetres;playerBefore=pMotion.Distance;t.presentation.localScale=Vector3.one*6;t.presentation.position+=new Vector3(1,.4f,-2);t.presentation.rotation=Quaternion.Euler(0,53,0);sf.Evaluate();followers[0].Evaluate();
                Check(Math.Abs(steam.TravelledMetres-companyBefore)<1e-6&&Math.Abs(pMotion.Distance-playerBefore)<1e-6,"View translation/yaw/zoom adds zero wheel travel while stopped");
                Check(!steam.GetComponentInChildren<Animator>(true).enabled&&!pMotion.Animator.enabled,"Rail-distance drivers exclusively own animation");
                t.presentation.localScale=Vector3.one;t.presentation.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var wheel=Find(t.locomotives[0],"Wheel_F0");var rest=wheel.localRotation;pMotion.AddTravelDistance(.3);Check(Quaternion.Angle(rest,wheel.localRotation)>20,"Player wheel rotates with distance");pMotion.AddTravelDistance(-.3);Check(Quaternion.Angle(rest,wheel.localRotation)<.05f,"Player reverse returns wheel phase");
                var rod=Find(t.company,"MainRod_L");steam.SetTravelDistance(0);var rodRest=rod.localPosition;steam.SetTravelDistance(.7);Check(Vector3.Distance(rodRest,rod.localPosition)>.01f,"Steam main rod continues its articulated motion");
                var smoke=t.company.GetComponent<CompanyLocomotiveSmoke>();smoke.ConfigureSimulationSpace();Check(smoke.chimneySmoke.main.customSimulationSpace==t.root&&smoke.chimneySmoke.main.scalingMode==ParticleSystemScalingMode.Hierarchy,"Smoke simulation and size inherit the common board branch");
                var snapshot=JsonUtility.ToJson(m);t.presentation.localScale=Vector3.one*3;Check(JsonUtility.ToJson(m)==snapshot,"Changing presentation leaves authoritative match snapshot byte-identical");
                Check(root.GetComponentsInChildren<OVRCameraRig>(true).Length==1&&root.GetComponentsInChildren<RaidersSession>(true).Length==1,"Saved launch retains exactly one rig and session");
                Check(root.GetComponentsInChildren<Transform>(true).All(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)==0),"Saved launch has no missing scripts");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            foreach(string path in AssetDatabase.FindAssets("t:Mesh",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);var v=mesh.vertices;var ix=mesh.triangles;bool valid=true;
                for(int i=0;i<ix.Length;i+=3)if(Vector3.Cross(v[ix[i+1]]-v[ix[i]],v[ix[i+2]]-v[ix[i]]).sqrMagnitude<1e-20f)valid=false;
                Check(valid&&mesh.uv.Length==mesh.vertexCount&&mesh.normals.Length==mesh.vertexCount,mesh.name+" nondegenerate triangles, normals and UVs");log.AppendLine(mesh.name+" triangles="+ix.Length/3+" vertices="+mesh.vertexCount);
            }
            log.AppendLine(count+" Task2 assertions passed. Edit Mode only; no headset, Play Mode, network or performance claim.");File.WriteAllText("Deliverables/DemeoRework/validation-task2.txt",log.ToString());Debug.Log(log.ToString());
        }
    }
}
