using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using SwitchyardRaiders.Startup;
using Object = UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class ModularRoomMenuSetup
    {
        public const string Art = RoomMenuSetup.Root + "/Art/Modular";
        public const string Report = "Deliverables/RoomSelection_Modular_v2";
        private const string Atlas = Art + "/RoomMenuAtlas.png";
        private static readonly Color Ivory = new(.97f, .92f, .78f);
        private static Material ui;

        [MenuItem("Switchyard Raiders/Room Menu/Apply modular design")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Apply in idle Edit Mode.");
            if (!Application.dataPath.Replace('\\','/').EndsWith("/Switchyard Raiders/Assets")) throw new InvalidOperationException("Wrong project.");
            var launch = Object.FindAnyObjectByType<LaunchSequence>(FindObjectsInactive.Include);
            var menu = Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);
            if (!launch || !menu || launch.gameObject.scene.path != LaunchSequenceTools.ScenePath) throw new InvalidOperationException("Open SR_Launch.");
            Directory.CreateDirectory(Report);
            if (!File.Exists(Report + "/SR_Launch-before-modular.unity"))
            {
                File.Copy(LaunchSequenceTools.ScenePath, Report + "/SR_Launch-before-modular.unity");
                File.Copy(LaunchSequenceTools.Root + "/Prefabs/SR_Launch.prefab", Report + "/SR_Launch-before-modular.prefab");
            }
            Vector3 scale = menu.canvas.transform.localScale;
            float distance = menu.follower.distance;
            ApplyVisuals(menu);
            if (scale != menu.canvas.transform.localScale || distance != menu.follower.distance) throw new InvalidOperationException("Placement changed unexpectedly.");
            foreach (var c in menu.GetComponentsInChildren<Component>(true))
                if (c) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            EditorUtility.SetDirty(menu);
            bool saved;
            PrefabUtility.SaveAsPrefabAsset(launch.gameObject, LaunchSequenceTools.Root + "/Prefabs/SR_Launch.prefab", out saved);
            if (!saved) throw new InvalidOperationException("Prefab save failed.");
            EditorSceneManager.MarkSceneDirty(launch.gameObject.scene);
            if (!EditorSceneManager.SaveScene(launch.gameObject.scene)) throw new InvalidOperationException("Scene save failed.");
            AssetDatabase.SaveAssets();
            Validate();
        }

        public static void ApplyVisuals(RoomChoiceMenu menu)
        {
            ImportArt();
            ui = AssetDatabase.LoadAssetAtPath<Material>(RoomMenuSetup.Root + "/Art/SR_WorldUI.mat");
            if (!ui || !menu.realButton || !menu.virtualButton) throw new InvalidOperationException("Expected existing XR UI and room buttons.");
            Undo.RecordObject(menu, "Apply modular room menu");
            var root = Rect("Modular room pieces", menu.canvas.transform, Vector2.zero, new Vector2(660,470));
            var rail = Picture("Sculpted railway ribbon", root, new Vector2(0,54), new Vector2(620,266),
                Sprite("Rails", 8,8,1536,660,Vector4.zero));
            rail.material = ui;
            rail.transform.SetAsFirstSibling();

            var existingSign = menu.heading.transform.parent as RectTransform;
            bool keepHeading = existingSign && existingSign.name == "Enamel heading";
            string headingText = keepHeading ? menu.heading.text : "CHOOSE YOUR ROOM";
            int headingFont = keepHeading ? menu.heading.fontSize : 27;
            Vector2 headingPosition = keepHeading ? menu.heading.rectTransform.anchoredPosition : new Vector2(0,-10);
            Vector2 headingSize = keepHeading ? menu.heading.rectTransform.rect.size : new Vector2(298,46);
            var title = Picture("Enamel heading", root, keepHeading ? existingSign.anchoredPosition : new Vector2(-30,164), keepHeading ? existingSign.rect.size : new Vector2(352,96),
                MechanicalSprite("StationHeading",0,1024,288));
            title.type = UnityEngine.UI.Image.Type.Simple;
            title.rectTransform.localRotation = Quaternion.Euler(0,0,3);
            MoveText(menu.heading, title.transform, headingText, headingPosition, headingSize, headingFont, menu.heading.fontStyle);
            menu.heading.color = new Color(.07f,.18f,.14f);

            var real = Choice(menu, root, false);
            var vr = Choice(menu, root, true);
            MoveText(menu.status, root, "Touch a button or point and pinch", new Vector2(0,-197), new Vector2(575,38), 16, FontStyle.Normal);
            menu.status.color = Ivory;
            // The previous layout is backed up outside Assets. Remove only its now-empty visual frame.
            var old = menu.canvas.transform.Find("Destination board");
            if (old && old.childCount == 0) Undo.DestroyObjectImmediate(old.gameObject);
            else if (old) old.gameObject.SetActive(false);

            var motion = menu.GetComponent<ModularRoomMenuMotion>();
            if (!motion) motion = Undo.AddComponent<ModularRoomMenuMotion>(menu.gameObject);
            motion.realChoice = Group(real); motion.virtualChoice = Group(vr);
            motion.realRoomImage = menu.realButton.GetComponent<RoomSelectorFeedback>().replaceableIcon;
            motion.virtualRoomImage = menu.virtualButton.GetComponent<RoomSelectorFeedback>().replaceableIcon;
            motion.pieces = new[] { Piece(title.rectTransform,0), Piece(rail.rectTransform,.05f), Piece(real,.10f), Piece(vr,.17f), Piece(menu.status.rectTransform,.22f) };
            motion.enabled = false; menu.motion = motion;
            menu.selectionRoot = root.gameObject;
            MainMenu(menu);
            // Static editor view; runtime Awake hides the menu until the opening finishes.
            menu.group.alpha = 1; menu.group.interactable = menu.group.blocksRaycasts = true;
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.material || graphic.material.shader.name != "Switchyard Raiders/XR/Room Menu Art") graphic.material = ui;
                if (graphic.GetComponent<UnityEngine.UI.Button>() == null) graphic.raycastTarget = false;
            }
            EditorUtility.SetDirty(motion);
        }

        private static RectTransform Choice(RoomChoiceMenu menu, Transform parent, bool virtualChoice)
        {
            var button = virtualChoice ? menu.virtualButton : menu.realButton;
            var group = Rect(virtualChoice ? "Virtual room pieces" : "Your room pieces",parent,
                new Vector2(virtualChoice ? 145 : -145, -36),new Vector2(278,240));
            Undo.RecordObject(button.transform,"Position room button");
            button.transform.SetParent(group,false);
            Place((RectTransform)button.transform,new Vector2(0,-78),new Vector2(258,74));
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation {mode=Navigation.Mode.None};
            var hit=button.GetComponent<UnityEngine.UI.Image>(); hit.color=Color.clear; hit.raycastTarget=true; hit.material=ui;
            button.targetGraphic=hit;
            var feedback=button.GetComponent<RoomSelectorFeedback>();
            if (!feedback) feedback=Undo.AddComponent<RoomSelectorFeedback>(button.gameObject);
            var face=feedback.face ? feedback.face.GetComponent<UnityEngine.UI.Image>() :
                Picture("Enamel destination face",button.transform,Vector2.zero,new Vector2(258,74),null);
            Place(face.rectTransform,Vector2.zero,new Vector2(242,64));
            face.sprite=MechanicalSprite("RaisedButtonCap",304,1024,288);
            face.type=UnityEngine.UI.Image.Type.Simple;
            face.color=Color.white;face.raycastTarget=false;
            face.material=ui;
            var housing=button.transform.Find("Cast metal housing");
            if(housing)housing.gameObject.SetActive(false);
            var cap=face.transform.Find("Cap side walls");
            if(cap)cap.gameObject.SetActive(false);
            var caption=face.transform.Find("Embedded label")?.GetComponent<Text>();
            if(!caption)
            {
                caption=RoomMenuSetup.Label("Embedded label",face.transform,"",22,0,185,35,FontStyle.Bold);
                MoveText(caption,face.transform,virtualChoice ? "VIRTUAL ROOM":"YOUR ROOM",new Vector2(8,4),new Vector2(185,35),22,FontStyle.Bold);
            }
            var detail=feedback.detail;
            if(detail)detail.gameObject.SetActive(false);
            var dial=Picture("Pressure gauge",face.transform,new Vector2(-101,0),new Vector2(19,19),Sprite("SignalLens",1560,470,64,64,Vector4.zero));
            dial.color=Ivory;
            var needle=Picture("Pressure needle",dial.transform,new Vector2(0,0),new Vector2(1.3f,6),null);
            needle.rectTransform.pivot=new Vector2(.5f,0);needle.color=new Color(.15f,.22f,.17f);needle.rectTransform.localRotation=Quaternion.Euler(0,0,-28);
            feedback.pressureNeedle=needle.rectTransform;
            var vent=button.transform.Find("Pressure release");
            if(vent)Undo.DestroyObjectImmediate(vent.gameObject);
            feedback.steam=null;
            var illustration=Rect("Replaceable room illustration",group,new Vector2(0,75),new Vector2(146,118));
            illustration.localRotation=Quaternion.Euler(0,0,virtualChoice ? -4:5);
            var plate=Picture("Ivory enamel icon backplate",illustration,Vector2.zero,new Vector2(146,118),
                Sprite("IconBackplate",1560,8,480,232,new Vector4(85,65,85,65)));
            plate.type=UnityEngine.UI.Image.Type.Sliced;plate.pixelsPerUnitMultiplier=2.7f;
            var icon=Picture("Room icon - replace Source Image",illustration,new Vector2(0,1),new Vector2(116,93),
                Sprite(virtualChoice ? "VirtualRoomIcon":"YourRoomIcon",virtualChoice ? 1800:1560,264,224,180,Vector4.zero));
            icon.preserveAspect=true;
            var signal=Picture("Selection signal",group,new Vector2(133,-78),new Vector2(17,17),
                Sprite("SignalLens",1560,470,64,64,Vector4.zero));
            signal.color=new Color(.35f,.43f,.35f);
            feedback.face=face.rectTransform;feedback.artwork=face;feedback.detail=detail;
            feedback.menu=menu;feedback.virtualChoice=virtualChoice;feedback.arrivalDelay=0;
            feedback.illustration=illustration;feedback.replaceableIcon=icon;feedback.signal=signal;
            EditorUtility.SetDirty(feedback);
            return group;
        }

        private static ModularRoomMenuMotion.Piece Piece(RectTransform rect,float delay) =>
            new() {rect=rect,opacity=Group(rect),delay=delay};
        private static CanvasGroup Group(RectTransform rect)
        {
            var group=rect.GetComponent<CanvasGroup>();
            if (!group) group=Undo.AddComponent<CanvasGroup>(rect.gameObject);
            group.alpha=1;return group;
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var old=parent.Find(name);
            var rt=old ? old.GetComponent<RectTransform>() : null;
            if(!rt)
            {
                var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
                Undo.RegisterCreatedObjectUndo(go,"Create modular menu piece");rt=(RectTransform)go.transform;
            }
            Place(rt,position,size);return rt;
        }
        private static void Place(RectTransform rt,Vector2 position,Vector2 size)
        {
            rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);
            rt.localScale=Vector3.one;rt.localRotation=Quaternion.identity;
            rt.sizeDelta=size;rt.anchoredPosition3D=new Vector3(position.x,position.y,0);
        }
        private static UnityEngine.UI.Image Picture(string name,Transform parent,Vector2 pos,Vector2 size,Sprite sprite)
        {
            var rt=Rect(name,parent,pos,size);
            var image=rt.GetComponent<UnityEngine.UI.Image>();
            if (!image) image=Undo.AddComponent<UnityEngine.UI.Image>(rt.gameObject);
            image.sprite=sprite;image.raycastTarget=false;image.material=ui;image.color=Color.white;
            return image;
        }
        private static void MoveText(Text label,Transform parent,string value,Vector2 position,Vector2 size,int fontSize,FontStyle style)
        {
            if(!label) throw new InvalidOperationException("Expected live text reference");
            label.transform.SetParent(parent,false);Place(label.rectTransform,position,size);
            label.text=value;label.fontSize=fontSize;label.fontStyle=style;label.alignment=TextAnchor.MiddleCenter;
            label.color=Ivory;label.material=ui;label.raycastTarget=false;
            label.resizeTextForBestFit=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;
            label.verticalOverflow=VerticalWrapMode.Truncate;
        }
        private static void ImportArt()
        {
            ImportTexture(Art+"/MechanicalMenuAtlas.png",1024);
            ImportTexture(Atlas,2048);
        }
        private static void ImportTexture(string path,int maxSize)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var t=(TextureImporter)AssetImporter.GetAtPath(path);
            t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.alphaIsTransparency=true;
            t.mipmapEnabled=true;t.isReadable=false;t.npotScale=TextureImporterNPOTScale.None;
            t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Trilinear;t.anisoLevel=2;t.maxTextureSize=maxSize;
            var android=t.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=maxSize;
            android.format=TextureImporterFormat.ASTC_6x6;t.SetPlatformTextureSettings(android);t.SaveAndReimport();
        }
        private static Sprite MechanicalSprite(string name,int top,int width,int height)
        {
            string path=Art+"/"+name+".asset";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite)return sprite;
            sprite=UnityEngine.Sprite.Create(AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/MechanicalMenuAtlas.png"),new Rect(0,1024-top-height,width,height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;
        }
        private static MechanicalButtonBody Body(string name,Transform parent,Vector2 size,Color color,float thickness)
        {
            var rt=Rect(name,parent,Vector2.zero,size);var body=rt.GetComponent<MechanicalButtonBody>();
            if(!rt.GetComponent<CanvasRenderer>())Undo.AddComponent<CanvasRenderer>(rt.gameObject);
            if(!body)body=Undo.AddComponent<MechanicalButtonBody>(rt.gameObject);
            body.color=color;body.thickness=thickness;body.material=ui;body.raycastTarget=false;body.SetAllDirty();return body;
        }
        private static void MainMenu(RoomChoiceMenu menu)
        {
            var flow=menu.GetComponent<StationMainMenu>();if(!flow)flow=Undo.AddComponent<StationMainMenu>(menu.gameObject);
            var root=Rect("Station main menu",menu.canvas.transform,Vector2.zero,new Vector2(660,470));
            flow.roomMenu=menu;flow.content=root.gameObject;menu.mainMenu=flow;
            var sign=Picture("Station title",root,new Vector2(0,165),new Vector2(370,100),MechanicalSprite("StationHeading",0,1024,288));
            var title=MenuText("Game title",sign.transform,"SWITCHYARD RAIDERS",new Vector2(0,-10),new Vector2(330,46),27);
            title.color=new Color(.07f,.18f,.14f);
            flow.matchButton=MenuButton("Find a match",root,65,flow.FindMatch);
            flow.friendsButton=MenuButton("Play with friends",root,-15,flow.PlayWithFriends);
            flow.practiceButton=MenuButton("Learn and practice",root,-95,flow.Practice);
            flow.matchButton.interactable=flow.friendsButton.interactable=flow.practiceButton.interactable=false;
            var back=MenuButton("Change room",root,-173,flow.ChangeRoom);
            ((RectTransform)back.transform).sizeDelta=new Vector2(190,43);
            var backLabel=back.GetComponentInChildren<Text>();backLabel.fontSize=17;backLabel.rectTransform.sizeDelta=new Vector2(170,32);
            flow.status=MenuText("Mode availability",root,"Game modes are not connected yet",new Vector2(0,-220),new Vector2(550,28),14);
            root.gameObject.SetActive(false);EditorUtility.SetDirty(flow);
        }
        private static Text MenuText(string name,Transform parent,string text,Vector2 pos,Vector2 size,int fontSize)
        {
            var old=parent.Find(name);var label=old?old.GetComponent<Text>():null;
            if(!label)label=RoomMenuSetup.Label(name,parent,text,fontSize,0,size.x,size.y,FontStyle.Bold);
            MoveText(label,parent,text,pos,size,fontSize,FontStyle.Bold);return label;
        }
        private static UnityEngine.UI.Button MenuButton(string label,Transform parent,float y,UnityEngine.Events.UnityAction callback)
        {
            var image=Picture(label,parent,new Vector2(0,y),new Vector2(320,64),MechanicalSprite("RaisedButtonCap",304,1024,288));
            image.raycastTarget=true;
            var button=image.GetComponent<UnityEngine.UI.Button>();if(!button)button=Undo.AddComponent<UnityEngine.UI.Button>(image.gameObject);
            button.targetGraphic=image;button.transition=Selectable.Transition.ColorTint;
            var colors=button.colors;colors.disabledColor=new Color(.48f,.52f,.48f,.75f);button.colors=colors;
            button.navigation=new Navigation{mode=Navigation.Mode.None};
            while(button.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(button.onClick,0);
            UnityEventTools.AddPersistentListener(button.onClick,callback);
            MenuText("Label",image.transform,label.ToUpperInvariant(),Vector2.zero,new Vector2(290,38),23);
            return button;
        }
        private static Sprite Sprite(string name,int x,int y,int w,int h,Vector4 border)
        {
            string path=Art+"/"+name+".asset";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite) return sprite;
            sprite=UnityEngine.Sprite.Create(AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas),new Rect(x,1024-y-h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
            sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;
        }
        private static Material ArtMaterial(string name,Vector4 uv,float mode)
        {
            string path=Art+"/"+name+".mat";
            var shader=Shader.Find("Switchyard Raiders/XR/Room Menu Art");
            if(!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Room art shader has errors");
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) { m=new Material(shader);AssetDatabase.CreateAsset(m,path); }
            m.SetVector("_ArtRect",uv);m.SetFloat("_SheenMode",mode);m.SetFloat("_Hover",0);
            m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }

        [MenuItem("Switchyard Raiders/Room Menu/Validate modular design")]
        public static void Validate()
        {
            RoomMenuSetup.Validate();
            var menu=Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);
            var active=menu.canvas.GetComponentsInChildren<Graphic>(false);
            var buttons=menu.canvas.GetComponentsInChildren<UnityEngine.UI.Button>(false);
            var report=new System.Text.StringBuilder();
            if(menu.canvas.renderMode!=RenderMode.WorldSpace || buttons.Length!=2) throw new InvalidOperationException("World-space/two-button validation failed.");
            if(!menu.motion || !menu.motion.realRoomImage || !menu.motion.virtualRoomImage) throw new InvalidOperationException("Missing modular references");
            foreach(var button in buttons)
            {
                if(button.onClick.GetPersistentEventCount()!=1 || button.onClick.GetPersistentTarget(0)!=menu) throw new InvalidOperationException("Invalid button event");
                var feedback=button.GetComponent<RoomSelectorFeedback>();
                if(!feedback || !feedback.illustration || !feedback.signal || !feedback.replaceableIcon) throw new InvalidOperationException("Missing button visuals");
                report.AppendLine(button.name+" -> "+button.onClick.GetPersistentMethodName(0)+"; hit area "+((RectTransform)button.transform).sizeDelta);
            }
            foreach(var graphic in active)
            {
                if(!graphic.GetComponent<CanvasRenderer>())throw new InvalidOperationException("Missing CanvasRenderer: "+graphic.name);
                if(graphic.rectTransform.rect.width<=0 || graphic.rectTransform.rect.height<=0)throw new InvalidOperationException("Zero-size graphic "+graphic.name);
                if(graphic.raycastTarget && !graphic.GetComponent<UnityEngine.UI.Button>()) throw new InvalidOperationException("Decoration intercepts hands: "+graphic.name);
                if (graphic is Text label)
                {
                    float preferred = label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text, label.GetGenerationSettings(label.rectTransform.rect.size)) / label.pixelsPerUnit;
                    if (preferred > label.rectTransform.rect.height) throw new InvalidOperationException("Text would be truncated: " + label.name);
                }
            }
            report.AppendLine("PASS saved modular room-selection setup; "+active.Length+" visible graphics.");
            report.AppendLine("World space; original follower distance "+menu.follower.distance+"m, vertical offset "+menu.follower.heightOffset+"m; original canvas scale "+menu.canvas.transform.localScale.ToString("F6"));
            if(!menu.mainMenu || !menu.selectionRoot)throw new InvalidOperationException("Missing main-menu transition");
            report.AppendLine("2048x1024 room atlas + 1024x1024 sign/cap atlas; mipmaps; ASTC 6x6 Android. Two independent button hit targets.");
            report.AppendLine("Static editor verification only. No Play Mode, headset test, restart or player build.");
            report.AppendLine("Selection confirms briefly, exits, then shows StationMainMenu. Change room returns to selector. Gameplay routes disabled until connected.");
            Directory.CreateDirectory(Report);File.WriteAllText(Report+"/unity-validation.txt",report.ToString());
            Debug.Log(report);
        }
    }
}

