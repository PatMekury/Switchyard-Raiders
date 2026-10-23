using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using Meta.XR.MRUtilityKit;
using SwitchyardRaiders.Startup;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class TabletopIntegrationChecks
    {
        private static int count;
        private static readonly StringBuilder report=new();
        private static void Check(bool condition,string message){if(!condition)throw new Exception(message);count++;report.AppendLine("PASS "+message);}
        private static void Field(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
        private static void Tick(RaidersSession session)=>typeof(RaidersSession).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session,null);
        [MenuItem("Switchyard Raiders/Tabletop/Run Edit Mode integration checks")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
            count=0;report.Clear();RaidersRulesChecks.Run();
            var m=new RaidersMatch();m.Reset(false,4);
            Check(m.Target==6 && m.playerCount==4,"Four players, six team deliveries");
            Check(m.HomeFor(0)==m.HomeFor(2) && m.HomeFor(1)==m.HomeFor(3),"Teammates share homes");
            Check(m.players.Distinct().Count()==4,"Four independent train states");
            Check(m.cargo.All(x=>x.owner==-4),"No full cargo chain appears at reset");
            m.Step(.02f);Check(m.cargo.Count(x=>x.owner==-2)==1,"First freight arrives alone");
            for(int i=0;i<100;i++)m.Step(.02f);Check(m.cargo.Count(x=>x.owner==-2)==1,"Freight waits for its arrival interval");
            m.elapsed=20;m.Step(.02f);Check(m.cargo.Count(x=>x.owner==-2)==2,"Freight arrives gradually");
            m.cargo[0].owner=-3;m.elapsed=40;m.Step(.02f);Check(m.cargo[0].owner==-2,"Delivered freight slot replenishes");
            for(int i=0;i<4;i++)Check(m.Apply(i,RaidersAction.Throttle,0,.5f,out _),"Slot "+i+" can drive");
            var positions=m.players.Select(x=>x.distance).ToArray();m.Step(.02f);
            Check(Enumerable.Range(0,4).All(i=>positions[i]!=m.players[i].distance),"All four trains advance in the same simulation tick");
            Check(m.Apply(2,RaidersAction.Place,0,0,out _),"P3 owns a decoupler");
            Check(!m.Apply(0,RaidersAction.Fire,0,0,out _),"Teammate cannot fire another player's device");
            m.Reset(false,4);m.nextFreight=999;
            for(int i=0;i<3;i++){m.cargo[i].owner=0;m.cargo[i].order=i+1;}
            m.players[0].distance=1;m.Step(.02f);
            Check(m.teamScores[0]==3 && m.winner==-1,"Three team deliveries do not win 2v2");
            for(int i=3;i<6;i++){m.cargo[i].owner=2;m.cargo[i].order=i-2;}
            m.players[2].distance=1;m.Step(.02f);
            Check(m.teamScores[0]==6 && m.winner==0 && m.players[0].score==3 && m.players[2].score==3,"Teammate deliveries combine to win at six");
            m.Reset(false,4);Check(m.teamScores.All(x=>x==0) && m.winner==-1 && m.cargo.All(x=>x.owner==-4),"Rematch resets scores and gradual supply");
            var encoded=JsonUtility.ToJson(m);var decoded=JsonUtility.FromJson<RaidersMatch>(encoded);
            Check(decoded.players.Length==4 && decoded.Target==6 && decoded.teamScores.Length==2,"Four-player snapshot serialization round trip");
            var go=new GameObject("Edit Mode local session fixture");go.SetActive(false);
            try
            {
                var session=go.AddComponent<RaidersSession>();Field(session,"developerTesting",true);session.SelectPlayerCount(4);session.StartEditorTest();
                Check(session.Phase==RaidersSessionPhase.Ready && session.PlayerCount==4,"Developer route creates four simulated slots");
                Check(session.IsLocal && session.SlotLabel(3).Contains("SIMULATED"),"Test slots are explicitly simulated");
                session.ToggleReady();Check(session.Phase==RaidersSessionPhase.Countdown && Enumerable.Range(0,4).All(session.IsReady),"One tester can ready all slots");
                Field(session,"countdownEnd",Time.unscaledTime-1);Tick(session);Check(session.Phase==RaidersSessionPhase.Playing,"Countdown activates match");
                for(int i=0;i<4;i++){session.Command(RaidersAction.Throttle,0,.7f);session.SwitchPlayer();}
                Check(session.ControlledSlot==0 && session.Match.players.All(x=>x.throttle==.7f),"Switching controls preserves commands for every slot");
                var before=session.Match.players.Select(x=>x.distance).ToArray();Tick(session);
                Check(Enumerable.Range(0,4).All(i=>before[i]!=session.Match.players[i].distance),"Slot switching preserves simultaneous simulation");
                session.Match.winner=0;Tick(session);Check(session.Phase==RaidersSessionPhase.Finished,"Winner activates results phase");
                session.Rematch();Check(session.Phase==RaidersSessionPhase.Ready && session.Match.winner==-1,"Local rematch returns through readiness");
                session.Leave();Check(session.Phase==RaidersSessionPhase.Idle && session.Match==null && session.PlayerCount==0,"Leave removes local session state");
                session.SelectPlayerCount(2);session.StartEditorTest();Check(session.PlayerCount==2,"Two-slot retry starts cleanly");session.Leave();
                Field(session,"developerTesting",false);session.StartEditorTest();Check(session.Phase==RaidersSessionPhase.Idle,"Disabled developer checkbox blocks test route");
            }
            finally{Object.DestroyImmediate(go);}
            var left=new Vector3(-.2f,1,0);var right=new Vector3(.2f,1,0);var initial=new Vector3(0,.75f,1);
            TabletopViewMath.Solve(initial,Quaternion.identity,1,left,right,left,right,.2f,12,out var pos,out var rot,out var scale);
            Check(Vector3.Distance(pos,initial)<.00001f && scale==1,"Two-hand acquisition does not jump");
            var move=new Vector3(.3f,.1f,.2f);
            TabletopViewMath.Solve(initial,Quaternion.identity,1,left,right,left+move,right+move,.2f,12,out pos,out rot,out scale);
            Check(Vector3.Distance(pos,initial+move)<.00001f,"Moving both hands translates presentation");
            var midpoint=(left+right)/2;
            TabletopViewMath.Solve(initial,Quaternion.identity,1,left,right,midpoint+(left-midpoint)*2,midpoint+(right-midpoint)*2,.2f,12,out pos,out rot,out scale);
            Check(scale==2 && Vector3.Distance(pos,midpoint+(initial-midpoint)*2)<.00001f,"Scale stays centered on hand midpoint");
            var yaw=Quaternion.Euler(0,90,0);
            TabletopViewMath.Solve(initial,Quaternion.identity,1,left,right,midpoint+yaw*(left-midpoint),midpoint+yaw*(right-midpoint),.2f,12,out pos,out rot,out scale);
            Check(Quaternion.Angle(rot,yaw)<.01f && Vector3.Angle(rot*Vector3.up,Vector3.up)<.01f,"Rotation is horizontal with tilt locked");
            TabletopViewMath.Solve(initial,Quaternion.identity,1,left,right,left*100,right*100,.2f,12,out pos,out rot,out scale);Check(scale==12,"Exploration scale cap is enforced");
            Check(Enumerable.Range(0,4).Select(i=>TabletopViewMath.SeatPosition(i,4)).Distinct().Count()==4,"Four seats do not overlap");
            Check(TabletopViewMath.SeatPosition(0,2).z==-TabletopViewMath.SeatPosition(1,2).z,"Two seats face opposite sides");
            var surface=new GameObject("Synthetic tabletop footprint fixture");surface.SetActive(false);
            try
            {
                var anchor=surface.AddComponent<MRUKAnchor>();typeof(MRUKAnchor).GetProperty("VolumeBounds").SetValue(anchor,(Bounds?)new Bounds(new Vector3(0,.375f,0),new Vector3(1.6f,.75f,1.2f)));
                Check(RaidersRoom.FitsAnchor(anchor,new Vector3(0,.756f,0),Quaternion.identity,1),"Terrain fits a tabletop with edge clearance");
                Check(!RaidersRoom.FitsAnchor(anchor,new Vector3(.2f,.756f,0),Quaternion.identity,1),"Overhanging terrain rejected");
                Check(!RaidersRoom.FitsAnchor(anchor,new Vector3(0,1,0),Quaternion.identity,1),"Floating placement rejected");
                Check(!RaidersRoom.FitsAnchor(anchor,new Vector3(0,.756f,0),Quaternion.Euler(0,90,0),1),"Rotated footprint exceeding table rejected");
                Check(RaidersRoom.FitsAnchor(anchor,new Vector3(0,.756f,0),Quaternion.Euler(0,90,0),.6f),"Reduced rotated footprint fits");
                Check(!RaidersRoom.FitsAnchor(anchor,new Vector3(0,.756f,0),Quaternion.identity,2),"Anchored state rejects exploration scale");
            }
            finally{Object.DestroyImmediate(surface);}
            var viewFixture=new GameObject("Edit Mode view lifecycle fixture");viewFixture.SetActive(false);
            try
            {
                Transform Node(string name){var child=new GameObject(name);child.transform.SetParent(viewFixture.transform,false);return child.transform;}
                var view=viewFixture.AddComponent<RaidersTable>();var mode=viewFixture.AddComponent<RoomChoiceMenu>();var launch=viewFixture.AddComponent<LaunchSequence>();var session=viewFixture.AddComponent<RaidersSession>();
                Field(mode,"<IsVirtual>k__BackingField",true);Field(mode,"<HasSelection>k__BackingField",true);
                launch.headset=Node("Head");launch.headset.position=new Vector3(0,1.3f,0);view.launch=launch;view.roomMenu=mode;view.session=session;
                view.presentation=Node("Presentation");view.root=Node("Gameplay");view.virtualTable=Node("Virtual table").gameObject;view.terrainAssembly=Node("Terrain").gameObject;
                view.ModeChosen();Check(view.virtualTable.activeSelf && !view.terrainAssembly.activeSelf && !view.CanManipulate,"VR menu shows table with terrain and manipulation off");
                Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Playing);Check(view.Show() && view.terrainAssembly.activeSelf && view.CanManipulate,"Match start activates terrain and manipulation eligibility");
                var normal=view.presentation.position;view.presentation.position+=Vector3.right;view.presentation.localScale=Vector3.one*3;view.PrepareModeChange();view.ModeChosen();
                Check(view.presentation.localScale.x==3 && Vector3.Distance(view.presentation.position,normal+Vector3.right)<.001f,"Returning to VR restores personal placement and scale");
                view.ResetView();Check(view.presentation.localScale.x==1 && Vector3.Distance(view.presentation.position,normal)<.001f,"Reset restores normal tabletop after a mode round trip");
                Check(launch.headset.position==new Vector3(0,1.3f,0),"View changes leave tracked head transform untouched");
            }
            finally{Object.DestroyImmediate(viewFixture);}
            var table=Object.FindAnyObjectByType<RaidersTable>(FindObjectsInactive.Include);
            Check(table && table.presentation && table.terrainMount && table.terrainAssembly && table.virtualTable,"Launch scene binds real table and terrain");
            Check(table.root.parent==table.terrainMount && table.root.localPosition.y>=.15f,"Gameplay sits above terrain top");
            Check(table.terrainAssembly.transform.parent==table.terrainMount && table.terrainAssembly.transform.localPosition==Vector3.zero,"Terrain bottom sits on mounting surface");
            Check(!table.root.gameObject.activeSelf && !table.terrainAssembly.activeSelf,"Terrain and gameplay start inactive");
            Check(table.locomotives.Length==4 && table.seats.Length==4,"Four trains and seat references wired");
            Check(table.manipulation.leftHand && table.manipulation.rightHand && table.manipulation.vignette,"Tracked hand input and vignette wired");
            Check(table.session.table==table && table.roomMenu.table==table && table.session.avatars.playerSeats.Length==4,"Session, mode and avatar wiring agree");
            Check(!new SerializedObject(table.session).FindProperty("developerTesting").boolValue,"Developer testing defaults off");
            Check(Object.FindObjectsByType<RaidersSession>(FindObjectsInactive.Include).Length==1,"Exactly one session owner");
            Check(Object.FindObjectsByType<OVRCameraRig>(FindObjectsInactive.Include).Length==1,"Exactly one XR rig");
            Check(table.roomMenu.cancelModeButton && table.placementPreview,"MR confirmation and cancellation presentation wired");
            Check(table.session.avatars.GetType().GetField("buffer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(table.session.avatars)!=null,"Avatar streaming buffer remains non-null");
            report.AppendLine(count+" integration assertions plus original rules checks passed. Android is the active compilation target.");
            report.AppendLine("Edit Mode only: no Play Mode, device launch, network authentication, remote streaming, voice, native room query, or performance measurement.");
            Directory.CreateDirectory("Deliverables/TabletopIntegration");File.WriteAllText("Deliverables/TabletopIntegration/validation.txt",report.ToString());Debug.Log(report.ToString());
        }
    }
}
