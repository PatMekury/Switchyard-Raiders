using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace SwitchyardRaiders.CompanyTrain.Editor
{
    public static class CompanyLocomotiveControllerValidation
    {
        [MenuItem("Switchyard Raiders/Company Locomotive/Validate Animator Controller")]
        public static void Run()
        {
            const string root="Assets/SwitchyardRaiders/CompanyLocomotive";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/CompanyLocomotive.prefab");
            var go=UnityEngine.Object.Instantiate(prefab);
            try
            {
                var motion=go.GetComponent<CompanyLocomotiveMotion>();motion.Initialize();
                var animator=go.GetComponentInChildren<Animator>(true);
                if(!animator || !animator.enabled || !animator.runtimeAnimatorController)
                    throw new Exception("Prefab must have an enabled, assigned Animator.");
                var controller=animator.runtimeAnimatorController as AnimatorController;
                var state=controller.layers[0].stateMachine.defaultState;
                var clip=state.motion as AnimationClip;
                if(!clip || AssetDatabase.GetAssetPath(clip)!=root+"/Art/CompanyLocomotive.fbx")
                    throw new Exception("Controller must reference the imported FBX animation directly.");
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();animator.Update(0);
                var bones=go.GetComponentsInChildren<Transform>(true)
                    .Where(t=>t.parent && t.parent.name=="Body").ToArray();
                var wheel=bones.Single(t=>t.name=="Drive1");
                const double metresPerSecond=20*2*Math.PI*.825/40;
                double positionError=0,rotationError=0;
                // Use arbitrary subframes: the clip must really bind and interpolate,
                // not merely match at its initial pose or a whole wheel revolution.
                foreach(float time in new[]{0f,.013f,.131f,.51f,1.037f,3.143f,7.777f,16.219f,28.813f,39.99f})
                {
                    motion.UseAnimatorPlayback(0);
                    animator.Play("Base Layer.Rolling",0,time/40);animator.Update(0);
                    var positions=bones.Select(t=>t.localPosition).ToArray();
                    var rotations=bones.Select(t=>t.localRotation).ToArray();
                    motion.SetTravelDistance(time*metresPerSecond);
                    if(animator.enabled || motion.motionDriver!=CompanyLocomotiveMotion.MotionDriver.RailDistance)
                        throw new Exception("Two animation drivers are active together.");
                    for(int i=0;i<bones.Length;i++)
                    {
                        positionError=Math.Max(positionError,Vector3.Distance(positions[i],bones[i].localPosition));
                        rotationError=Math.Max(rotationError,Quaternion.Angle(rotations[i],bones[i].localRotation));
                    }
                }
                // FBX is sampled at 24 Hz; small interpolation error is expected.
                if(positionError>.004 || rotationError>.2) throw new Exception("Imported clip differs from mechanism: "+positionError+" m / "+rotationError+" degrees");
                motion.UseAnimatorPlayback(1);
                animator.Play("Base Layer.Rolling",0,.2f);animator.Update(0);
                Quaternion start=wheel.localRotation;animator.Update(.1f);
                Quaternion forward=Quaternion.Inverse(start)*wheel.localRotation;
                animator.SetFloat("PlaybackSpeed",-1);
                animator.Play("Base Layer.Rolling",0,.2f);animator.Update(0);
                start=wheel.localRotation;animator.Update(.1f);
                Quaternion reverse=Quaternion.Inverse(start)*wheel.localRotation;
                float forwardDegrees=Quaternion.Angle(Quaternion.identity,forward);
                float reverseError=Quaternion.Angle(forward,Quaternion.Inverse(reverse));
                if(forwardDegrees<10 || reverseError>.1f) throw new Exception("Controller forward/reverse playback failed: "+forwardDegrees+" / "+reverseError);
                animator.SetFloat("PlaybackSpeed",0);start=wheel.localRotation;animator.Update(.25f);
                float pauseDrift=Quaternion.Angle(start,wheel.localRotation);
                if(pauseDrift>.01f) throw new Exception("PlaybackSpeed=0 did not pause.");
                File.WriteAllText("Deliverables/CompanyLocomotiveV2/Validation/unity_controller.json",JsonUtility.ToJson(new Report {
                    passed=true,controller=AssetDatabase.GetAssetPath(controller),clip=clip.name,
                    clipSeconds=clip.length,comparedPoses=10,maximumLocalPositionDifference=(float)positionError,
                    maximumRotationDifferenceDegrees=(float)rotationError,forwardStepDegrees=forwardDegrees,
                    reverseSymmetryErrorDegrees=reverseError,pauseDriftDegrees=pauseDrift,
                    exclusiveAnimationDrivers=true,rootMotion=false
                },true));
                Debug.Log("Animator Controller passed: imported clip bound; forward, pause, reverse and exclusive distance mode verified.");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Serializable] private class Report
        {
            public bool passed,exclusiveAnimationDrivers,rootMotion;
            public string controller,clip;
            public int comparedPoses;
            public float clipSeconds,maximumLocalPositionDifference,maximumRotationDifferenceDegrees,forwardStepDegrees,reverseSymmetryErrorDegrees,pauseDriftDegrees;
        }
    }
}
