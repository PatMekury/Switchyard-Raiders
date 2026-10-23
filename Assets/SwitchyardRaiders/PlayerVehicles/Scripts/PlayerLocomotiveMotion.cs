using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwitchyardRaiders.PlayerVehicles
{
    /// <summary>Asset metres, rail-relative signed distance. Board motion never drives wheels.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerLocomotiveMotion : MonoBehaviour
    {
        public enum Driver { RailDistance, Animator }
        public Driver driver = Driver.RailDistance;
        public const float WheelRadius = .60f;
        [SerializeField] private double distance;
        private Animator animator;
        private readonly Dictionary<string, Rest> parts = new();
        private struct Rest { public Transform node; public Quaternion rotation; }
        public double Distance => distance;
        public Animator Animator { get { Initialize(); return animator; } }
        private void Awake() { Initialize(); if(driver==Driver.RailDistance)SetTravelDistance(distance); }
        public void Initialize()
        {
            if (parts.Count != 0) return;
            animator = GetComponentInChildren<Animator>(true);
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Wheel_") || t.name.StartsWith("Bogie_") ||
                    t.name.StartsWith("Door_") || t.name.StartsWith("Control_") || t.name.StartsWith("CouplingPivot_"))
                    parts[t.name] = new Rest { node = t, rotation = t.localRotation };
        }
        private void Rotate(string name, Vector3 worldAxis, float angle)
        {
            if (!parts.TryGetValue(name, out var p)) return;
            var axis = p.node.parent.InverseTransformDirection(worldAxis).normalized;
            p.node.localRotation = Quaternion.AngleAxis(angle, axis) * p.rotation;
        }
        public void SetTravelDistance(double signedMetres)
        {
            if (double.IsNaN(signedMetres) || double.IsInfinity(signedMetres)) return;
            Initialize(); driver = Driver.RailDistance;
            if (animator) animator.enabled = false;
            distance = signedMetres;
            float degrees = (float)Math.IEEERemainder(distance / WheelRadius, Math.PI * 2) * Mathf.Rad2Deg;
            // Unity +X cross -Y = -Z: the bottom of each wheel cancels forward travel.
            foreach (var p in parts)
                if (p.Key.StartsWith("Wheel_"))
                {
                    Transform bogie = p.Value.node.parent;
                    // Imported bone axes are arbitrary. Use the yawed prefab transverse axis.
                    var delta = bogie.localRotation * Quaternion.Inverse(parts[bogie.name].rotation);
                    var axle = bogie.parent.TransformDirection(delta * bogie.parent.InverseTransformDirection(transform.right));
                    Rotate(p.Key, axle, degrees);
                }
        }
        public void AddTravelDistance(double metres) => SetTravelDistance(distance + metres);
        public void SetBogieYaw(float frontDegrees, float rearDegrees)
        {
            Initialize();
            Rotate("Bogie_Front", transform.up, Mathf.Clamp(frontDegrees, -30, 30));
            Rotate("Bogie_Rear", transform.up, Mathf.Clamp(rearDegrees, -30, 30));
            if (driver == Driver.RailDistance) SetTravelDistance(distance);
        }
        public void SetDoorOpen(bool front, bool left, float fraction)
        {
            Initialize(); if (driver == Driver.Animator) SetTravelDistance(distance);
            Rotate("Door_" + (front ? "Front" : "Rear") + (left ? "_L" : "_R"), transform.up,
                (front ? 1 : -1) * (left ? -1 : 1) * Mathf.Clamp01(fraction) * 78);
        }
        public void SetControl(bool front, float fraction)
        {
            Initialize(); if (driver == Driver.Animator) SetTravelDistance(distance);
            Rotate("Control_" + (front ? "Front" : "Rear"), transform.up, Mathf.Clamp(fraction, -1, 1) * 40);
        }
        public void SetCouplingYaw(bool front, float degrees)
        {
            Initialize(); if(driver==Driver.Animator)SetTravelDistance(distance);
            Rotate("CouplingPivot_" + (front ? "Front" : "Rear"), transform.up, Mathf.Clamp(degrees, -30, 30));
        }
        /// <summary>Explicit reverse take always plays at positive speed. Zero pauses.</summary>
        public void UseAnimatorPlayback(float signedSpeed = 1)
        {
            if (float.IsNaN(signedSpeed) || float.IsInfinity(signedSpeed)) return;
            Initialize(); if (!animator || !animator.runtimeAnimatorController) return;
            if(driver==Driver.Animator && animator.enabled && signedSpeed==0){animator.speed=0;return;}
            string state=signedSpeed<0?"Reverse":"Forward";
            float phase=(float)((distance/(Math.PI*2*WheelRadius)%1+1)%1);
            if(driver==Driver.Animator&&animator.enabled)
            {
                var current=animator.GetCurrentAnimatorStateInfo(0);
                if(current.IsName(state)){animator.speed=Mathf.Abs(signedSpeed);return;}
                if(current.IsName("Forward")||current.IsName("Reverse"))phase=Mathf.Repeat(current.normalizedTime,1);
                if(current.IsName("Reverse"))phase=1-phase;
            }
            if(signedSpeed<0)phase=1-phase;
            driver = Driver.Animator; animator.enabled = true; animator.speed = Mathf.Abs(signedSpeed);
            animator.Play(state, 0, phase);
        }
        public void PlayInteraction(string state)
        {
            if (state != "Doors_Open" && state != "Doors_Close" && state != "Controls") return;
            Initialize(); if (!animator) return;
            driver = Driver.Animator; animator.enabled = true; animator.speed = 1; animator.Play(state, 0, 0);
        }
    }
}
