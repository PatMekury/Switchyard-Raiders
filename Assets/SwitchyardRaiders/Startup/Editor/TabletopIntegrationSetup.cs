using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using SwitchyardRaiders.Startup;
using SwitchyardRaiders.PlayerVehicles;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class TabletopIntegrationSetup
    {
        public const string Prefab="Assets/SwitchyardRaiders/Startup/Prefabs/SR_TabletopPresentation.prefab";
        private static Transform Child(Transform parent,string name)
        {
            var found=parent.Find(name);if(found)return found;
            var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;
        }
        private static T Get<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c?c:go.AddComponent<T>();}
        private static GameObject Asset(string path,Transform parent,string name)
        {
            var existing=parent.Find(name);if(existing)return existing.gameObject;
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new Exception("Missing authored asset: "+path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);go.name=name;return go;
        }
        [MenuItem("Switchyard Raiders/Tabletop/Integrate existing table and terrain")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !Application.dataPath.EndsWith("Switchyard Raiders/Assets"))throw new Exception("Switchyard Raiders in Edit Mode required.");
            var table=Object.FindAnyObjectByType<RaidersTable>(FindObjectsInactive.Include);
            var room=table.roomMenu;var session=table.session;var avatars=session.avatars;var launch=table.launch;
            var oldRoot=table.root;
            var presentation=Child(launch.transform,"Personal tabletop presentation");table.presentation=presentation;
            var bindings=Get<RaidersTabletopBindings>(presentation.gameObject);
            var visual=Asset("Assets/SwitchyardRaiders/PlayTable/Prefabs/SR_PlayTable.prefab",presentation,"Virtual table");
            visual.transform.localPosition=new Vector3(0,-TabletopViewContract.TerrainMountHeight,0);visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one;
            table.virtualTable=visual;
            var authoredMount=visual.transform.Find("TabletopPivot/TownMount");
            if(!authoredMount)throw new Exception("Authored terrain mounting anchor missing.");
            var mount=Child(presentation,"Terrain mounting surface");mount.localPosition=presentation.InverseTransformPoint(authoredMount.position);mount.localRotation=Quaternion.identity;table.terrainMount=mount;
            var terrain=Asset("Assets/SwitchyardRaiders/TerrainBase/Prefabs/SR_TerrainBase.prefab",mount,"Terrain and play-field assembly");
            terrain.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);terrain.transform.localScale=Vector3.one;table.terrainAssembly=terrain;
            oldRoot.SetParent(mount,false);oldRoot.name="Gameplay railway and controls";oldRoot.localPosition=new Vector3(0,TabletopViewContract.GameplayHeight,0);oldRoot.localRotation=Quaternion.identity;oldRoot.localScale=Vector3.one*TabletopViewContract.GameplayScale;
            var ground=oldRoot.Find("Town base");if(ground)ground.gameObject.SetActive(false);
            foreach(var label in oldRoot.GetComponentsInChildren<TextMesh>(true))if(label.name=="Emplacement number")label.characterSize=.014f;
            var oldSeat=oldRoot.Find("Remote player seat");if(oldSeat)oldSeat.gameObject.SetActive(false);
            var integration=Object.FindAnyObjectByType<PlayerVehicleTableIntegration>(FindObjectsInactive.Include);if(integration)integration.enabled=false;
            var trains=new Transform[4];
            for(int i=0;i<4;i++)
            {
                if(i<table.locomotives.Length && table.locomotives[i])table.locomotives[i].gameObject.SetActive(false);
                string path="Assets/SwitchyardRaiders/PlayerVehicles/Prefabs/"+(i%2==0?"Player01_Adler":"Player02_Falke")+".prefab";
                var train=Asset(path,oldRoot,"Player "+(i+1)+" authored locomotive");trains[i]=train.transform;
                trains[i].localScale=Vector3.one*(TabletopViewContract.PreservedTrainScale/TabletopViewContract.GameplayScale);trains[i].localPosition=RaidersMatch.Point((i%2)*16+(i/2)*2.2f);
                var follower=Get<PlayerVehicleTrackFollower>(train);follower.table=table;follower.playerSlot=i;
                var number=Child(train.transform,"Player number");number.localPosition=new Vector3(0,7,0);number.localRotation=Quaternion.Euler(90,0,0);
                var numberText=Get<TextMesh>(number.gameObject);numberText.text=(i+1).ToString();numberText.font=room.heading.font;numberText.fontSize=48;numberText.characterSize=1.2f;numberText.anchor=TextAnchor.MiddleCenter;numberText.color=i%2==0?table.playerOne.color:table.playerTwo.color;numberText.GetComponent<Renderer>().sharedMaterial=room.heading.font.material;
                train.SetActive(i<2);
            }
            table.locomotives=trains;
            if(table.company)table.company.gameObject.SetActive(false);
            var company=Asset("Assets/SwitchyardRaiders/CompanyLocomotive/Prefabs/CompanyLocomotive.prefab",oldRoot,"Company authored locomotive");
            company.transform.localScale=Vector3.one*(TabletopViewContract.PreservedTrainScale/TabletopViewContract.GameplayScale);company.transform.localPosition=RaidersMatch.Point(8);table.company=company.transform;
            Get<CompanyTableFollower>(company).table=table;company.SetActive(true);
            var seatsRoot=Child(launch.transform,"Human scale player seats");table.humanPresentation=seatsRoot;table.seats=new Transform[4];avatars.playerSeats=new Transform[4];avatars.playerFallbacks=new GameObject[4];
            for(int i=0;i<4;i++)
            {
                var seat=Child(seatsRoot,"Seat "+(i+1));seat.localPosition=TabletopViewMath.SeatPosition(i,4);var forward=-seat.localPosition;forward.y=0;seat.localRotation=Quaternion.LookRotation(forward);
                var marker=Child(seat,"Player identity - loading or simulated");marker.localPosition=new Vector3(0,1.05f,0);
                var label=Get<TextMesh>(marker.gameObject);label.text="P"+(i+1)+" - avatar loading / unavailable";label.font=room.heading.font;label.fontSize=48;label.characterSize=.045f;label.anchor=TextAnchor.MiddleCenter;
                label.GetComponent<Renderer>().sharedMaterial=room.heading.font.material;label.color=i%2==0?new Color(.25f,.9f,.6f):new Color(1,.5f,.3f);
                table.seats[i]=avatars.playerSeats[i]=seat;avatars.playerFallbacks[i]=marker.gameObject;marker.gameObject.SetActive(false);
            }
            avatars.headset=launch.headset;avatars.remoteSeat=table.seats[1];avatars.remoteFallback=avatars.playerFallbacks[1];
            var handles=Child(presentation,"Manipulation handles");table.manipulationHandles=handles.gameObject;
            for(int i=0;i<2;i++)
            {
                var handle=Child(handles,i==0?"Left two-hand grip":"Right two-hand grip");handle.localPosition=new Vector3((i==0?-1:1)*(TabletopViewContract.OuterSize+TabletopViewContract.UsableLandSize)*.25f,TabletopViewContract.RimHeight,-.30f);
                var line=Get<LineRenderer>(handle.gameObject);line.useWorldSpace=false;line.loop=true;line.positionCount=33;line.widthMultiplier=.006f;line.sharedMaterial=table.neutral;
                for(int j=0;j<33;j++){float a=j*Mathf.PI*2/32;line.SetPosition(j,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.045f);}
            }
            var preview=Child(launch.transform,"MR tabletop placement preview");var outline=Get<LineRenderer>(preview.gameObject);
            outline.useWorldSpace=false;outline.loop=true;outline.positionCount=4;outline.widthMultiplier=.008f;outline.sharedMaterial=table.playerOne;
            float half=TabletopViewContract.OuterSize*.5f;
            outline.SetPositions(new[]{new Vector3(-half,0,-half),new Vector3(half,0,-half),new Vector3(half,0,half),new Vector3(-half,0,half)});table.placementPreview=outline;preview.gameObject.SetActive(false);
            var manip=Get<RaidersTableManipulation>(table.gameObject);manip.table=table;manip.leftHand=launch.leftHand;manip.rightHand=launch.rightHand;table.manipulation=manip;
            var vignette=Get<OVRVignette>(launch.headset.gameObject);vignette.VignetteColor=Color.black;vignette.enabled=false;manip.vignette=vignette;
            session.table=table;room.table=table;
            TabletopHandGrabSetup.Configure(table);
            AvatarTask4Setup.Configure(table);
            var cancel=room.panelRoot.transform.Find("Cancel room change");
            if(cancel)Object.DestroyImmediate(cancel.gameObject);
            var cancelObject=Object.Instantiate(room.mainMenu.navigation.actions[5].gameObject,room.panelRoot.transform);cancelObject.name="Cancel room change";
            var menuAction=cancelObject.GetComponent<RaidersMenuAction>();if(menuAction)Object.DestroyImmediate(menuAction);
            var cancelButton=cancelObject.GetComponent<Button>();cancelButton.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(cancelButton.onClick,room.CancelModeChange);
            foreach(var caption in cancelObject.GetComponentsInChildren<Text>(true))caption.text="Keep current mode";
            var cancelRect=cancelObject.GetComponent<RectTransform>();cancelRect.anchoredPosition=new Vector2(0,-240);cancelRect.localScale=Vector3.one;
            room.cancelModeButton=cancelObject;cancelObject.SetActive(false);
            var so=new SerializedObject(session);so.FindProperty("developerTesting").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            bindings.virtualTable=visual;bindings.terrainAssembly=terrain;bindings.terrainMount=mount;bindings.gameplay=oldRoot;bindings.seats=Array.Empty<Transform>();bindings.playerLocomotives=trains;bindings.manipulationHandles=handles.gameObject;
            terrain.SetActive(false);oldRoot.gameObject.SetActive(false);handles.gameObject.SetActive(false);presentation.gameObject.SetActive(false);
            foreach(var obj in new Object[]{table,room,session,avatars,manip,bindings,room.mainMenu.navigation}){EditorUtility.SetDirty(obj);PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
            PrefabUtility.SaveAsPrefabAsset(presentation.gameObject,Prefab);
            EditorSceneManager.MarkSceneDirty(table.gameObject.scene);EditorSceneManager.SaveScene(table.gameObject.scene);
            PrefabUtility.SaveAsPrefabAsset(launch.gameObject,"Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab");AssetDatabase.SaveAssets();
            TabletopScaleTask2.Build();
            Debug.Log("Tabletop integration and calibrated railway saved. No Play Mode entered.");
        }
    }
}
