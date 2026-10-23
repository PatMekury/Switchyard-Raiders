using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class TabletopContactGrabChecks
    {
        private sealed class Points : IGrabbable
        {
            public List<Pose> GrabPoints { get; } = new();
            public Transform Transform { get; set; }
        }
        private static void Field(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
        [MenuItem("Switchyard Raiders/Tabletop/Check contact grabbing and recentering")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
            int count=0;var report=new StringBuilder();
            void Check(bool condition,string text){if(!condition)throw new Exception(text);count++;report.AppendLine("PASS "+text);}
            var fixture=new GameObject("Contact grab checks");fixture.SetActive(false);
            try
            {
                Transform Node(string name){var child=new GameObject(name);child.transform.SetParent(fixture.transform,false);return child.transform;}
                var table=fixture.AddComponent<RaidersTable>();var room=fixture.AddComponent<RoomChoiceMenu>();var session=fixture.AddComponent<RaidersSession>();
                var launch=fixture.AddComponent<LaunchSequence>();var manip=fixture.AddComponent<RaidersTableManipulation>();
                table.roomMenu=room;table.session=session;table.launch=launch;table.manipulation=manip;manip.table=table;
                launch.headset=Node("Head");launch.headset.position=new Vector3(2,1.6f,-3);launch.headset.rotation=Quaternion.Euler(15,70,0);
                table.presentation=Node("View");table.root=Node("Railway");table.virtualTable=Node("Table").gameObject;table.terrainAssembly=Node("Terrain").gameObject;
                Field(room,"<IsVirtual>k__BackingField",true);Field(room,"<HasSelection>k__BackingField",true);
                table.ModeChosen();
                var forward=Vector3.ProjectOnPlane(launch.headset.forward,Vector3.up).normalized;
                var offset=table.presentation.position-launch.headset.position;
                Check(Vector3.Distance(Vector3.ProjectOnPlane(offset,Vector3.up),forward)<.0001f,"VR table centered 1m ahead of current head yaw");
                Check(Mathf.Abs(offset.y+.5f)<.0001f,"Tabletop placed 0.5m below eyes");
                Check(Mathf.Abs(Vector3.Dot(offset,forward)-.7f-.3f)<.0001f,"Near rim within 30cm horizontal reach");
                launch.headset.rotation=Quaternion.Euler(0,-35,0);
                Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Playing);table.Show();
                var home=TabletopViewMath.ReachablePose(launch.headset);
                Check(Vector3.Distance(table.presentation.position,home.position)<.0001f,"Match start recenters after player turns during menus");
                var points=new Points{Transform=table.presentation};manip.Initialize(points);
                var left=home.position+new Vector3(-.2f,0,-.7f);var right=left+Vector3.right*.4f;
                points.GrabPoints.Add(new Pose(left,Quaternion.identity));manip.BeginTransform();
                points.GrabPoints[0]=new Pose(left+Vector3.right,Quaternion.identity);manip.UpdateTransform();
                Check(!manip.Dragging && Vector3.Distance(table.presentation.position,home.position)<.0001f,"One grabbed hand cannot move the table");
                Check(manip.BlocksGameplay,"A held table contact blocks gameplay commands");
                points.GrabPoints[0]=new Pose(left,Quaternion.identity);points.GrabPoints.Add(new Pose(right,Quaternion.identity));manip.BeginTransform();manip.UpdateTransform();
                Check(manip.Dragging && Vector3.Distance(table.presentation.position,home.position)<.0001f,"Second grab starts without a jump");
                var delta=new Vector3(.15f,.03f,.2f);
                points.GrabPoints[0]=new Pose(left+delta,Quaternion.identity);points.GrabPoints[1]=new Pose(right+delta,Quaternion.identity);manip.UpdateTransform();
                Check(Vector3.Distance(table.presentation.position,home.position+delta)<.0001f,"Two palm contacts translate the personal table");
                var mid=(left+right)*.5f;var yaw=Quaternion.Euler(0,35,0);
                points.GrabPoints[0]=new Pose(mid+yaw*(left-mid)*1.5f,Quaternion.identity);points.GrabPoints[1]=new Pose(mid+yaw*(right-mid)*1.5f,Quaternion.identity);manip.UpdateTransform();
                Check(Mathf.Abs(table.presentation.localScale.x-1.5f)<.0001f,"Spreading the two contacts enlarges the table");
                Check(Quaternion.Angle(table.presentation.rotation,yaw*home.rotation)<.01f,"Turning the two contacts rotates horizontally");
                Check(Vector3.Angle(table.presentation.up,Vector3.up)<.01f,"Table tilt remains locked");
                var held=table.presentation.position;points.GrabPoints.RemoveAt(1);manip.EndTransform();manip.BeginTransform();
                points.GrabPoints[0]=new Pose(left+Vector3.up,Quaternion.identity);manip.UpdateTransform();
                Check(!manip.Dragging && Vector3.Distance(table.presentation.position,held)<.0001f,"Releasing either hand freezes the table");
                points.GrabPoints.Add(new Pose(right,Quaternion.identity));manip.BeginTransform();manip.UpdateTransform();
                Check(manip.Dragging && Vector3.Distance(table.presentation.position,held)<.0001f,"Regrabbing the second hand rebases without a jump");
                manip.Suspend();points.GrabPoints[0]=new Pose(left*2,Quaternion.identity);manip.UpdateTransform();
                Check(!manip.Dragging && Vector3.Distance(table.presentation.position,held)<.0001f,"Suspending a grab prevents stale input from moving the table");
                var headPosition=launch.headset.position;launch.headset.rotation=Quaternion.Euler(0,140,0);table.ResetView();home=TabletopViewMath.ReachablePose(launch.headset);
                Check(table.presentation.localScale.x==1 && Vector3.Distance(table.presentation.position,home.position)<.0001f,"Recenter uses current view and restores normal scale");
                Check(launch.headset.position==headPosition,"Manipulation never moves the tracked head");
                Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Ready);manip.BeginTransform();
                Check(!manip.Dragging,"Table cannot move before gameplay");
            }
            finally{Object.DestroyImmediate(fixture);}
            var live=Object.FindAnyObjectByType<RaidersTable>(FindObjectsInactive.Include);var input=live.manipulation;
            Check(input.handGrab && input.grabbable,"Scene has a real ISDK contact interactable and grabbable");
            Check(input.handGrab.SupportedGrabTypes==GrabTypeFlags.Palm,"Table accepts palm closure rather than pinch grabs");
            Check(input.handGrab.MaxSelectingInteractors==2 && input.grabbable.MaxGrabPoints==2 && !input.handGrab.ResetGrabOnGrabsUpdated,"Both hands can hold the same table simultaneously");
            Check(input.grabEdges.Length==4 && input.grabEdges.All(x=>x && x.isTrigger),"All four table edges have contact colliders");
            Check(input.handGrab.transform.localScale==Vector3.one && input.handGrab.Rigidbody.isKinematic,"Grab body stays unscaled and cannot fall or be thrown");
            Check(live.launch.xrRigRoot.GetComponentsInChildren<HandGrabInteractor>(true).Length==2,"Existing rig has exactly one contact grab interactor per hand");
            var serialized=new SerializedObject(input.grabbable);
            Check(serialized.FindProperty("_oneGrabTransformer").objectReferenceValue==input && serialized.FindProperty("_twoGrabTransformer").objectReferenceValue==input,"Both ISDK grab paths use the two-hand-only table transformer");
            Check(!serialized.FindProperty("_throwWhenUnselected").boolValue,"Release never throws the table");
            report.AppendLine(count+" contact grab and placement assertions passed in Edit Mode. Live palm recognition still requires a two-hand runtime test.");
            Directory.CreateDirectory("Deliverables/TabletopIntegration");File.WriteAllText("Deliverables/TabletopIntegration/contact-grab-checks.txt",report.ToString());Debug.Log(report.ToString());
        }
    }
}
