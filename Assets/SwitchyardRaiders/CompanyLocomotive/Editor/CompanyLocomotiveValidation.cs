using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace SwitchyardRaiders.CompanyTrain.Editor
{
    public static class CompanyLocomotiveValidation
    {
        [MenuItem("Switchyard Raiders/Company Locomotive/Validate and render prefab")]
        public static void Run()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SwitchyardRaiders/CompanyLocomotive/Prefabs/CompanyLocomotive.prefab");
            var go = UnityEngine.Object.Instantiate(source);
            var preview = new PreviewRenderUtility();
            try
            {
                var motion = go.GetComponent<CompanyLocomotiveMotion>(); motion.Initialize(); motion.SetTravelDistance(0);
                var nodes = go.GetComponentsInChildren<Transform>(true).ToDictionary(t => t.name, t => t);
                Vector3 origin = nodes["Basis_Origin"].position;
                Vector3 bx = nodes["Basis_X"].position-origin, by=nodes["Basis_Y"].position-origin, bz=nodes["Basis_Z"].position-origin;
                Vector3 B(float x,float y,float z) => origin+bx*x+by*y+bz*z;
                var pins = new List<(Transform a, Vector3 aLocal, Transform b, Vector3 bLocal)>();
                void Pin(string a,string b,Vector3 world)
                { pins.Add((nodes[a],nodes[a].InverseTransformPoint(world),nodes[b],nodes[b].InverseTransformPoint(world))); }
                foreach (string side in new[] { "L", "R" })
                {
                    double phase=side=="L" ? 0 : Math.PI/2;
                    float y=side=="L" ? -1.25f : 1.25f;
                    float px=(float)(.315*Math.Cos(phase)), pz=(float)(.825-.315*Math.Sin(phase));
                    float cx=px-Mathf.Sqrt(2.85f*2.85f-(pz-.825f)*(pz-.825f));
                    Pin("Drive1","MainRod_"+side,B(px,y,pz));
                    Pin("Crosshead_"+side,"MainRod_"+side,B(cx,y,.825f));
                    float gy=side=="L" ? -1.54f : 1.54f;
                    Vector3 shared=B((float)(.28*Math.Cos(phase+Math.PI/2)),gy,(float)(.825-.28*Math.Sin(phase+Math.PI/2)));
                    Pin("Drive1","EccentricRod_"+side,shared);
                    shared=2*nodes["EccentricRod_"+side].position-shared;
                    Pin("EccentricRod_"+side,"ExpansionLink_"+side,shared);
                    shared=2*nodes["ExpansionLink_"+side].position-shared;
                    Pin("ExpansionLink_"+side,"RadiusRod_"+side,shared);
                    shared=2*nodes["RadiusRod_"+side].position-shared;
                    Pin("RadiusRod_"+side,"ValveStem_"+side,shared);
                }
                double maxError=0;
                foreach (float scale in new[] { 1f,.05f,2f })
                {
                    go.transform.localScale=Vector3.one*scale;
                    go.transform.rotation=Quaternion.Euler(13,27,-6);
                    for(int i=-360;i<=360;i++)
                    {
                        motion.SetTravelDistance(i*Math.PI*.825/180);
                        foreach(var pin in pins)
                            maxError=Math.Max(maxError,Vector3.Distance(pin.a.TransformPoint(pin.aLocal),pin.b.TransformPoint(pin.bLocal))/scale);
                    }
                }
                if(maxError>.0002) throw new Exception("Mechanical joint drift: "+maxError+" metres");
                // Joint agreement alone cannot detect a train skating backwards.
                // At the bottom of every wheel, signed root travel must cancel rim
                // travel. Check actual imported transforms in forward and reverse.
                double maximumContactSlip=0;
                foreach(float scale in new[]{1f,.05f,2f})
                {
                    go.transform.localScale=Vector3.one*scale;
                    go.transform.rotation=Quaternion.Euler(13,27,-6);
                    Vector3 forward=go.transform.forward,up=go.transform.up,right=go.transform.right;
                    foreach(double distance in new[]{-8.0,-1.3,0.0,2.6,9.0})
                    foreach(var wheel in nodes.Values.Where(t=>t.name.StartsWith("Drive") || t.name.StartsWith("Pilot")))
                    {
                        float radius=wheel.name.StartsWith("Drive") ? .825f : .5f;
                        const double epsilon=.005;
                        go.transform.position=forward*(float)(distance*scale);motion.SetTravelDistance(distance);
                        Vector3 contactLocal=wheel.InverseTransformPoint(wheel.position-up*radius*scale);
                        go.transform.position=forward*(float)((distance-epsilon)*scale);motion.SetTravelDistance(distance-epsilon);
                        Vector3 before=wheel.TransformPoint(contactLocal);
                        go.transform.position=forward*(float)((distance+epsilon)*scale);motion.SetTravelDistance(distance+epsilon);
                        Vector3 after=wheel.TransformPoint(contactLocal);
                        Vector3 travel=(after-before)/(float)(2*epsilon*scale);
                        maximumContactSlip=Math.Max(maximumContactSlip,new Vector2(Vector3.Dot(travel,forward),Vector3.Dot(travel,right)).magnitude);
                    }
                }
                if(maximumContactSlip>.002) throw new Exception("Wrong wheel direction or slip: "+maximumContactSlip+" m per metre");
                go.transform.position=Vector3.zero;
                go.transform.localScale=Vector3.one;go.transform.rotation=Quaternion.identity;motion.SetTravelDistance(0);
                if(go.GetComponentsInChildren<LODGroup>(true).Length!=1) throw new Exception("Duplicate LODGroup");
                var group=go.GetComponent<LODGroup>();
                foreach(var lod in group.GetLODs())
                    if(lod.renderers.Length!=1 || lod.renderers[0].sharedMaterials.Length!=1) throw new Exception("Unexpected draw/material count");
                preview.AddSingleGO(go);
                preview.camera.orthographic=true;preview.camera.orthographicSize=7.6f;
                preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=100;
                preview.camera.transform.position=new Vector3(15,9,19);
                preview.camera.transform.LookAt(new Vector3(0,1.9f,0));
                preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.055f,.07f,.085f);
                preview.lights[0].intensity=1.8f;preview.lights[0].transform.rotation=Quaternion.Euler(40,210,0);
                preview.lights[1].intensity=1.1f;preview.lights[1].transform.rotation=Quaternion.Euler(40,20,0);
                preview.ambientColor=new Color(.4f,.4f,.4f);
                foreach(int lod in new[]{0,1,2})
                {
                    group.ForceLOD(lod);
                    preview.BeginStaticPreview(new Rect(0,0,1400,900));preview.Render(true);
                    var texture=preview.EndStaticPreview();
                    File.WriteAllBytes("Deliverables/CompanyLocomotiveV2/Preview/Unity_LOD"+lod+".png",texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                File.WriteAllText("Deliverables/CompanyLocomotiveV2/Validation/unity_animation.json",
                    "{\n  \"passed\": true,\n  \"joint_samples\": 25956,\n  \"maximum_joint_gap_metres\": "+maxError.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\n  \"tested_scales\": [1,0.05,2],\n  \"reverse_and_forward\": true,\n  \"maximum_contact_slip_metres_per_metre\": "+maximumContactSlip.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\n  \"wheel_contact_samples\": 105,\n  \"forward_axis\": \"prefab +Z, smokebox first\",\n  \"lod_groups\": 1\n}\n");
                Debug.Log("Company locomotive: all imported linkage joint checks passed; max gap "+maxError+" m. Unity preview renders saved.");
            }
            finally { preview.Cleanup(); if(go) UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
