using System;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Editor.QuickActions;
using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class TabletopHandGrabSetup
    {
        private static T Get<T>(GameObject go) where T:Component => go.GetComponent<T>() ?? go.AddComponent<T>();

        [MenuItem("Switchyard Raiders/Tabletop/Install contact hand grabbing")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !Application.dataPath.EndsWith("Switchyard Raiders/Assets"))
                throw new InvalidOperationException("Switchyard Raiders in Edit Mode required.");
            var table=Object.FindAnyObjectByType<RaidersTable>(FindObjectsInactive.Include);
            if(!table || table.gameObject.scene.path!=LaunchSequenceTools.ScenePath)throw new InvalidOperationException("Open SR_Launch first.");
            Configure(table);
            PrefabUtility.SaveAsPrefabAsset(table.launch.gameObject,"Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab");
            EditorSceneManager.MarkSceneDirty(table.gameObject.scene);
            EditorSceneManager.SaveScene(table.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Table contact palm grabbing installed on the existing two-hand rig. Developer testing setting preserved.");
        }

        public static void Configure(RaidersTable table)
        {
            var hands=table.launch.xrRigRoot.GetComponentsInChildren<Hand>(true)
                .Where(h=>h.transform.parent && h.transform.parent.name=="OVRHands").ToArray();
            if(hands.Length!=2)throw new InvalidOperationException("Expected the existing left and right ISDK hands.");
            foreach(var hand in hands)
                if(!hand.GetComponentInChildren<HandGrabInteractor>(true))
                    QuickActionsAPI.AddHandInteractors(hand.gameObject,QuickActionInteractorTypes.Grab);

            var found=table.launch.transform.Find("Table rim hand grab");
            var proxy=found?found.gameObject:new GameObject("Table rim hand grab");
            proxy.transform.SetParent(table.launch.transform,false);
            proxy.transform.localScale=Vector3.one;
            var body=Get<Rigidbody>(proxy);body.isKinematic=true;body.useGravity=false;
            var grab=Get<Grabbable>(proxy);
            grab.MaxGrabPoints=2;
            grab.InjectOptionalTargetTransform(table.presentation);
            grab.InjectOptionalRigidbody(body);
            grab.InjectOptionalThrowWhenUnselected(false);
            grab.InjectOptionalOneGrabTransformer(table.manipulation);
            grab.InjectOptionalTwoGrabTransformer(table.manipulation);
            var contact=Get<HandGrabInteractable>(proxy);
            contact.InjectAllHandGrabInteractable(GrabTypeFlags.Palm,body,GrabbingRule.DefaultPinchRule,GrabbingRule.DefaultPalmRule);
            contact.InjectOptionalPointableElement(grab);
            contact.InjectOptionalMovementProvider(Get<MoveFromTargetProvider>(proxy));
            contact.HandAlignment=HandAlignType.None;
            contact.MaxSelectingInteractors=2;
            contact.ResetGrabOnGrabsUpdated=false;
            contact.enabled=false;
            var edges=proxy.GetComponents<BoxCollider>().ToList();
            while(edges.Count<4)edges.Add(proxy.AddComponent<BoxCollider>());
            if(edges.Count!=4)throw new InvalidOperationException("Expected exactly four table rim colliders.");
            foreach(var edge in edges)edge.isTrigger=true;
            table.manipulation.handGrab=contact;
            table.manipulation.grabbable=grab;
            table.manipulation.grabEdges=edges.ToArray();
            table.manipulation.SyncGrabSurface();
            EditorUtility.SetDirty(table.manipulation);
            foreach(var component in proxy.GetComponents<Component>())EditorUtility.SetDirty(component);
        }
    }
}
