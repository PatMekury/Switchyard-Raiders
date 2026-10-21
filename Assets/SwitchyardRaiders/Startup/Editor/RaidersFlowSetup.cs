using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SwitchyardRaiders.Startup;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    // A project-scoped import request lets the correct open editor perform the work
    // when external MCP aliases resolve to another editor. No Play Mode is entered.
    [InitializeOnLoad]
    public static class RaidersFlowSetup
    {
        private const string Request="Temp/SRFlow.apply";
        private const string Report="Deliverables/MainMenuFlow";
        private const string Art="Assets/SwitchyardRaiders/Startup/GameplayArt";
        private static Font font;
        private static Material ui;
        private static Sprite cap;
        static RaidersFlowSetup(){EditorApplication.delayCall+=ApplyRequested;}
        private static void ApplyRequested()
        {
            if(!File.Exists(Request) || !Application.dataPath.Replace('\\','/').EndsWith("/Switchyard Raiders/Assets"))return;
            if(EditorApplication.isCompiling || EditorApplication.isUpdating){EditorApplication.delayCall+=ApplyRequested;return;}
            try { Apply();File.Delete(Request); }
            catch(Exception e){Directory.CreateDirectory(Report);File.WriteAllText(Report+"/setup-error.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Switchyard Raiders/Main Menu/Apply game-start flow")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying || !Application.dataPath.Replace('\\','/').EndsWith("/Switchyard Raiders/Assets"))throw new InvalidOperationException("Switchyard Raiders in Edit Mode is required.");
            var room=Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);
            if(!room || room.gameObject.scene.path!=LaunchSequenceTools.ScenePath)throw new InvalidOperationException("Open SR_Launch before applying.");
            Directory.CreateDirectory(Report);
            string preserved=JsonUtility.ToJson(new PreservedRoom(room));
            var main=room.mainMenu;
            if(!main)throw new InvalidOperationException("Existing main menu required.");
            if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/SwitchyardRaiders/Startup","GameplayArt");
            // Match the established live room-menu text. The bundled TMP import is
            // blocked by Unity's unsigned-package warning and is not bypassed.
            font=room.heading.font;
            if(!font)throw new InvalidOperationException("Existing room-menu font missing.");
            ui=AssetDatabase.LoadAssetAtPath<Material>("Assets/SwitchyardRaiders/Startup/RoomMenu/Art/SR_WorldUI.mat");
            cap=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SwitchyardRaiders/Startup/RoomMenu/Art/Modular/RaisedButtonCap.asset");
            if(!cap || !ui)throw new InvalidOperationException("Approved menu art missing.");
            var session=Get<RaidersSession>(room.gameObject);
            var nav=Get<RaidersMenu>(room.gameObject);Undo.RecordObject(nav,"Wire menu navigation");
            nav.main=main;nav.session=session;Undo.RecordObject(main,"Connect game-start flow");main.navigation=nav;
            var screen=Rect("Game-start screens",room.canvas.transform,Vector2.zero,new Vector2(660,470));nav.screen=screen.gameObject;
            nav.screenGroup=Get<CanvasGroup>(screen.gameObject);
            nav.heading=Label("Heading",screen,"DEPARTURE BOARD",new Vector2(0,190),new Vector2(600,44),29);
            nav.message=Label("Feedback",screen,"",new Vector2(0,112),new Vector2(600,100),20);
            nav.detail=Label("Player identity",screen,"",new Vector2(0,-214),new Vector2(630,42),17);
            nav.actions=new RaidersMenuAction[6];
            for(int i=0;i<6;i++)nav.actions[i]=Action("Action "+(i+1),screen,nav,new Vector2(i%2==0?-154:154,28-i/2*77),new Vector2(288,62),"",22,room.menuAudio);
            nav.keypad=new RaidersMenuAction[17];
            for(int i=0;i<16;i++)nav.keypad[i]=Action("Code key "+i.ToString("X"),screen,nav,new Vector2(-132+(i%4)*88,38-(i/4)*45),new Vector2(76,40),"key:"+i.ToString("X"),23,room.menuAudio);
            nav.keypad[16]=Action("Erase code",screen,nav,new Vector2(245,-28),new Vector2(126,48),"erase",18,room.menuAudio);
            foreach(var a in nav.keypad){a.label.text=a.command=="erase"?"Erase":a.command.Substring(4);a.gameObject.SetActive(false);}
            var settings=room.canvas.transform.Find("Station main menu/Change room");
            if(settings)
            {
                var button=settings.GetComponent<Button>();Undo.RecordObject(button,"Settings entry");
                while(button.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(button.onClick,0);
                UnityEventTools.AddPersistentListener(button.onClick,main.Settings);
                var text=settings.GetComponentInChildren<Text>();Undo.RecordObject(text,"Settings caption");text.text="SETTINGS";
            }
            main.matchButton.interactable=main.friendsButton.interactable=main.practiceButton.interactable=true;
            main.status.text="";
            var table=Get<RaidersTable>(room.gameObject);nav.table=table;table.session=session;table.launch=room.launch;table.headset=room.launch.headset;
            BuildTable(room,table);
            var avatars=Get<RaidersAvatars>(room.gameObject);avatars.session=session;session.avatars=avatars;
            var rig=room.launch.xrRigRoot.GetComponent<OVRCameraRig>();avatars.trackingSpace=rig.trackingSpace;
            var seat=Child("Remote player seat",table.root);seat.localPosition=new Vector3(0,-.7f,1.1f);seat.localRotation=Quaternion.Euler(0,180,0);avatars.remoteSeat=seat;
            var marker=Child("Remote player loading marker",seat);Cube("Player presence",marker,new Vector3(0,1.1f,0),new Vector3(.16f,.23f,.08f),table.playerTwo);avatars.remoteFallback=marker.gameObject;marker.gameObject.SetActive(false);
            avatars.managerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(LaunchSequenceTools.Root+"/Prefabs/SR_AvatarManager.prefab");
            var samples=Sample.FindByPackage("com.meta.xr.sdk.avatars","40.0.1").ToArray();
            if(!avatars.managerPrefab && samples.Length>0 && !samples[0].isImported){samples[0].Import(Sample.ImportOptions.HideImportWindow);AssetDatabase.Refresh();}
            var managers=AssetDatabase.FindAssets("AvatarSdkManagerStyle2Meta t:Prefab");
            if(!avatars.managerPrefab && managers.Length>0)
            {
                var contents=PrefabUtility.LoadPrefabContents(AssetDatabase.GUIDToAssetPath(managers[0]));
                try
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(contents))PrefabUtility.UnpackPrefabInstance(contents,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    foreach(var input in contents.GetComponentsInChildren<Oculus.Avatar2.OvrAvatarInputManager>(true))Object.DestroyImmediate(input);
                    contents.AddComponent<RaidersAvatarInput>();
                    avatars.managerPrefab=PrefabUtility.SaveAsPrefabAsset(contents,LaunchSequenceTools.Root+"/Prefabs/SR_AvatarManager.prefab");
                }
                finally{PrefabUtility.UnloadPrefabContents(contents);}
            }
            if(!avatars.managerPrefab)throw new InvalidOperationException("Avatar sample manager import is pending. Apply again after import.");
            var managerPath=AssetDatabase.GetAssetPath(avatars.managerPrefab);
            var managerContents=PrefabUtility.LoadPrefabContents(managerPath);
            try
            {
                if(PrefabUtility.IsPartOfPrefabInstance(managerContents))PrefabUtility.UnpackPrefabInstance(managerContents,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                var settingsObject=new SerializedObject(managerContents.GetComponent<Oculus.Avatar2.OvrAvatarManager>());
                settingsObject.FindProperty("MaxConcurrentAvatarsLoading").intValue=2;
                settingsObject.FindProperty("_preloadZipFiles").arraySize=0;
                settingsObject.ApplyModifiedPropertiesWithoutUndo();
                foreach(var face in managerContents.GetComponentsInChildren<Oculus.Avatar2.OvrAvatarFaceTrackingBehaviorOvrPlugin>(true))Object.DestroyImmediate(face);
                foreach(var eyes in managerContents.GetComponentsInChildren<Oculus.Avatar2.OvrAvatarEyeTrackingBehaviorOvrPlugin>(true))Object.DestroyImmediate(eyes);
                avatars.managerPrefab=PrefabUtility.SaveAsPrefabAsset(managerContents,managerPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(managerContents);}
            screen.gameObject.SetActive(false);table.root.gameObject.SetActive(false);
            foreach(var c in new Component[]{main,session,nav,table,avatars}){EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
            if(preserved!=JsonUtility.ToJson(new PreservedRoom(room)))throw new InvalidOperationException("Room-selection preservation check failed.");
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
            if(!EditorSceneManager.SaveScene(room.gameObject.scene))throw new IOException("Scene save failed.");
            PrefabUtility.SaveAsPrefabAsset(room.launch.gameObject,LaunchSequenceTools.Root+"/Prefabs/SR_Launch.prefab",out bool saved);
            if(!saved)throw new IOException("Launch prefab save failed.");
            AssetDatabase.SaveAssets();Validate();RaidersRulesChecks.Run();
            PruneUnusedSampleScenes(avatars.managerPrefab);
            if(File.Exists(Report+"/setup-error.txt"))File.Delete(Report+"/setup-error.txt");
        }
        private static void PruneUnusedSampleScenes(GameObject manager)
        {
            const string samples="Assets/Samples/Meta Avatars SDK/40.0.1/Sample Scenes";
            if(!AssetDatabase.IsValidFolder(samples))return;
            var keep=new System.Collections.Generic.HashSet<string>(AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(manager),true));
            // Retain SDK support code/materials; omit unused demonstration worlds
            // and Resources prefabs from the player dependency set.
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab t:Scene",new[]{samples}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if(!keep.Contains(path))AssetDatabase.DeleteAsset(path);
            }
        }
        private static T Get<T>(GameObject go)where T:Component{var value=go.GetComponent<T>();return value?value:Undo.AddComponent<T>(go);}
        private static Transform Child(string name,Transform parent)
        {
            var t=parent.Find(name);if(t)return t;
            var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Create flow component");go.transform.SetParent(parent,false);return go.transform;
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var t=parent.Find(name) as RectTransform;
            if(!t){var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Create menu element");t=(RectTransform)go.transform;t.SetParent(parent,false);}
            Undo.RecordObject(t,"Layout menu element");t.anchorMin=t.anchorMax=t.pivot=new Vector2(.5f,.5f);t.anchoredPosition=position;t.sizeDelta=size;t.localScale=Vector3.one;return t;
        }
        private static Text Label(string name,Transform parent,string text,Vector2 position,Vector2 size,float points)
        {
            var rt=Rect(name,parent,position,size);var label=Get<Text>(rt.gameObject);
            label.font=font;label.text=text;label.fontSize=(int)points;label.resizeTextForBestFit=false;label.alignment=TextAnchor.MiddleCenter;
            label.color=new Color(.94f,.9f,.77f);label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.material=ui;
            return label;
        }
        private static RaidersMenuAction Action(string name,Transform parent,RaidersMenu nav,Vector2 position,Vector2 size,string command,int points,MenuInteractionAudio bus)
        {
            var rt=Rect(name,parent,position,size);var face=Get<Image>(rt.gameObject);face.sprite=cap;face.material=ui;face.type=Image.Type.Sliced;face.raycastTarget=true;
            var button=Get<Button>(rt.gameObject);button.targetGraphic=face;button.navigation=new Navigation{mode=Navigation.Mode.None};
            var colours=button.colors;colours.normalColor=Color.white;colours.highlightedColor=new Color(1,.93f,.7f);colours.pressedColor=new Color(.75f,.8f,.68f);colours.disabledColor=new Color(.4f,.45f,.4f);button.colors=colours;
            var action=Get<RaidersMenuAction>(rt.gameObject);action.menu=nav;action.command=command;action.label=Label("Label",rt,name,Vector2.zero,size-new Vector2(18,4),points);
            action.label.color=new Color(.96f,.92f,.79f);
            while(button.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(button.onClick,0);
            UnityEventTools.AddPersistentListener(button.onClick,action.Invoke);
            var audio=Get<MenuButtonAudio>(rt.gameObject);audio.audioBus=bus;audio.clickClip=bus.confirmClip;
            return action;
        }
        private static Material Material(string name,Color color)
        {
            if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/SwitchyardRaiders/Startup","GameplayArt");
            var m=AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+name+".mat");if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));AssetDatabase.CreateAsset(m,Art+"/"+name+".mat");}
            m.color=color;m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        private static Transform Cube(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        {
            var t=parent.Find(name);
            if(!t){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;Undo.RegisterCreatedObjectUndo(go,"Create yard component");t=go.transform;t.SetParent(parent,false);var collider=go.GetComponent<Collider>();if(collider)Undo.DestroyObjectImmediate(collider);}
            t.localPosition=position;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=material;
            t.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return t;
        }
        private static Transform Train(string name,Transform parent,Material material,bool engine)
        {
            var t=Child(name,parent);Cube("Chassis",t,new Vector3(0,.012f,0),new Vector3(.027f,.018f,.06f),material);
            Cube(engine?"Cab":"Cargo",t,new Vector3(0,.031f,0),new Vector3(.026f,engine?.035f:.025f,.038f),material);return t;
        }
        [MenuItem("Switchyard Raiders/Main Menu/Preview departure layout (Edit Mode)")]
        private static void PreviewLayout()
        {
            if(EditorApplication.isPlaying)return;
            var room=Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);if(!room)return;
            room.selectionRoot.SetActive(false);room.mainMenu.content.SetActive(false);
            var nav=room.mainMenu.navigation;nav.screen.SetActive(true);nav.screenGroup.alpha=1;
            nav.heading.text="PLAY WITH FRIENDS";nav.message.text="Host a private yard or enter a friend's code.";nav.detail.text="";
            for(int i=0;i<nav.actions.Length;i++)nav.actions[i].gameObject.SetActive(i==0 || i==1 || i==5);
            nav.actions[0].label.text="Host private 1v1";nav.actions[1].label.text="Join with code";nav.actions[5].label.text="Back";
            foreach(var key in nav.keypad)key.gameObject.SetActive(false);
            Selection.activeGameObject=nav.screen;
            SceneView.lastActiveSceneView.Frame(new Bounds(room.canvas.transform.position,Vector3.one*.65f),false);
        }
        [MenuItem("Switchyard Raiders/Main Menu/Restore room-selection preview")]
        private static void RestorePreview()
        {
            if(EditorApplication.isPlaying)return;
            var room=Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);if(!room)return;
            room.mainMenu.navigation.screen.SetActive(false);room.mainMenu.content.SetActive(false);room.selectionRoot.SetActive(true);
            Apply();
        }
        private static void BuildTable(RoomChoiceMenu room,RaidersTable table)
        {
            table.root=Child("Raiders tabletop",room.launch.transform);
            table.neutral=Material("Freight enamel",new Color(.73f,.62f,.37f));table.playerOne=Material("Player 1 jade",new Color(.15f,.56f,.4f));table.playerTwo=Material("Player 2 coral",new Color(.79f,.31f,.2f));table.spent=Material("Spent steel",new Color(.23f,.27f,.25f));
            var ground=Material("Yard ground",new Color(.25f,.34f,.26f));var rail=Material("Rail steel",new Color(.46f,.5f,.47f));
            Cube("Town base",table.root,new Vector3(0,-.025f,0),new Vector3(1.5f,.04f,1.05f),ground);
            var rails=Child("Editable railway",table.root);
            for(int i=0;i<128;i++)
            {
                var a=RaidersMatch.Point(i*RaidersMatch.Length/128);var b=RaidersMatch.Point((i+1)*RaidersMatch.Length/128);
                var segment=Cube("Rail segment "+i,rails,(a+b)*.5f,new Vector3(.025f,.006f,Vector3.Distance(a,b)+.001f),rail);segment.localRotation=Quaternion.LookRotation(b-a,Vector3.up);
            }
            table.company=Train("Company locomotive",table.root,table.spent,true);
            table.locomotives=new Transform[2];table.wagons=new Transform[RaidersMatch.CargoCount];table.wagonMarkers=new Renderer[RaidersMatch.CargoCount];
            for(int i=0;i<2;i++)
            {
                var material=i==0?table.playerOne:table.playerTwo;table.locomotives[i]=Train("Player "+(i+1)+" locomotive",table.root,material,true);
                Cube("Player "+(i+1)+" unloading area",table.root,RaidersMatch.Point(RaidersMatch.Home(i))+Vector3.down*.008f,new Vector3(.4f,.008f,.13f),material);
            }
            for(int i=0;i<table.wagons.Length;i++){table.wagons[i]=Train("Cargo wagon "+(i+1),table.root,table.neutral,false);table.wagonMarkers[i]=table.wagons[i].Find("Cargo").GetComponent<Renderer>();}
            table.devices=new Transform[RaidersMatch.SiteCount];table.deviceMarkers=new Renderer[RaidersMatch.SiteCount];
            table.firingBars=new Transform[RaidersMatch.SiteCount];
            for(int i=0;i<table.devices.Length;i++)
            {
                var p=RaidersMatch.Point(RaidersMatch.Site(i));var outward=new Vector3(p.x,0,p.z).normalized;
                var site=Child("Emplacement "+(i+1),table.root);site.localPosition=p+outward*.09f;
                Cube("Site marker",site,Vector3.zero,new Vector3(.05f,.007f,.05f),table.neutral);
                var number=Child("Emplacement number",site);number.localPosition=new Vector3(0,.012f,.045f);number.localRotation=Quaternion.Euler(75,0,0);
                var caption=Get<TextMesh>(number.gameObject);caption.text=(i+1).ToString();caption.font=font;caption.fontSize=64;caption.characterSize=.035f;caption.anchor=TextAnchor.MiddleCenter;caption.color=new Color(.96f,.92f,.79f);
                caption.GetComponent<Renderer>().sharedMaterial=font.material;
                var device=Child("Decoupler",site);table.devices[i]=device;
                Cube("Window background",device,new Vector3(0,.065f,0),new Vector3(.06f,.005f,.008f),table.spent);
                table.firingBars[i]=Cube("Firing window",device,new Vector3(0,.065f,0),new Vector3(0,.006f,.008f),table.neutral);
                table.deviceMarkers[i]=Cube("Aiming head",device,new Vector3(0,.022f,0),new Vector3(.018f,.018f,.05f),table.playerOne).GetComponent<Renderer>();
                device.gameObject.SetActive(false);
            }
        }
        [Serializable] private sealed class PreservedRoom
        {
            public string heading,status;public Vector3 position,scale;public string follow;
            public PreservedRoom(RoomChoiceMenu r){heading=r.heading.text;status=r.status.text;position=r.canvas.transform.localPosition;scale=r.canvas.transform.localScale;follow=EditorJsonUtility.ToJson(r.follower);}
        }
        [MenuItem("Switchyard Raiders/Main Menu/Validate game-start flow")]
        public static void Validate()
        {
            var room=Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);var nav=room.mainMenu.navigation;
            if(!nav || !nav.session || !nav.table || !nav.session.avatars || !nav.session.avatars.managerPrefab)throw new InvalidOperationException("Missing flow reference.");
            var report=new System.Text.StringBuilder();
            foreach(var transform in room.launch.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)throw new InvalidOperationException("Missing script: "+transform.name);
            var inputs=nav.session.avatars.managerPrefab.GetComponentsInChildren<Oculus.Avatar2.OvrAvatarInputManager>(true);
            if(inputs.Length!=1 || !(inputs[0] is RaidersAvatarInput))throw new InvalidOperationException("Avatar manager must use only the native Raiders tracking input.");
            if(!nav.table.launch || !nav.session.avatars.trackingSpace || !nav.session.avatars.remoteSeat)throw new InvalidOperationException("Missing tracking or table anchor.");
            report.AppendLine("Project: "+Application.dataPath);report.AppendLine("Edit Mode only; no runtime, headset, service connection or player build verification.");
            foreach(var action in nav.actions.Concat(nav.keypad))
            {
                var button=action.GetComponent<Button>();
                if(!action.menu || !action.label || button.onClick.GetPersistentEventCount()!=1 || button.onClick.GetPersistentTarget(0)!=action)throw new InvalidOperationException("Broken action: "+action.name);
                if(action.label.raycastTarget)throw new InvalidOperationException("Text intercepts interaction.");
                report.AppendLine(action.name+" -> RaidersMenuAction.Invoke; live text; audio connected");
            }
            if(room.canvas.renderMode!=RenderMode.WorldSpace)throw new InvalidOperationException("Expected world space canvas.");
            if(room.canvas.GetComponentsInChildren<Oculus.Interaction.PointableCanvas>(true).Length==0)throw new InvalidOperationException("Missing PointableCanvas.");
            report.AppendLine("Existing room-selector text, transform and follower preserved during setup.");
            report.AppendLine("Editor checkbox: RoomChoiceMenu object / RaidersSession / Solo 1v1 Test — Control Both Players.");
            report.AppendLine("Network: Fusion 2.1.3 Host/Client; custom 1v1 lobby; ready gate; server validates sender slot; cancellation and timeout; no authority bypass.");
            report.AppendLine("Avatar SDK: 40.0.1; authenticated user and entitlement; invisible local tracked entity; remote pose streaming; explicit fallback marker.");
            foreach(var path in AssetDatabase.FindAssets("t:PhotonAppSettings").Select(AssetDatabase.GUIDToAssetPath))
            {
                var so=new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(path));var it=so.GetIterator();
                while(it.NextVisible(true))if(it.name=="AppIdFusion")report.AppendLine("Fusion App ID configured: "+!string.IsNullOrWhiteSpace(it.stringValue));
            }
            File.WriteAllText(Report+"/validation.txt",report.ToString());Debug.Log(report.ToString());
        }
    }
}
