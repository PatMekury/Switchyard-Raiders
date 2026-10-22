using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwitchyardRaiders.CompanyTrain
{
    /// <summary>Rigid locomotive mechanism, driven by rail distance in original asset metres.
    /// Feed signed authoritative distance, not world-space movement: moving/resizing the
    /// tabletop must not rotate the wheels. Positive distance means forward.</summary>
    [DisallowMultipleComponent]
    public sealed class CompanyLocomotiveMotion : MonoBehaviour
    {
        public enum MotionDriver { AnimatorController, RailDistance }
        [Tooltip("AnimatorController plays the imported loop. SetTravelDistance switches to exact rail-distance motion.")]
        public MotionDriver motionDriver = MotionDriver.AnimatorController;
        public bool previewRolling;
        public float previewMetresPerSecond = 2.591814f;
        [SerializeField] private double travelledMetres;
        private Transform body;
        private Vector3 bx, by, bz;
        private float handedness;
        private Animator trainAnimator;
        private static readonly int PlaybackSpeed = Animator.StringToHash("PlaybackSpeed");
        private readonly Dictionary<string, Rest> parts = new Dictionary<string, Rest>();
        private struct Rest { public Transform transform; public Vector3 position; public Quaternion rotation; }
        public double TravelledMetres => travelledMetres;

        private void Awake() { Initialize(); if (motionDriver == MotionDriver.RailDistance) Evaluate(); }
        private void LateUpdate()
        {
            if (trainAnimator) trainAnimator.enabled = motionDriver == MotionDriver.AnimatorController;
            if (motionDriver == MotionDriver.AnimatorController) return;
            if (previewRolling) travelledMetres += previewMetresPerSecond * Time.deltaTime;
            Evaluate();
        }

        public void Initialize()
        {
            if (parts.Count != 0) return;
            var nodes = GetComponentsInChildren<Transform>(true);
            Transform Find(string name) => Array.Find(nodes, t => t.name == name);
            body = Find("Body");
            var origin = Find("Basis_Origin");
            var x = Find("Basis_X"); var y = Find("Basis_Y"); var z = Find("Basis_Z");
            if (!body || !origin || !x || !y || !z)
            {
                Debug.LogError("Company locomotive requires its exported rig and basis markers.", this);
                enabled = false; return;
            }
            bx = body.InverseTransformVector(x.position - origin.position);
            by = body.InverseTransformVector(y.position - origin.position);
            bz = body.InverseTransformVector(z.position - origin.position);
            handedness = Mathf.Sign(Vector3.Dot(Vector3.Cross(bx, by), bz));
            foreach (var t in nodes)
                if (t.parent == body && (t.name.StartsWith("Drive") || t.name.StartsWith("Pilot") ||
                    t.name.StartsWith("SideRod_") || t.name.StartsWith("Crosshead_") || t.name.StartsWith("MainRod_") ||
                    t.name.StartsWith("EccentricRod_") || t.name.StartsWith("ExpansionLink_") || t.name.StartsWith("RadiusRod_") || t.name.StartsWith("ValveStem_")))
                    parts[t.name] = new Rest { transform = t, position = t.localPosition, rotation = t.localRotation };
            trainAnimator = GetComponentInChildren<Animator>(true);
            if (trainAnimator) trainAnimator.enabled = motionDriver == MotionDriver.AnimatorController;
        }

        public void SetTravelDistance(double signedForwardMetres)
        {
            if (double.IsNaN(signedForwardMetres) || double.IsInfinity(signedForwardMetres)) return;
            Initialize();
            motionDriver = MotionDriver.RailDistance;
            if (trainAnimator) trainAnimator.enabled = false;
            travelledMetres = signedForwardMetres; Evaluate();
        }

        /// <summary>Resume the attached imported loop. Speed 1 is forward, 0 pauses,
        /// and -1 reverses. Root movement still belongs to the track/path system.</summary>
        public void UseAnimatorPlayback(float speed = 1f)
        {
            if (float.IsNaN(speed) || float.IsInfinity(speed)) return;
            Initialize();
            if (!trainAnimator || !trainAnimator.runtimeAnimatorController) return;
            motionDriver = MotionDriver.AnimatorController;
            trainAnimator.enabled = true;
            trainAnimator.SetFloat(PlaybackSpeed, speed);
            double cycleMetres = 20 * 2 * Math.PI * .825;
            double phase = (travelledMetres % cycleMetres + cycleMetres) % cycleMetres / cycleMetres;
            trainAnimator.Play("Base Layer.Rolling", 0, (float)phase);
        }

        public void AddTravelDistance(double signedForwardMetres) => SetTravelDistance(travelledMetres + signedForwardMetres);

        private void Evaluate()
        {
            if (parts.Count == 0) return;
            // Reduce doubles before conversion to float, avoiding long-session wheel jitter.
            double theta = Math.IEEERemainder(-travelledMetres / .825, Math.PI * 2);
            foreach (var pair in parts)
            {
                string name = pair.Key; Rest rest = pair.Value;
                Vector3 shift = Vector3.zero; double angle = 0;
                if (name.StartsWith("Drive")) angle = theta;
                else if (name.StartsWith("Pilot")) angle = Math.IEEERemainder(-travelledMetres / .5, Math.PI * 2);
                else
                {
                    double phase = name.EndsWith("_L") ? 0 : Math.PI / 2;
                    double px = .315 * Math.Cos(theta + phase), dz = -.315 * Math.Sin(theta + phase);
                    double px0 = .315 * Math.Cos(phase), dz0 = -.315 * Math.Sin(phase);
                    double cx = px - Math.Sqrt(2.85 * 2.85 - dz * dz);
                    double cx0 = px0 - Math.Sqrt(2.85 * 2.85 - dz0 * dz0);
                    if (name.StartsWith("EccentricRod_") || name.StartsWith("ExpansionLink_") || name.StartsWith("RadiusRod_") || name.StartsWith("ValveStem_"))
                    {
                        ValveEndpoints(name, theta, phase, out var a, out var b);
                        ValveEndpoints(name, 0, phase, out var a0, out var b0);
                        Vector2 delta = (a + b - a0 - b0) * .5f;
                        shift = bx * delta.x + bz * delta.y;
                        angle = -Math.Atan2(b.y - a.y, b.x - a.x) + Math.Atan2(b0.y - a0.y, b0.x - a0.x);
                    }
                    else if (name.StartsWith("SideRod_")) shift = bx * (float)(px - px0) + bz * (float)(dz - dz0);
                    else if (name.StartsWith("Crosshead_")) shift = bx * (float)(cx - cx0);
                    else if (name.StartsWith("MainRod_"))
                    {
                        shift = bx * (float)((cx + px - cx0 - px0) / 2) + bz * (float)((dz - dz0) / 2);
                        angle = -Math.Atan2(dz, px - cx) + Math.Atan2(dz0, px0 - cx0);
                    }
                }
                rest.transform.localPosition = rest.position + shift;
                rest.transform.localRotation = Quaternion.AngleAxis((float)(angle * Mathf.Rad2Deg) * handedness, by.normalized) * rest.rotation;
            }
        }

        private static void ValveEndpoints(string name, double theta, double phase, out Vector2 a, out Vector2 b)
        {
            double angle = theta + phase + Math.PI / 2;
            var eccentric = new Vector2((float)(.28 * Math.Cos(angle)), (float)(.825 - .28 * Math.Sin(angle)));
            var pivot = new Vector2(-1.9f, 1.4f);
            Vector2 delta = eccentric - pivot; double d = delta.magnitude;
            double along = (.32 * .32 - 1.99 * 1.99 + d * d) / (2 * d);
            double h = Math.Sqrt(Math.Max(0, .32 * .32 - along * along));
            Vector2 u = delta / (float)d, mid = pivot + (float)along * u;
            var perp = new Vector2(-u.y, u.x);
            Vector2 q1 = mid + (float)h * perp, q2 = mid - (float)h * perp;
            Vector2 q = q1.y < q2.y ? q1 : q2, top = 2 * pivot - q;
            var slider = new Vector2(top.x - (float)Math.Sqrt(1.6 * 1.6 - (top.y - 1.35) * (top.y - 1.35)), 1.35f);
            if (name.StartsWith("EccentricRod_")) { a = eccentric; b = q; }
            else if (name.StartsWith("ExpansionLink_")) { a = q; b = top; }
            else if (name.StartsWith("RadiusRod_")) { a = top; b = slider; }
            else { a = slider; b = slider + new Vector2(-.75f, 0); }
        }
    }
}
