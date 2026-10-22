using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace SwitchyardRaiders.CompanyTrain.Editor
{
    public static class CompanyLocomotiveDetailValidation
    {
        [MenuItem("Switchyard Raiders/Company Locomotive/Validate cab chimney and clips")]
        public static void Run()
        {
            const string root="Assets/SwitchyardRaiders/CompanyLocomotive/";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"Prefabs/CompanyLocomotive.prefab");
            var go=UnityEngine.Object.Instantiate(prefab);
            var preview=new PreviewRenderUtility();
            var evidence=new System.Collections.Generic.List<string>();
            try
            {
                var motion=go.GetComponent<CompanyLocomotiveMotion>();motion.Initialize();
                var animator=go.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var nodes=go.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
                Vector3 origin=nodes["Basis_Origin"].position;
                Vector3 bx=nodes["Basis_X"].position-origin,by=nodes["Basis_Y"].position-origin,bz=nodes["Basis_Z"].position-origin;
                Vector3 B(float x,float y,float z)=>origin+bx*x+by*y+bz*z;
                var wheel=nodes["Drive1"];
                foreach(string state in new[]{"Rolling","Reverse"})
                {
                    motion.UseAnimatorPlayback(1);animator.Rebind();animator.SetFloat("PlaybackSpeed",1);
                    animator.Play("Base Layer."+state,0,.1f);animator.Update(0);
                    Vector3 point=wheel.InverseTransformPoint(wheel.position-go.transform.up*.825f);
                    Vector3 before=wheel.TransformPoint(point);animator.Update(.001f);
                    float speed=Vector3.Dot((wheel.TransformPoint(point)-before)/.001f,go.transform.forward);
                    if(state=="Rolling" ? speed> -2.5f : speed<2.5f)throw new Exception(state+" wrong contact direction: "+speed);
                    evidence.Add(state+" contact velocity along forward: "+speed+" m/s");
                    foreach(var marker in nodes.Values.Where(t=>t.name.StartsWith("NPC_")))
                        if(Vector3.Dot(marker.forward,go.transform.forward)<.999f)throw new Exception(marker.name+" faces away during "+state);
                }
                foreach(string name in new[]{"VFX_ChimneyExhaust","VFX_CylinderDrain_L","VFX_CylinderDrain_R","VFX_SafetyValve_L","VFX_SafetyValve_R","VFX_Whistle"})
                    if(!nodes.ContainsKey(name))throw new Exception("Missing steam attachment "+name);
                motion.SetTravelDistance(0);
                foreach(var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh=new UnityEngine.Mesh();renderer.BakeMesh(mesh);
                    try
                    {
                        var vertices=mesh.vertices.Select(v=>renderer.transform.TransformPoint(v)).ToArray();var tri=mesh.triangles;
                        foreach(var offset in new[]{Vector2.zero,new Vector2(.1f,0),new Vector2(-.1f,0),new Vector2(0,.1f),new Vector2(0,-.1f)})
                        {
                            var ray=new Ray(B(-4.93f+offset.x,offset.y,4.6f),-bz.normalized);
                            for(int i=0;i<tri.Length;i+=3)
                                if(Intersects(ray,vertices[tri[i]],vertices[tri[i+1]],vertices[tri[i+2]],1.3f))
                                    throw new Exception(renderer.name+" has a face blocking the chimney bore");
                        }
                        evidence.Add(renderer.name+": five rays pass through mouth and boiler shell.");
                    }
                    finally{UnityEngine.Object.DestroyImmediate(mesh);}
                }
                preview.AddSingleGO(go);preview.camera.orthographic=true;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=100;
                preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.08f,.10f,.12f);
                preview.lights[0].intensity=2;preview.lights[0].transform.rotation=Quaternion.Euler(35,210,0);
                preview.lights[1].intensity=1.5f;preview.lights[1].transform.rotation=Quaternion.Euler(40,20,0);
                preview.ambientColor=new Color(.45f,.45f,.45f);
                foreach(int lod in new[]{0,1})
                foreach(string view in new[]{"Cab","Chimney","Fittings"})
                {
                    go.GetComponent<LODGroup>().ForceLOD(lod);
                    preview.camera.transform.position=view=="Cab"?B(3.55f,-1.22f,3.37f):view=="Chimney"?B(-6.1f,-1.5f,5.8f):B(2.5f,-2.7f,4.4f);
                    preview.camera.transform.LookAt(view=="Cab"?B(4.4f,-.6f,2.48f):view=="Chimney"?B(-4.93f,0,3.95f):B(2.4f,-.1f,3.64f),go.transform.up);
                    preview.camera.orthographicSize=view=="Fittings"?.58f:.85f;
                    preview.BeginStaticPreview(new Rect(0,0,1100,1000));preview.Render(true);
                    var texture=preview.EndStaticPreview();
                    File.WriteAllBytes("Deliverables/CompanyLocomotiveV2/Preview/Unity_"+view+"_LOD"+lod+".png",texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                File.WriteAllLines("Deliverables/CompanyLocomotiveV2/Validation/unity_functional_details.txt",evidence);
                Debug.Log("Functional details passed: bore open in all LODs, both clip directions correct, NPC axes and six VFX anchors present.");
            }
            finally{preview.Cleanup();if(go)UnityEngine.Object.DestroyImmediate(go);}
        }

        private static bool Intersects(Ray ray,Vector3 a,Vector3 b,Vector3 c,float maximum)
        {
            Vector3 e1=b-a,e2=c-a,p=Vector3.Cross(ray.direction,e2);
            float determinant=Vector3.Dot(e1,p);if(Mathf.Abs(determinant)<1e-8f)return false;
            float inverse=1/determinant;Vector3 t=ray.origin-a;
            float u=Vector3.Dot(t,p)*inverse;if(u<0||u>1)return false;
            Vector3 q=Vector3.Cross(t,e1);float v=Vector3.Dot(ray.direction,q)*inverse;
            if(v<0||u+v>1)return false;float distance=Vector3.Dot(e2,q)*inverse;
            return distance>1e-5f&&distance<maximum;
        }
    }
}
