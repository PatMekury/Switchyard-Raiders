using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace SwitchyardRaiders.CompanyTrain.Editor
{
    public static class CompanyLocomotiveSmokeValidation
    {
        [MenuItem("Switchyard Raiders/Company Locomotive/Validate and preview smoke")]
        public static void Run()
        {
            const string root="Assets/SwitchyardRaiders/CompanyLocomotive/";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"Prefabs/CompanyLocomotive.prefab");
            var board=new GameObject("Smoke preview board");
            var go=UnityEngine.Object.Instantiate(prefab,board.transform);
            var preview=new PreviewRenderUtility();
            try
            {
                var smoke=go.GetComponent<CompanyLocomotiveSmoke>();smoke.Initialize();
                var ps=smoke.chimneySmoke;var renderer=ps.GetComponent<ParticleSystemRenderer>();
                if(ps.main.maxParticles!=48||renderer.sharedMaterial.shader.name!="Universal Render Pipeline/Particles/Unlit")throw new Exception("Smoke budget or material incorrect");
                if(ps.main.customSimulationSpace!=board.transform)throw new Exception("Smoke must follow board space, not train space");
                if(Vector3.Dot(ps.transform.forward,go.transform.up)<.999f)throw new Exception("Chimney emission axis points away from top");
                ps.useAutoRandomSeed=false;ps.randomSeed=173;
                var motion=go.GetComponent<CompanyLocomotiveMotion>();motion.SetTravelDistance(0);
                for(int i=0;i<72;i++){smoke.UpdateEmission(2.591814f,0,1f/24);ps.Simulate(1f/24,false,i==0,true);}
                int alive=ps.particleCount;if(alive<10||alive>48)throw new Exception("Plume failed to simulate: "+alive);
                var particles=new ParticleSystem.Particle[48];int count=ps.GetParticles(particles);
                Vector3 relative=particles[0].position;
                go.transform.position+=new Vector3(0,0,2);ps.GetParticles(particles);
                if((particles[0].position-relative).sqrMagnitude>1e-8f)throw new Exception("Old puffs follow the locomotive instead of lingering");
                go.transform.position=Vector3.zero;
                smoke.SetEngineRunning(false);smoke.UpdateEmission(0,0,.1f);ps.Simulate(3.1f,false,false,true);
                if(ps.particleCount!=0)throw new Exception("Engine-off smoke does not dissipate");
                smoke.SetEngineRunning(true);smoke.UpdateEmission(2.6f,100,.1f);
                if(ps.particleCount!=0||ps.isPlaying)throw new Exception("Distant effect not culled");
                smoke.UpdateEmission(2.6f,0,.1f);
                if(!ps.isPlaying)throw new Exception("Smoke failed to resume nearby");
                preview.AddSingleGO(board);go.GetComponent<LODGroup>().ForceLOD(1);
                preview.camera.orthographic=true;preview.camera.orthographicSize=8;
                preview.camera.nearClipPlane=.05f;preview.camera.farClipPlane=100;
                preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.09f,.12f,.15f);
                preview.lights[0].intensity=1.8f;preview.lights[0].transform.rotation=Quaternion.Euler(40,210,0);
                preview.lights[1].intensity=1.2f;preview.lights[1].transform.rotation=Quaternion.Euler(40,20,0);
                preview.ambientColor=new Color(.4f,.4f,.4f);
                ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                for(int i=0;i<72;i++){smoke.UpdateEmission(2.591814f,0,1f/24);ps.Simulate(1f/24,false,i==0,true);}
                preview.camera.transform.position=new Vector3(17,11,19);preview.camera.transform.LookAt(new Vector3(0,3.4f,0));
                preview.BeginStaticPreview(new Rect(0,0,1400,1000));preview.Render(true);var still=preview.EndStaticPreview();
                File.WriteAllBytes("Deliverables/CompanyLocomotiveV2/Preview/Unity_ChimneySmoke.png",still.EncodeToPNG());UnityEngine.Object.DestroyImmediate(still);
                // Simulate and render a small moving-train sequence, without entering
                // the user's gameplay scene or changing the editor's Play state.
                string frames="Deliverables/CompanyLocomotiveV2/Preview/SmokeFrames";Directory.CreateDirectory(frames);
                for(int i=0;i<72;i++)
                {
                    float distance=i/24f*2.591814f;go.transform.position=Vector3.forward*distance;motion.SetTravelDistance(distance);
                    smoke.UpdateEmission(2.591814f,0,1f/24);ps.Simulate(1f/24,false,false,true);
                    preview.camera.transform.position=new Vector3(17,11,19)+go.transform.position;
                    preview.camera.transform.LookAt(new Vector3(0,3.4f,0)+go.transform.position);
                    preview.BeginStaticPreview(new Rect(0,0,960,640));preview.Render(true);var image=preview.EndStaticPreview();
                    File.WriteAllBytes(frames+"/"+i.ToString("D4")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                }
                // At miniature scale verify actual particle spread scales with the hierarchy.
                go.transform.position=Vector3.zero;board.transform.localScale=Vector3.one*.05f;
                ps.Simulate(2,false,true,true);count=ps.GetParticles(particles);
                float maxDistance=0;Vector3 outlet=board.transform.InverseTransformPoint(ps.transform.position);
                for(int i=0;i<count;i++)maxDistance=Mathf.Max(maxDistance,Vector3.Distance(particles[i].position,outlet));
                if(count<1||maxDistance>12)throw new Exception("Miniature simulation scale invalid: "+maxDistance);
                File.WriteAllText("Deliverables/CompanyLocomotiveV2/Validation/unity_smoke.txt",
                    "Passed editor particle simulation and rendering.\nAlive at steady state: "+alive+"; maximum 48/train, 240/five trains.\nOne shared 512 atlas and unlit material; no shadows, collision, distortion or depth-texture requirement.\nBoard simulation space, lingering puffs, engine-off dissipation, distance cull/resume and miniature scale checked.\nNo on-headset performance claim.\n");
            }
            finally{preview.Cleanup();if(board)UnityEngine.Object.DestroyImmediate(board);}
        }
    }
}
