using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.Editor.QuickActions;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    internal static class DestinationBoard
    {
        private const string Atlas = RoomMenuSetup.Root + "/Art/SR_Destination_Atlas.png";
        public static void Apply(RoomChoiceMenu menu)
        {
            AssetDatabase.ImportAsset(Atlas,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(Atlas);
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=true; importer.alphaIsTransparency=true; importer.mipmapEnabled=true;
            importer.isReadable=false; importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp; importer.filterMode=FilterMode.Trilinear; importer.maxTextureSize=2048;
            var android=importer.GetPlatformTextureSettings("Android"); android.overridden=true; android.maxTextureSize=2048;
            android.format=TextureImporterFormat.ASTC_6x6; importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
            // Only replace our visual children; keep the SDK interaction surfaces and event system.
            if (menu.realButton) menu.realButton.transform.SetParent(menu.canvas.transform,false);
            if (menu.virtualButton) menu.virtualButton.transform.SetParent(menu.canvas.transform,false);
            foreach(Transform child in menu.canvas.transform.Cast<Transform>().ToArray())
                if (child.GetComponent<Graphic>() && child.GetComponent<UnityEngine.UI.Button>()==null)
                    Undo.DestroyObjectImmediate(child.gameObject);
            var frame=Image("Destination board",menu.canvas.transform,new Vector2(620,430),
                Slice("SR_DestinationBoard",new Rect(20,1254-690,1215,660),new Vector4(90,115,90,290)));
            frame.type=UnityEngine.UI.Image.Type.Sliced; frame.pixelsPerUnitMultiplier=2;
            frame.transform.SetAsFirstSibling();
            menu.heading=RoomMenuSetup.Label("Inset heading",frame.transform,"CHOOSE YOUR ROOM",27,103,480,46,FontStyle.Bold);
            menu.heading.color=new Color(.12f,.16f,.13f);
            menu.realButton=Choice(menu,menu.realButton,frame.transform,false,menu.ChooseRealRoom);
            menu.virtualButton=Choice(menu,menu.virtualButton,frame.transform,true,menu.ChooseVirtualRoom);
            menu.status=RoomMenuSetup.Label("Inset instruction",frame.transform,"Touch a destination, or point and pinch",15,-178,490,36);
            menu.status.color=new Color(.12f,.16f,.13f);
        }
        private static UnityEngine.UI.Button Choice(RoomChoiceMenu menu,UnityEngine.UI.Button button,Transform parent,bool virtualChoice,UnityEngine.Events.UnityAction action)
        {
            if (!button)
            {
                var go=new GameObject("Destination",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
                button=go.GetComponent<UnityEngine.UI.Button>();
                UnityEventTools.AddPersistentListener(button.onClick,action);
            }
            button.name=virtualChoice ? "Virtual room destination" : "Your room destination";
            button.transform.SetParent(parent,false);
            var rt=(RectTransform)button.transform;
            rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);
            rt.sizeDelta=new Vector2(250,198); rt.anchoredPosition=new Vector2(virtualChoice ? 137:-137,-44);
            rt.localScale=Vector3.one; rt.localRotation=Quaternion.identity;
            foreach(var component in button.GetComponents<MonoBehaviour>())
                if(component is RoomSelectorFeedback || component is TicketArrival || component is EnamelButtonFeedback) Undo.DestroyObjectImmediate(component);
            foreach(Transform child in button.transform.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(child.gameObject);
            var hit=button.GetComponent<UnityEngine.UI.Image>(); hit.color=Color.clear; hit.raycastTarget=true;
            button.targetGraphic=hit; button.transition=Selectable.Transition.None;
            button.navigation=new Navigation {mode=Navigation.Mode.None};
            var face=Image("Enamel destination face",button.transform,rt.sizeDelta,
                Slice(virtualChoice ? "SR_DestinationVirtual" : "SR_DestinationReal",
                    new Rect(virtualChoice ? 637:4,1254-1188,613,466),Vector4.zero));
            var caption=RoomMenuSetup.Label("Embedded label",face.transform,virtualChoice ? "VIRTUAL ROOM":"YOUR ROOM",25,-42,213,34,FontStyle.Bold);
            var detail=RoomMenuSetup.Label("Selection state",face.transform,virtualChoice ? "A quiet space to play":"Play where you are",14,-70,211,25);
            var feedback=button.gameObject.AddComponent<RoomSelectorFeedback>();
            feedback.face=face.rectTransform; feedback.artwork=face; feedback.detail=detail;
            feedback.menu=menu; feedback.virtualChoice=virtualChoice; feedback.arrivalDelay=virtualChoice ? .07f:0;
            return button;
        }
        private static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 size,Sprite sprite)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform; rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);
            rt.sizeDelta=size; rt.anchoredPosition=Vector2.zero;
            var image=go.GetComponent<UnityEngine.UI.Image>(); image.sprite=sprite; image.raycastTarget=false;
            // Use Unity's XR-capable UI material for both pictures and live text.
            image.material=null; return image;
        }
        private static Sprite Slice(string name,Rect rect,Vector4 border)
        {
            string path=RoomMenuSetup.Root+"/Art/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(existing) return existing;
            var sprite=Sprite.Create(AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas),rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
            sprite.name=name; AssetDatabase.CreateAsset(sprite,path); return sprite;
        }
    }
}

namespace SwitchyardRaiders.Editor
{
    public static class RoomMenuSetup
    {
        public const string Root = "Assets/SwitchyardRaiders/Startup/RoomMenu";
        public const string Report = "Deliverables/RoomMenu";
        private static readonly Color Ivory = new(.94f, .89f, .73f);

        [MenuItem("Switchyard Raiders/Room Menu/Install or update")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            if (!Application.dataPath.Replace('\\','/').EndsWith("/Switchyard Raiders/Assets")) throw new InvalidOperationException("Wrong project.");
            var launch = Object.FindAnyObjectByType<LaunchSequence>();
            if (!launch || launch.gameObject.scene.path != LaunchSequenceTools.ScenePath) throw new InvalidOperationException("Open SR_Launch.");
            Directory.CreateDirectory(Report);
            bool preview = launch.desktopPreviewInEditor;
            launch.xrRigRoot.SetActive(true);
            InstallHands(launch);
            var titleFollow = launch.titleAnchor.GetComponent<SeatedPanelFollow>();
            if (!titleFollow) titleFollow = launch.titleAnchor.gameObject.AddComponent<SeatedPanelFollow>();
            titleFollow.distance = launch.titleDistance = .65f;
            titleFollow.repositionThreshold = .6f;
            titleFollow.heightOffset = -.15f;
            launch.titleDownAngle = 12.995f;
            launch.titleFollow = titleFollow;
            launch.presentation.size = new Vector2(.44f, .44f * 847 / 1116);
            launch.presentation.titleRenderer.enabled = true;
            titleFollow.Snap(launch.desktopPreviewHead);
            titleFollow.head = launch.headset;
            launch.presentation.ResetForPlayback();
            // Flat preview approximates headset framing; XR keeps its runtime projection.
            launch.desktopPreviewHead.GetComponent<Camera>().fieldOfView = 75;

            var existingMenu = launch.transform.Find("SR_RoomChoice");
            var root = existingMenu ? existingMenu.gameObject : new GameObject("SR_RoomChoice");
            if (!existingMenu) root.transform.SetParent(launch.transform, false);
            var menu = root.GetComponent<RoomChoiceMenu>() ?? root.AddComponent<RoomChoiceMenu>();
            menu.launch = launch;
            if (!menu.follower)
            {
                var anchor = new GameObject("Seated menu anchor"); anchor.transform.SetParent(root.transform,false);
                menu.follower = anchor.AddComponent<SeatedPanelFollow>();
            }
            menu.follower.distance = .65f;
            menu.follower.heightOffset = -.15f;
            menu.follower.followSpeed = 3f;
            menu.follower.repositionThreshold = .6f;
            menu.follower.Snap(launch.desktopPreviewHead); menu.follower.head = launch.headset;
            if (!menu.canvas)
            {
                var canvasObject = new GameObject("Room choice canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(menu.follower.transform,false);
                menu.canvas=canvasObject.GetComponent<Canvas>();
            }
            var rect=(RectTransform)menu.canvas.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=new Vector2(660,470); rect.localScale=Vector3.one*.00085f;
            rect.localPosition=Vector3.zero; rect.localRotation=Quaternion.identity;
            menu.canvas.renderMode=RenderMode.WorldSpace;
            menu.canvas.worldCamera=launch.headset.GetComponent<Camera>();
            menu.group=menu.canvas.GetComponent<CanvasGroup>();
            menu.panelRoot=menu.canvas.gameObject;
            if (!menu.realButton || !menu.virtualButton) DestinationBoard.Apply(menu);
            ModularRoomMenuSetup.ApplyVisuals(menu);
            var uiMaterial=XRMaterial("SR_WorldUI", "Switchyard Raiders/XR/World UI");
            foreach(var graphic in menu.canvas.GetComponentsInChildren<Graphic>(true))
                if (!graphic.material || graphic.material.shader.name != "Switchyard Raiders/XR/Room Menu Art") graphic.material=uiMaterial;
            if (!menu.canvas.GetComponentsInChildren<PointableCanvas>(true).Any(p=>p.GetComponent<PokeInteractable>()))
                QuickActionsAPI.AddPokeCanvasInteraction(menu.canvas.gameObject);
            if (!menu.canvas.GetComponentsInChildren<PointableCanvas>(true).Any(p=>p.GetComponent<RayInteractable>()))
                QuickActionsAPI.AddRayCanvasInteraction(menu.canvas.gameObject);
            var module = Object.FindAnyObjectByType<PointableCanvasModule>();
            if (!module) throw new InvalidOperationException("Meta PointableCanvasModule missing.");
            // The SDK moves this object's event camera. It must never be an ancestor of the UI or hands.
            if (!module.transform.IsChildOf(launch.transform)) module.transform.SetParent(root.transform, true);
            module.ExclusiveMode = true;
            if (!menu.virtualRoom) menu.virtualRoom=CreateRoom(launch.transform);
            menu.virtualRoom.SetActive(false);
            launch.xrRigRoot.SetActive(!preview);
            if (!existingMenu) Undo.RegisterCreatedObjectUndo(root, "Install room choice menu");
            EditorUtility.SetDirty(launch);
            PrefabUtility.SaveAsPrefabAsset(launch.gameObject, LaunchSequenceTools.Root + "/Prefabs/SR_Launch.prefab");
            EditorSceneManager.MarkSceneDirty(launch.gameObject.scene);
            EditorSceneManager.SaveScene(launch.gameObject.scene);
            AssetDatabase.SaveAssets();
            Validate();
        }

        private static void InstallHands(LaunchSequence launch)
        {
            var rig = launch.xrRigRoot.GetComponent<OVRCameraRig>();
            var existing = rig.GetComponentInChildren<OVRCameraRigRef>(true);

            const string package = "Packages/com.meta.xr.sdk.interaction.ovr/Runtime/Prefabs/";
            var interaction = existing ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(package + "OVRInteraction.prefab"), rig.transform);
            interaction.name = "SR_HandInteraction";
            var reference = interaction.GetComponent<OVRCameraRigRef>(); reference.InjectAllOVRCameraRigRef(rig, true);
            var transformer = interaction.GetComponent<TrackingToWorldTransformerOVR>();
            transformer.InjectCameraRigRef(reference);
            var hands = interaction.transform.Find("OVRHands");
            if (!hands) hands=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(package + "OVRHands.prefab"), interaction.transform)).transform;
            // Wire the SDK's existing hand data stack, skeleton providers and hand meshes.
            foreach (var component in interaction.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var serialized = new SerializedObject(component);
                SetReference(serialized, "_cameraRigRef", reference);
                SetReference(serialized, "_trackingToWorldTransformer", transformer);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var hand in hands.GetComponentsInChildren<Hand>(true))
                if (hand.transform.parent == hands && !hand.GetComponentInChildren<PokeInteractor>(true))
                    QuickActionsAPI.AddHandInteractors(hand.gameObject, QuickActionInteractorTypes.Poke | QuickActionInteractorTypes.Ray);
            var rigRefs=new SerializedObject(reference);
            SetReference(rigRefs,"_leftHand",launch.leftHand); SetReference(rigRefs,"_rightHand",launch.rightHand);
            rigRefs.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reference); EditorUtility.SetDirty(transformer);
            var handMaterial=XRMaterial("SR_HandOutline", "Switchyard Raiders/XR/Hand Outline");
            foreach(var renderer in hands.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!renderer.sharedMaterial || renderer.sharedMaterial.shader.name != "Interaction/OculusHand") continue;
                renderer.sharedMaterial=handMaterial;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        [MenuItem("Switchyard Raiders/Room Menu/Fix pointer rendering")]
        public static void FixPointerRendering()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            var launch=Object.FindAnyObjectByType<LaunchSequence>();
            if(!launch || launch.gameObject.scene.path!=LaunchSequenceTools.ScenePath) throw new InvalidOperationException("Open SR_Launch first.");
            int changed=0;
            foreach(var renderer in launch.xrRigRoot.GetComponentsInChildren<Renderer>(true))
            {
                var source=renderer.sharedMaterial;
                if(!source) continue;
                string oldShader=source.shader.name;
                bool cursor=oldShader=="Hidden/Interaction/Deprecated/OculusHandCursor";
                bool pinch=renderer.name=="OculusHandPinchArrowBlended" && oldShader=="UI/Default";
                bool selection=renderer.name=="SelectionCircle" && oldShader=="Sprites/Default";
                if(!cursor && !pinch && !selection) continue;
                string name=cursor ? "SR_PokeCursor" : pinch ? "SR_PinchIndicator" : "SR_SelectionRing";
                var material=XRMaterial(name,cursor ? "Switchyard Raiders/XR/Poke Cursor" : "Switchyard Raiders/XR/Pointer Visual");
                material.CopyPropertiesFromMaterial(source);
                material.enableInstancing=true;
                EditorUtility.SetDirty(material);
                renderer.sharedMaterial=material;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                changed++;
            }
            PrefabUtility.SaveAsPrefabAsset(launch.gameObject,LaunchSequenceTools.Root+"/Prefabs/SR_Launch.prefab");
            EditorSceneManager.MarkSceneDirty(launch.gameObject.scene);
            EditorSceneManager.SaveScene(launch.gameObject.scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Report);
            File.WriteAllText(Report+"/pointer-render-fix.txt",$"Saved {changed} pointer renderer assignments. Poke cursor radial/press properties retained; pinch and selector textures/colors retained. Tracking transforms, fingertip offsets, hands and menu unchanged. No Play test performed.\n");
        }

        private static Material XRMaterial(string name,string shaderName)
        {
            var shader=Shader.Find(shaderName);
            if(!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("XR shader unavailable: "+shaderName);
            string path=Root+"/Art/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) { material=new Material(shader); AssetDatabase.CreateAsset(material,path); }
            material.enableInstancing=true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetReference(SerializedObject target, string name, Object value)
        {
            var p = target.FindProperty(name); if (p != null && p.propertyType == SerializedPropertyType.ObjectReference) p.objectReferenceValue = value;
        }

        internal static Text Label(string name, Transform parent, string text, int size, float y, float width, float height, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(width,height); rt.anchoredPosition = new Vector2(0,y);
            var label = go.GetComponent<Text>(); label.text = text; label.fontSize = size; label.fontStyle = style;
            label.font = AssetDatabase.LoadAssetAtPath<Font>(style == FontStyle.Bold ? "Assets/Photon/PhotonVoice/Demos/DemoVoiceUI/RobotoFont/RobotoCondensed-Bold.ttf" : "Assets/Photon/PhotonVoice/Demos/DemoVoiceUI/RobotoFont/RobotoCondensed-Regular.ttf");
            label.alignment = TextAnchor.MiddleCenter; label.color = Ivory; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static GameObject CreateRoom(Transform parent)
        {
            var root = new GameObject("SR_VirtualRoom"); root.transform.SetParent(parent, false);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Reference/DefaultEmptyRoom.prefab");
            if (!source) throw new InvalidOperationException("Spirit Sling fallback room missing.");
            var walls = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            var wallMaterial = FlatMaterial("SR_RoomWall", new Color(.24f,.28f,.27f));
            foreach (var renderer in walls.GetComponentsInChildren<MeshRenderer>())
            { renderer.sharedMaterial = wallMaterial; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
            // The reference is a four-wall MRUK room definition. Add inexpensive floor/ceiling surfaces for full VR.
            Surface("Floor", root.transform, new Vector3(.23f,-.025f,-.22f), new Vector3(3.8f,.05f,4.3f), FlatMaterial("SR_RoomFloor", new Color(.13f,.16f,.15f)));
            Surface("Ceiling", root.transform, new Vector3(.23f,2.43f,-.22f), new Vector3(3.8f,.05f,4.3f), FlatMaterial("SR_RoomCeiling", new Color(.34f,.37f,.34f)));
            return root;
        }

        private static void Surface(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent,false);
            go.transform.localPosition = position; go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        }

        private static Material FlatMaterial(string name, Color color)
        {
            string path = Root + "/Art/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat,path); }
            mat.SetColor("_BaseColor",color); mat.SetFloat("_Cull",0); EditorUtility.SetDirty(mat); return mat;
        }

        [MenuItem("Switchyard Raiders/Room Menu/Validate scene")]
        public static void Validate()
        {
            var launch = Object.FindAnyObjectByType<LaunchSequence>(FindObjectsInactive.Include);
            var menu = Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);
            if (!menu) throw new InvalidOperationException("Room menu missing.");
            int missing = launch.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            int pokes = launch.GetComponentsInChildren<PokeInteractor>(true).Length;
            int rays = launch.GetComponentsInChildren<RayInteractor>(true).Length;
            int pointable = menu.GetComponentsInChildren<PointableCanvas>(true).Length;
            bool pass = missing == 0 && pokes == 2 && rays == 2 && pointable == 2 && menu.realButton.onClick.GetPersistentEventCount() == 1 && menu.virtualButton.onClick.GetPersistentEventCount() == 1;
            string report = $"{(pass ? "PASS" : "FAIL")} room menu scene validation\nScene: {launch.gameObject.scene.path}\nMissing scripts: {missing}\nPoke interactors: {pokes}; ray interactors: {rays}; pointable canvases: {pointable}\nTitle size: {launch.presentation.size}; distance: {launch.titleDistance}\nMenu distance: {menu.follower.distance}; eye height offset: {menu.follower.heightOffset}\nVirtual renderers: {menu.virtualRoom.GetComponentsInChildren<Renderer>(true).Length}\nDesktop preview: {launch.desktopPreviewInEditor}\nHardware hands, comfort and GPU timing require Quest Link testing.\n";
            Directory.CreateDirectory(Report); File.WriteAllText(Report+"/scene-validation.txt", report); Debug.Log(report);
            if (!pass) throw new InvalidOperationException(report);
        }
    }
}
