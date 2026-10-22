using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace SwitchyardRaiders.CompanyTrain.Editor
{
    public static class CompanyLocomotiveBuilder
    {
        private const string Root = "Assets/SwitchyardRaiders/CompanyLocomotive";
        [MenuItem("Switchyard Raiders/Company Locomotive/Build prefab from exported assets")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            string modelPath = Root + "/Art/CompanyLocomotive.fbx";
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Exported locomotive FBX is missing.");
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.importAnimation = true;
            importer.importCameras = false; importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = false; importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = true; clip.loopPose = false;
                clip.name = clip.name.Contains("Reverse") ? "Locomotive_Reverse" : "Locomotive_Forward";
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            foreach (string map in new[] { "BaseColor", "Normal", "Mask" })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(Root + "/Art/CompanyLocomotive_" + map + ".png");
                ti.textureType = map == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = map == "BaseColor";
                ti.alphaSource = map == "Mask" ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                ti.maxTextureSize = 4096; ti.mipmapEnabled = true; ti.streamingMipmaps = true;
                ti.wrapMode = TextureWrapMode.Clamp; ti.filterMode = FilterMode.Trilinear; ti.anisoLevel = 4;
                ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                    name = "Android", overridden = true, maxTextureSize = 2048,
                    format = TextureImporterFormat.ASTC_6x6, compressionQuality = 100 });
                ti.SaveAndReimport();
            }
            string matPath = Root + "/Art/CompanyLocomotive.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
            Texture2D Tex(string suffix) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/CompanyLocomotive_" + suffix + ".png");
            mat.SetTexture("_BaseMap", Tex("BaseColor")); mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", Tex("Normal")); mat.SetFloat("_BumpScale", 1);
            mat.SetTexture("_MetallicGlossMap", Tex("Mask")); mat.SetFloat("_Metallic", 1); mat.SetFloat("_Smoothness", 1);
            mat.SetTexture("_OcclusionMap", Tex("Mask")); mat.SetFloat("_OcclusionStrength", .65f);
            mat.EnableKeyword("_NORMALMAP"); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); mat.EnableKeyword("_OCCLUSIONMAP");
            mat.enableInstancing = true; EditorUtility.SetDirty(mat);
            var rollingClip = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .FirstOrDefault(c => c.name == "Locomotive_Forward");
            var reverseClip = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .FirstOrDefault(c => c.name == "Locomotive_Reverse");
            if (!reverseClip) throw new InvalidOperationException("Explicit reverse clip is missing from FBX.");
            if (!rollingClip || rollingClip.length < 39.9f) throw new InvalidOperationException("Imported rolling animation is missing or truncated.");
            Directory.CreateDirectory(Root + "/Animations"); AssetDatabase.Refresh();
            string controllerPath = Root + "/Animations/CompanyLocomotive.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (!controller.parameters.Any(p => p.name == "PlaybackSpeed")) controller.AddParameter("PlaybackSpeed",AnimatorControllerParameterType.Float);
            var parameters = controller.parameters;
            foreach(var parameter in parameters) if(parameter.name=="PlaybackSpeed") parameter.defaultFloat=1;
            controller.parameters=parameters;
            var stateMachine=controller.layers[0].stateMachine;
            var rollingState=stateMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Rolling");
            if(!rollingState) rollingState=stateMachine.AddState("Rolling");
            rollingState.motion=rollingClip; rollingState.speed=1; rollingState.speedParameter="PlaybackSpeed";
            rollingState.speedParameterActive=true;rollingState.writeDefaultValues=false;
            stateMachine.defaultState=rollingState;EditorUtility.SetDirty(rollingState);EditorUtility.SetDirty(controller);
            var reverseState=stateMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Reverse");
            if(!reverseState) reverseState=stateMachine.AddState("Reverse");
            reverseState.motion=reverseClip;reverseState.speed=1;reverseState.speedParameterActive=false;
            reverseState.writeDefaultValues=false;EditorUtility.SetDirty(reverseState);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var wrapper = new GameObject("CompanyLocomotive");
            wrapper.SetActive(false);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source,wrapper.transform);
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var importedLod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(importedLod);
                // FBX clip bindings are relative to the imported model root, not wrapper.
                var animator=model.GetComponent<Animator>();
                if(!animator) animator=model.AddComponent<Animator>();
                animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
                animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;animator.enabled=true;
                model.transform.SetParent(wrapper.transform, false);
                var transforms = model.GetComponentsInChildren<Transform>(true);
                Transform Find(string n) => transforms.First(t => t.name == n);
                // Rotate the imported source so the vehicle points along prefab +Z.
                Vector3 forward = (Find("Basis_Origin").position - Find("Basis_X").position).normalized;
                Vector3 up = (Find("Basis_Z").position - Find("Basis_Origin").position).normalized;
                model.transform.rotation = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
                // NPC anchors use Unity +Z forward / +Y up, matching the vehicle.
                // Keep this explicit at the import boundary; FBX empty axes differ.
                foreach(var marker in transforms.Where(t=>t.name.StartsWith("NPC_")))
                    marker.rotation = wrapper.transform.rotation;
                var renders = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (renders.Length != 4) throw new InvalidOperationException("Expected four rigged LOD meshes, got " + renders.Length);
                var lods = new LOD[4]; float[] screen = { .65f, .12f, .035f, .008f };
                for (int i = 0; i < 4; i++)
                {
                    var r = renders.Single(v => v.name == "CompanyLocomotive_LOD" + i);
                    r.gameObject.SetActive(true); r.sharedMaterial = mat; r.quality = SkinQuality.Bone1;
                    r.updateWhenOffscreen = false; r.shadowCastingMode = ShadowCastingMode.On;
                    // Slight margin around the moving rods, no huge animated bounds.
                    Bounds b = r.localBounds; b.Expand(.8f); r.localBounds = b;
                    lods[i] = new LOD(screen[i], new Renderer[] { r });
                }
                var lod = wrapper.AddComponent<LODGroup>(); lod.fadeMode = LODFadeMode.None; lod.SetLODs(lods); lod.RecalculateBounds();
                wrapper.AddComponent<CompanyLocomotiveMotion>();
                // Leave the cab and standing platform usable; a solid whole-train box
                // would block the NPC from occupying the newly designed interior.
                var collision = wrapper.AddComponent<BoxCollider>(); collision.center = new Vector3(0,1.8f,1.8f); collision.size = new Vector3(2.9f,2.3f,9.8f);
                Vector3 o = Find("Basis_Origin").position;
                Vector3 bx = Find("Basis_X").position-o, by = Find("Basis_Y").position-o, bz = Find("Basis_Z").position-o;
                void Floor(string name, Vector3 authoredPosition, Vector3 authoredSize)
                {
                    var child = new GameObject(name); child.transform.SetParent(wrapper.transform,false);
                    child.transform.position = o + bx*authoredPosition.x + by*authoredPosition.y + bz*authoredPosition.z;
                    child.AddComponent<BoxCollider>().size = new Vector3(authoredSize.y,authoredSize.z,authoredSize.x);
                }
                Floor("CabFloorCollider",new Vector3(4.5f,0,1.65f),new Vector3(3,2.72f,.14f));
                Floor("StandingDeckCollider",new Vector3(5.78f,0,2.91f),new Vector3(1.8f,2.6f,.1f));
                for(int step=0;step<5;step++) Floor("CabStairCollider"+step,new Vector3(3.96f+step*.22f,.75f,1.96f+step*.25f),new Vector3(.26f,.6f,.055f));
                var gameplayMesh = renders.Single(r=>r.name=="CompanyLocomotive_LOD1").sharedMesh;
                if(gameplayMesh.vertexCount>25000 || gameplayMesh.GetIndexCount(0)/3>15000)
                    throw new InvalidOperationException("Gameplay LOD exceeds 25k imported vertices / 15k triangles: "+gameplayMesh.vertexCount+" vertices, "+gameplayMesh.GetIndexCount(0)/3+" triangles.");
                foreach(string marker in new[]{"NPC_Seat","NPC_Stand","NPC_Aim","NPC_LauncherMuzzle","NPC_DriveHand_L","NPC_DriveHand_R"}) Find(marker);
                Directory.CreateDirectory(Root + "/Prefabs");
                CompanyLocomotiveSmokeBuilder.Attach(wrapper);
                wrapper.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(wrapper, Root + "/Prefabs/CompanyLocomotive.prefab");
                File.WriteAllText("Deliverables/CompanyLocomotiveV2/Validation/unity_import.txt",
                    "Unity " + Application.unityVersion + "\n" + string.Join("\n", renders.Select(r => r.name + ": " + r.sharedMesh.vertexCount + " imported vertices; " + (r.sharedMesh.GetIndexCount(0) / 3) + " triangles; " + r.sharedMesh.subMeshCount + " submesh; " + r.bones.Length + " bones")) +
                    "\n4 LODs; one opaque URP material; ASTC 6x6 at 2048 on Android.\nAnimator Controller: "+controllerPath+"; state Rolling; imported clip "+rollingClip.name+"; duration "+rollingClip.length+" seconds; PlaybackSpeed defaults to 1.\nNo live headset frame-time claim.\n");
            }
            finally { UnityEngine.Object.DestroyImmediate(wrapper); }
            AssetDatabase.SaveAssets();
            Debug.Log("Company locomotive prefab built and import report saved.");
        }
    }
}
