using System;
using System.Linq;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    // Kept under its existing type/name so old setup entry points migrate, never reinstall contact-only input.
    public static class TabletopHandGrabSetup
    {
        [MenuItem("Switchyard Raiders/Tabletop/Install two-hand grab anywhere")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !Application.dataPath.EndsWith("Switchyard Raiders/Assets"))
                throw new InvalidOperationException("Switchyard Raiders in Edit Mode required.");
            var table=Object.FindAnyObjectByType<RaidersTable>(FindObjectsInactive.Include);
            if(!table || table.gameObject.scene.path!=LaunchSequenceTools.ScenePath || table.gameObject.scene.isDirty)
                throw new InvalidOperationException("Open clean SR_Launch first; preserve unsaved work.");
            Configure(table);
            // Edit each asset in place; exporting the scene root would replace prefab-specific
            // data and regenerate unrelated object IDs in this shared checkout.
            const string path="Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try{Configure(prefab.GetComponentInChildren<RaidersTable>(true));PrefabUtility.SaveAsPrefabAsset(prefab,path);}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            EditorSceneManager.MarkSceneDirty(table.gameObject.scene);
            EditorSceneManager.SaveScene(table.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Two-hand palm classification bound to existing tracked hands; obsolete table contact proxy removed.");
        }
        public static void Configure(RaidersTable table)
        {
            var hands=table.launch.xrRigRoot.GetComponentsInChildren<Hand>(true)
                .Where(h=>h.transform.parent && h.transform.parent.name=="OVRHands").ToArray();
            if(hands.Length!=2)throw new InvalidOperationException("Expected existing left and right ISDK hands.");
            foreach(var hand in hands)
            {
                var api=hand.GetComponentInChildren<HandGrabAPI>(true);
                if(!api)throw new InvalidOperationException("Existing HandGrabAPI missing; do not create another rig.");
                // Dedicated reference to unmodified tracked hand data, not a synthetic hand pose.
                api.InjectHand(hand);
                // Hand.Handedness reads live data, which defaults to Left in inactive prefab
                // contents. Bind the verified authored rig nodes, not uninitialized tracking data.
                if(hand.name=="LeftHand")table.manipulation.leftPalm=api;
                else if(hand.name=="RightHand")table.manipulation.rightPalm=api;
                else throw new InvalidOperationException("Unexpected authored hand node: "+hand.name);
                EditorUtility.SetDirty(api);PrefabUtility.RecordPrefabInstancePropertyModifications(api);
            }
            if(!table.manipulation.leftPalm || !table.manipulation.rightPalm)throw new InvalidOperationException("Both palm APIs required.");
            var obsolete=table.launch.transform.Find("Table rim hand grab");
            if(obsolete)Object.DestroyImmediate(obsolete.gameObject);
            table.environmentMotion=table.roomMenu.virtualRoom.transform;
            if(table.environmentMotion.IsChildOf(table.presentation) || table.launch.xrRigRoot.transform.IsChildOf(table.environmentMotion) ||
                table.environmentMotion.lossyScale!=Vector3.one)throw new InvalidOperationException("Room must be a unit-scale sibling, outside tracking and table.");
            EditorUtility.SetDirty(table);EditorUtility.SetDirty(table.manipulation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(table);
            PrefabUtility.RecordPrefabInstancePropertyModifications(table.manipulation);
        }
    }
}
