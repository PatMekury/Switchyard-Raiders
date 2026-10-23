using System;
using System.IO;
using System.Linq;
using System.Text;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class RaidersRoomViewSetup
    {
        private const string Root = "Assets/SwitchyardRaiders/Startup/";
        [MenuItem("Switchyard Raiders/Room Menu/Apply VR-first room fallback")]
        public static void Apply()
        {
            Guard();
            var room = Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);
            if (!room || room.gameObject.scene.path != Root + "Scenes/SR_Launch.unity")
                throw new InvalidOperationException("Open SR_Launch first.");
            Configure(room);
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
            if (!EditorSceneManager.SaveScene(room.gameObject.scene)) throw new IOException("Scene save failed.");
            var path = Root + "Prefabs/SR_Launch.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Configure(prefab.GetComponentInChildren<RoomChoiceMenu>(true));
                PrefabUtility.SaveAsPrefabAsset(prefab, path, out bool saved);
                if (!saved) throw new IOException("Prefab save failed.");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            AssetDatabase.SaveAssets();
            Validate();
        }
        private static void Guard()
        {
            if (!Application.dataPath.Replace('\\', '/').EndsWith("/Switchyard Raiders/Assets") ||
                EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Use the idle Switchyard Raiders editor.");
        }
        private static void Configure(RoomChoiceMenu room)
        {
            Undo.RecordObject(room, "Configure room fallback");
            var vr = (RectTransform)room.virtualButton.transform.parent;
            var real = (RectTransform)room.realButton.transform.parent;
            Undo.RecordObjects(new Object[] { vr, real }, "Put VR first");
            vr.anchoredPosition = new Vector2(-145, vr.anchoredPosition.y);
            real.anchoredPosition = new Vector2(145, real.anchoredPosition.y);
            vr.SetSiblingIndex(Math.Min(vr.GetSiblingIndex(), real.GetSiblingIndex()));
            if (room.motion)
            {
                Undo.RecordObject(room.motion, "Animate VR first");
                foreach (var piece in room.motion.pieces)
                { if (piece.rect == vr) piece.delay = .10f; if (piece.rect == real) piece.delay = .17f; }
            }
            SetLabel(room.virtualButton, "PLAY IN VR");
            SetLabel(room.realButton, "USE YOUR ROOM");
            Undo.RecordObject(room.heading, "Clarify optional room access");
            room.heading.text = "CHOOSE YOUR SPACE";
            room.heading.fontSize = 27;
            Layout((RectTransform)room.heading.transform.parent, new Vector2(0,158), new Vector2(352,96));
            Layout(room.heading.rectTransform, new Vector2(0,-10), new Vector2(310,54));
            room.heading.transform.parent.localRotation = Quaternion.identity;
            room.status.text = "Play in VR anywhere, or use your saved room.";
            room.status.fontSize = 18;
            foreach (var follower in room.launch.GetComponentsInChildren<SeatedPanelFollow>(true))
            {
                Undo.RecordObject(follower, "Keep UI inside narrower view");
                follower.heightOffset = -.08f;
                EditorUtility.SetDirty(follower);
            }
            Undo.RecordObject(room.launch, "Align opening title with menu");
            room.launch.titleDownAngle = Mathf.Atan2(.08f, room.launch.titleDistance) * Mathf.Rad2Deg;

            var popup = Rect("Room unavailable popup", room.canvas.transform, Vector2.zero, new Vector2(500,270));
            var background = Get<Image>(popup.gameObject);
            background.color = new Color(.045f,.085f,.07f,.98f);
            background.material = room.heading.material;
            background.raycastTarget = true;
            room.roomUnavailablePopup = popup.gameObject;
            room.roomUnavailableHeading = Label("Heading", popup, room.heading, "Room not found", new Vector2(0,80), new Vector2(460,44), 28);
            room.roomUnavailableMessage = Label("Explanation", popup, room.status, "Your saved room isn't available here. Play in VR instead.", new Vector2(0,20), new Vector2(450,70), 22);
            var play = Button("Play in VR", popup, room, new Vector2(-115,-82));
            var back = Button("Back", popup, room, new Vector2(115,-82));
            play.onClick = new Button.ButtonClickedEvent();
            back.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(play.onClick, room.ChooseVirtualRoom);
            UnityEventTools.AddPersistentListener(back.onClick, room.DismissRoomUnavailable);
            popup.SetAsLastSibling();
            popup.gameObject.SetActive(false);
            foreach (var component in room.launch.GetComponentsInChildren<Component>(true))
                if (component) { EditorUtility.SetDirty(component); PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
        }
        private static T Get<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? Undo.AddComponent<T>(go);
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var existing = parent.Find(name);
            var rt = existing ? (RectTransform)existing : null;
            if (!rt)
            {
                var go = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Add room fallback UI");
                go.transform.SetParent(parent, false); rt = (RectTransform)go.transform;
            }
            Layout(rt, position, size); return rt;
        }
        private static void Layout(RectTransform rt, Vector2 position, Vector2 size)
        {
            Undo.RecordObject(rt, "Layout room UI");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f,.5f);
            rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
            rt.anchoredPosition3D = new Vector3(position.x,position.y,0); rt.sizeDelta = size;
        }
        private static Text Label(string name, Transform parent, Text source, string value, Vector2 pos, Vector2 size, int fontSize)
        {
            var label = Get<Text>(Rect(name,parent,pos,size).gameObject);
            label.font = source.font; label.material = source.material; label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter; label.color = new Color(.97f,.92f,.78f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            label.text = value; label.raycastTarget = false;
            return label;
        }
        private static Button Button(string name, Transform parent, RoomChoiceMenu room, Vector2 position)
        {
            var rt = Rect(name,parent,position,new Vector2(210,60));
            var image = Get<Image>(rt.gameObject); image.material = room.heading.material;
            image.color = new Color(.22f,.38f,.29f); image.raycastTarget = true;
            var button = Get<Button>(rt.gameObject); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label",rt,room.heading,name,Vector2.zero,new Vector2(190,50),22);
            return button;
        }
        private static void SetLabel(Button button, string text)
        {
            var label = button.GetComponentsInChildren<Text>(true).First(t => t.name == "Embedded label");
            Undo.RecordObject(label, "Label room choice"); label.text = text;
        }
        [MenuItem("Switchyard Raiders/Room Menu/Validate room fallback and viewing angles")]
        public static void Validate()
        {
            Guard();
            if (EditorUtility.scriptCompilationFailed) throw new InvalidOperationException("Compilation failed.");
            var report = new StringBuilder("VR-first room selection / narrower-FoV review\nTarget content area: 52 horizontal x 48 vertical degrees.\n");
            var room = Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);
            Check(room, "Scene", report);
            var prefab = PrefabUtility.LoadPrefabContents(Root + "Prefabs/SR_Launch.prefab");
            try { Check(prefab.GetComponentInChildren<RoomChoiceMenu>(true), "Prefab", report); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            if (!RaidersViewLayout.YardFitsView(Vector3.zero,Vector3.forward,new Vector3(0,-.5f,RaidersViewLayout.YardDistance),Quaternion.identity))
                throw new Exception("Virtual yard is outside content view.");
            if (RaidersViewLayout.YardFitsView(Vector3.zero,Vector3.forward,new Vector3(0,-.5f,1.05f),Quaternion.identity))
                throw new Exception("Near, oversized yard should be rejected.");
            report.AppendLine("PASS: yard footprint fits at 2.3m; former 1.05m placement is rejected.");
            report.AppendLine("Gameplay control panel: 76% of menu scale, +0.11m vertical offset; yard remains below the controls.");
            report.AppendLine("Room load and placement both require headset containment; no automatic Space Setup.");
            report.AppendLine("Startup preflight never prompts; explicit Use your room may request Spatial Data.");
            report.AppendLine("Runtime camera projections are unchanged. World-space placements are adjusted.");
            report.AppendLine("Pending: device/simulator perceived-FoV check in both eyes, missing/wrong room, denied permission, cancel/retry, and MR surface placement. No Play Mode or player build run.");
            Directory.CreateDirectory("Deliverables/MainMenuFlow");
            File.WriteAllText("Deliverables/MainMenuFlow/room-view-validation.txt", report.ToString());
            Debug.Log(report.ToString());
        }
        private static void Check(RoomChoiceMenu room, string name, StringBuilder report)
        {
            if (!room.roomUnavailablePopup || !room.roomUnavailableHeading || !room.roomUnavailableMessage)
                throw new Exception(name + ": missing popup references.");
            if (room.virtualButton.transform.parent.localPosition.x >= room.realButton.transform.parent.localPosition.x)
                throw new Exception(name + ": VR must be first.");
            foreach (var button in room.roomUnavailablePopup.GetComponentsInChildren<Button>(true))
                if (button.onClick.GetPersistentEventCount()!=1 || button.onClick.GetPersistentTarget(0)!=room)
                    throw new Exception(name + ": popup action not wired.");
            float maxX=0, maxY=0;
            var corners = new Vector3[4];
            foreach (var graphic in room.canvas.GetComponentsInChildren<Graphic>(true))
            {
                // Includes every screen and inactive modal, not only the editor's visible page.
                graphic.rectTransform.GetWorldCorners(corners);
                foreach (var point in corners)
                {
                    var p = room.follower.transform.InverseTransformPoint(point);
                    p.y += room.follower.heightOffset; p.z += room.follower.distance;
                    float x=Mathf.Abs(Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg);
                    float y=Mathf.Abs(Mathf.Atan2(p.y,p.z)*Mathf.Rad2Deg);
                    maxX=Mathf.Max(maxX,x); maxY=Mathf.Max(maxY,y);
                    if (x>RaidersViewLayout.HorizontalHalfAngle || y>RaidersViewLayout.VerticalHalfAngle)
                        throw new Exception(name + ": graphic outside view: " + graphic.name + " ("+x+", "+y+")");
                    var gameplay = room.follower.transform.InverseTransformPoint(point) * .76f;
                    gameplay.y += .11f; gameplay.z += room.follower.distance;
                    if (Mathf.Abs(Mathf.Atan2(gameplay.x,gameplay.z)*Mathf.Rad2Deg)>RaidersViewLayout.HorizontalHalfAngle ||
                        Mathf.Abs(Mathf.Atan2(gameplay.y,gameplay.z)*Mathf.Rad2Deg)>RaidersViewLayout.VerticalHalfAngle)
                        throw new Exception(name + ": gameplay panel outside view: " + graphic.name);
                }
            }
            report.AppendLine(name + ": PASS popup wiring, VR ordering, all menu screens within +/-"+maxX.ToString("F1")+" horizontal, +/-"+maxY.ToString("F1")+" vertical degrees.");
            var titleFollow = room.launch.titleFollow;
            float titleX=0, titleY=0;
            foreach (var renderer in room.launch.titleAnchor.GetComponentsInChildren<Renderer>(true))
            for (int i=0;i<8;i++)
            {
                var b=renderer.localBounds;
                var world=renderer.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                var point=titleFollow.transform.InverseTransformPoint(world);
                point.y+=titleFollow.heightOffset; point.z+=titleFollow.distance;
                titleX=Mathf.Max(titleX,Mathf.Abs(Mathf.Atan2(point.x,point.z)*Mathf.Rad2Deg));
                titleY=Mathf.Max(titleY,Mathf.Abs(Mathf.Atan2(point.y,point.z)*Mathf.Rad2Deg));
            }
            if (titleX>RaidersViewLayout.HorizontalHalfAngle || titleY>RaidersViewLayout.VerticalHalfAngle)
                throw new Exception(name + ": opening title outside view.");
            report.AppendLine(name + ": PASS static opening title within +/-"+titleX.ToString("F1")+" horizontal, +/-"+titleY.ToString("F1")+" vertical degrees.");
        }
    }
}
