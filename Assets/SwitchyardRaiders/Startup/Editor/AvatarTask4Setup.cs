using System;
using System.Linq;
using Oculus.Interaction.Input;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class AvatarTask4Setup
    {
        public const string Launch="Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab";
        public static void Configure(RaidersTable table)
        {
            var avatar=table.session.avatars;var rig=table.launch.xrRigRoot;
            var hands=rig.GetComponentsInChildren<Hand>(true).Where(h=>h.transform.parent && h.transform.parent.name=="OVRHands").ToArray();
            avatar.leftHandSource=hands.Single(h=>h.name=="LeftHand");avatar.rightHandSource=hands.Single(h=>h.name=="RightHand");
            avatar.hmdSource=rig.GetComponentsInChildren<Hmd>(true).Single(h=>h.name=="OVRHmd");
            avatar.outlineRenderers=rig.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r=>r.name=="l_handMeshNode" || r.name=="r_handMeshNode" || (r.name=="LeftHand"&&r.transform.parent.name=="OpenXRLeftHand") || (r.name=="RightHand"&&r.transform.parent.name=="OpenXRRightHand"))
                .Cast<Renderer>().ToArray();
            if(avatar.outlineRenderers.Length!=4)throw new InvalidOperationException("Expected four existing OVR/OpenXR hand mesh renderers; do not hide cursors or tracking objects.");
            if(avatar.trackingSpace.IsChildOf(table.presentation) || avatar.playerSeats.Any(s=>s.IsChildOf(table.presentation) || s.lossyScale!=Vector3.one))
                throw new InvalidOperationException("Physical avatar and human seats must remain outside miniature scale.");
            EditorUtility.SetDirty(avatar);PrefabUtility.RecordPrefabInstancePropertyModifications(avatar);
            var menu=avatar.GetComponent<RaidersMenu>();
            var footer=menu.detail.rectTransform;
            footer.anchoredPosition=new Vector2(0,-210);footer.sizeDelta=new Vector2(630,50);
            EditorUtility.SetDirty(footer);PrefabUtility.RecordPrefabInstancePropertyModifications(footer);
        }
        [MenuItem("Switchyard Raiders/Demeo Rework/9 - Bind first-person account avatar")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !Application.dataPath.EndsWith("Switchyard Raiders/Assets"))throw new InvalidOperationException("Switchyard Raiders Edit Mode required.");
            var table=Object.FindAnyObjectByType<RaidersTable>(FindObjectsInactive.Include);
            if(!table || table.gameObject.scene.path!=LaunchSequenceTools.ScenePath || table.gameObject.scene.isDirty)throw new InvalidOperationException("Clean SR_Launch required; preserve other unsaved work.");
            Configure(table);
            var root=PrefabUtility.LoadPrefabContents(Launch);
            try{Configure(root.GetComponentInChildren<RaidersTable>(true));PrefabUtility.SaveAsPrefabAsset(root,Launch);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            EditorSceneManager.MarkSceneDirty(table.gameObject.scene);EditorSceneManager.SaveScene(table.gameObject.scene);AssetDatabase.SaveAssets();
        }
    }
}
