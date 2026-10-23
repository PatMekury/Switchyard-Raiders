using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.PlayerVehicles.Editor
{
    public static class PlayerVehicleValidation
    {
        private static readonly StringBuilder Report=new();
        private static int failures;
        private static void Check(bool ok,string message)
        {Report.AppendLine((ok?"PASS ":"FAIL ")+message);if(!ok)failures++;}
        [MenuItem("Switchyard Raiders/Player Vehicles/Validate imported assets")]
        public static void Validate()
        {
            Report.Clear();failures=0;
            Report.AppendLine("Imported Unity mesh, clip, contact and anchor validation | "+Application.unityVersion);
            foreach(var name in PlayerVehicleBuilder.Names)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVehicleBuilder.Root+"/Prefabs/"+name+".prefab");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    var motion=go.GetComponent<PlayerLocomotiveMotion>();motion.Initialize();
                    var nodes=go.GetComponentsInChildren<Transform>(true);
                    Transform Find(string n)=>nodes.Single(t=>t.name==n);
                    var animator=motion.Animator;var model=animator.gameObject;
                    Check(animator.runtimeAnimatorController!=null&&!animator.applyRootMotion,name+" assigned controller / no root motion");
                    Check(go.transform.localScale==Vector3.one,name+" clean root scale");
                    Check(go.GetComponentsInChildren<ParticleSystem>(true).Length==0,name+" no inherited steam smoke");
                    foreach(var end in new[]{"Front","Rear"})
                    {
                        int sign=end=="Front"?1:-1;var c=Find("Coupler_"+end);var p=go.transform.InverseTransformPoint(c.position);
                        Check(Vector3.Distance(p,new Vector3(0,1.05f,sign*7.4f))<.001f,name+" "+end+" coupler height/spacing "+p);
                        Check(Vector3.Dot(c.forward,go.transform.forward*sign)>.999f&&Vector3.Dot(c.up,go.transform.up)>.999f,name+" "+end+" anchor +Z outward/+Y up");
                        var seat=Find("CabSeat_"+end);var hand=Find("ControlHand_"+end);
                        Check(Vector3.Dot(hand.position-seat.position,seat.forward)>.1f,name+" "+end+" control ahead of outward-facing seat");
                    }
                    var lods=go.GetComponent<LODGroup>().GetLODs();
                    for(int i=0;i<lods.Length;i++)
                    {
                        int v=lods[i].renderers.Cast<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.vertexCount);
                        long t=lods[i].renderers.Cast<SkinnedMeshRenderer>().Sum(r=>(long)r.sharedMesh.GetIndexCount(0)/3);
                        Check(i!=1||v<=25000,name+" LOD"+i+" imported vertices="+v+" triangles="+t);
                        Check(i==0?t>=30000&&t<=40000:i==1?t<=16000:t<=5500,name+" LOD"+i+" geometry envelope");
                        foreach(var r in lods[i].renderers.Cast<SkinnedMeshRenderer>())
                        {
                            Check(r.sharedMesh.uv.Length==r.sharedMesh.vertexCount,name+" "+r.name+" UV0 present");
                            Check(r.sharedMaterials.Length==1&&r.sharedMaterial!=null,name+" "+r.name+" single assigned material");
                        }
                    }
                    var clips=AssetDatabase.LoadAllAssetsAtPath(PlayerVehicleBuilder.Root+"/Art/"+name+".fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
                    foreach(var clip in clips)
                    {
                        var bindings=AnimationUtility.GetCurveBindings(clip);
                        Check(bindings.All(b=>string.IsNullOrEmpty(b.path)||model.transform.Find(b.path)),name+" "+clip.name+" all imported bindings resolve ("+bindings.Length+")");
                    }
                    var openClip=clips.Single(c=>c.name=="Doors_Open");
                    foreach(bool front in new[]{true,false})foreach(bool left in new[]{true,false})
                    {
                        var door=Find("Door_"+(front?"Front":"Rear")+(left?"_L":"_R"));
                        openClip.SampleAnimation(model,openClip.length);var exported=door.rotation;
                        motion.SetDoorOpen(front,left,1);
                        Check(Quaternion.Angle(exported,door.rotation)<.15f,name+" FBX door opening matches outward runtime pivot "+door.name);
                    }
                    clips.Single(c=>c.name=="Forward").SampleAnimation(model,0);
                    foreach(float scale in new[]{1f,.08f,.02f})
                    {
                        go.transform.localScale=Vector3.one*scale;
                        foreach(float direction in new[]{1f,-1f})
                        {
                            motion.SetBogieYaw(0,0);motion.SetTravelDistance(0);
                            foreach(var w in nodes.Where(n=>n.name.StartsWith("Wheel_")))
                            {
                                Vector3 contact=w.position-go.transform.up*.6f*scale;
                                Vector3 local=w.InverseTransformPoint(contact);
                                motion.SetTravelDistance(direction*.0006);
                                float tangential=Vector3.Dot(w.TransformPoint(local)-contact,go.transform.forward)/scale;
                                Check(Mathf.Abs(tangential+direction*.0006f)<.00002f,name+" "+w.name+" distance contact direction="+direction+" scale="+scale);
                                motion.SetTravelDistance(0);
                            }
                            string state=direction>0?"Forward":"Reverse";var clip=clips.Single(c=>c.name==state);
                            animator.enabled=false;clip.SampleAnimation(model,0);
                            var wheel=Find("Wheel_F0");var contact2=wheel.position-go.transform.up*.6f*scale;var local2=wheel.InverseTransformPoint(contact2);
                            Vector3 rootPosition=go.transform.position;clip.SampleAnimation(model,.01f);
                            float travel=Vector3.Dot(wheel.TransformPoint(local2)-contact2,go.transform.forward)/scale;
                            Check(travel*direction<-.005f,name+" actual FBX "+state+" wheel contact scale="+scale+" displacement="+travel);
                            Check(go.transform.position==rootPosition,name+" "+state+" no root translation");
                            clip.SampleAnimation(model,0);var q=wheel.rotation;clip.SampleAnimation(model,clip.length);
                            Check(Quaternion.Angle(q,wheel.rotation)<.1f,name+" "+state+" loop seam");
                        }
                        motion.SetTravelDistance(.3);var stopped=Find("Wheel_F0").rotation;motion.SetTravelDistance(.3);
                        Check(Quaternion.Angle(stopped,Find("Wheel_F0").rotation)<.001f,name+" exact stop scale="+scale);
                    }
                    go.transform.localScale=Vector3.one;
                    motion.UseAnimatorPlayback(-1);Check(animator.enabled&&animator.speed==1,name+" reverse Animator uses positive speed");
                    motion.SetTravelDistance(0);Check(!animator.enabled&&motion.driver==PlayerLocomotiveMotion.Driver.RailDistance,name+" Animator / distance exclusivity");
                    motion.SetBogieYaw(15,-15);Check(Quaternion.Angle(Find("Bogie_Front").rotation,Find("Bogie_Rear").rotation)>29,name+" independent bogie articulation");
                    motion.SetBogieYaw(0,0);
                    foreach(var end in new[]{"Front","Rear"})
                    {
                        var door=Find("Door_"+end+"_L");motion.SetDoorOpen(end=="Front",true,0);var q=door.rotation;motion.SetDoorOpen(end=="Front",true,1);
                        Check(Mathf.Abs(Quaternion.Angle(q,door.rotation)-78)<.1f,name+" "+end+" door pivot 78 degrees");
                    }
                    // Simple test crossheads only: actual attachment geometry, no production wagon.
                    foreach(int sign in new[]{-1,1})
                    {
                        var anchor=Find(sign>0?"Coupler_Front":"Coupler_Rear");
                        var fixture=new GameObject("Temporary coupling crosshead");fixture.transform.SetPositionAndRotation(anchor.position,Quaternion.LookRotation(-anchor.forward,anchor.up));
                        Check(Vector3.Distance(fixture.transform.position,anchor.position)<.00001f&&Vector3.Dot(fixture.transform.forward,anchor.forward)<-.999f,name+" fixture attaches at "+(sign>0?"front":"rear"));
                        foreach(float yaw in new[]{-20f,0f,20f})
                        {
                            fixture.transform.rotation=Quaternion.AngleAxis(yaw,go.transform.up)*Quaternion.LookRotation(-anchor.forward,anchor.up);
                            var box=fixture.AddComponent<BoxCollider>();box.center=new Vector3(0,0,-.65f);box.size=new Vector3(2.35f,.38f,.6f);
                            bool overlap=false;
                            foreach(var c in go.GetComponentsInChildren<BoxCollider>())
                                overlap|=Physics.ComputePenetration(box,box.transform.position,box.transform.rotation,c,c.transform.position,c.transform.rotation,out _,out _);
                            Check(!overlap,name+" crosshead/chassis clearance yaw="+yaw+" end="+sign);Object.DestroyImmediate(box);
                        }
                        Object.DestroyImmediate(fixture);
                    }
                }
                finally{Object.DestroyImmediate(go);}
            }
            Report.AppendLine("No headset connected; no 90Hz, five-train GPU/CPU or thermal certification.");
            Report.AppendLine("Failures: "+failures);File.WriteAllText(PlayerVehicleBuilder.Output+"/Validation/checks.txt",Report.ToString());
            Debug.Log("Player vehicle validation failures="+failures);
        }
        public static void Capture(string label,Vector3 cameraPosition,Vector3 target,float fov=40,int lod=-1,bool doors=false)
        {
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(PlayerVehicleBuilder.Root+"/Scenes/PlayerVehicles_Review.unity",OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var camera=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Camera>()).Single();camera.cullingMask=1<<31;
                foreach(var light in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light>()))light.cullingMask=1<<31;
                foreach(var group in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<LODGroup>()))group.ForceLOD(lod);
                if(doors)foreach(var m in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PlayerLocomotiveMotion>()))
                    foreach(bool front in new[]{true,false})foreach(bool left in new[]{true,false})m.SetDoorOpen(front,left,1);
                camera.transform.position=cameraPosition;camera.transform.LookAt(target);camera.fieldOfView=fov;
                Render(camera,PlayerVehicleBuilder.Output+"/Review/"+label+".png",1920,1080);
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void Render(Camera camera,string path,int width,int height)
        {
            var old=RenderTexture.active;var rt=new RenderTexture(width,height,24);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);}
        }
        public static void CaptureMotionFrames(int first,int count)
        {
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(PlayerVehicleBuilder.Root+"/Scenes/PlayerVehicles_Review.unity",OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);Directory.CreateDirectory(PlayerVehicleBuilder.Output+"/Review/Frames");
                foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var camera=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Camera>()).Single();camera.cullingMask=1<<31;
                camera.transform.position=new Vector3(1.4f,.75f,1.65f);camera.transform.LookAt(new Vector3(0,.13f,.10f));camera.fieldOfView=43;
                foreach(var light in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light>()))light.cullingMask=1<<31;
                var demos=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PlayerVehicleReviewDemo>()).ToArray();
                foreach(var group in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<LODGroup>()))group.ForceLOD(0);
                for(int f=first;f<first+count;f++)
                {
                    foreach(var demo in demos)demo.Evaluate(f/12f);
                    Render(camera,PlayerVehicleBuilder.Output+"/Review/Frames/"+f.ToString("D4")+".png",1280,720);
                }
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void CaptureCouplers()
        {
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(PlayerVehicleBuilder.Root+"/Scenes/PlayerVehicles_Review.unity",OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Camera>()).Single();camera.cullingMask=1<<31;
                var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.9f,.48f,.12f)};
                foreach(var motion in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PlayerLocomotiveMotion>()))
                {
                    foreach(int end in new[]{-1,1})
                    {
                        var anchor=motion.GetComponentsInChildren<Transform>().Single(t=>t.name==(end>0?"Coupler_Front":"Coupler_Rear"));
                        motion.SetCouplingYaw(end>0,end*20);
                        var fixture=new GameObject("Temporary "+(end>0?"front":"rear")+" attachment fixture");fixture.transform.SetPositionAndRotation(anchor.position,Quaternion.LookRotation(anchor.forward,anchor.up));fixture.transform.localScale=Vector3.one*.08f;
                        var cross=GameObject.CreatePrimitive(PrimitiveType.Cube);cross.name="Test crosshead only";cross.transform.SetParent(fixture.transform,false);cross.transform.localPosition=new Vector3(0,0,.62f);cross.transform.localScale=new Vector3(2.35f,.32f,.20f);cross.GetComponent<Renderer>().sharedMaterial=material;
                        var link=GameObject.CreatePrimitive(PrimitiveType.Cube);link.name="Test attachment linkage";link.transform.SetParent(fixture.transform,false);link.transform.localPosition=new Vector3(0,0,.31f);link.transform.localScale=new Vector3(.07f,.07f,.62f);link.GetComponent<Renderer>().sharedMaterial=material;
                    }
                }
                foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                foreach(var light in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light>()))light.cullingMask=1<<31;
                camera.transform.position=new Vector3(.95f,.55f,1.25f);camera.transform.LookAt(new Vector3(0,.10f,.55f));camera.fieldOfView=42;
                Render(camera,PlayerVehicleBuilder.Output+"/Review/Unity_Coupling_Front.png",1920,1080);
                camera.transform.position=new Vector3(-.95f,.55f,-1.25f);camera.transform.LookAt(new Vector3(0,.10f,-.55f));
                Render(camera,PlayerVehicleBuilder.Output+"/Review/Unity_Coupling_Rear.png",1920,1080);
                Object.DestroyImmediate(material);
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
