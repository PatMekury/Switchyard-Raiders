using UnityEngine;
using SwitchyardRaiders.Startup;

namespace SwitchyardRaiders.PlayerVehicles
{
    /// <summary>Visual adapter for RaidersMatch's existing rail-distance simulation.
    /// It does not change cargo ownership or implement a second coupling ruleset.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class PlayerVehicleTrackFollower : MonoBehaviour
    {
        public RaidersTable table;
        public int playerSlot;
        private PlayerLocomotiveMotion motion;
        private bool ready;
        private float previous;
        private void Awake() { motion = GetComponent<PlayerLocomotiveMotion>(); }
        private void LateUpdate()
        { Evaluate(); }
        public void Evaluate()
        {
            if (!table || !table.session || table.session.Match == null || !motion || playerSlot>=table.session.Match.playerCount) return;
            float simulationDistance = table.session.Match.players[playerSlot].distance;
            float d = simulationDistance + TabletopRailway.EngineOffset;
            float scale = Mathf.Abs(transform.localScale.x);
            if (scale < .00001f) return;
            if (ready)
            {
                float delta = RaidersMatch.Delta(d, previous);
                // Integrate arc length, not the nonuniform ellipse parameter.
                int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(delta) / .01f), 1, 256);
                float length = 0; Vector3 last = TabletopRailway.Point(previous);
                for (int i = 1; i <= steps; i++)
                { var next = TabletopRailway.Point(previous + delta * i / steps); length += Vector3.Distance(last, next); last = next; }
                motion.AddTravelDistance(Mathf.Sign(delta) * length / scale);
            }
            previous = d; ready = true;
            TabletopRailway.Place(transform,simulationDistance,true,.0415f);
            FitBogies(transform,motion,d);
        }
        public static void FitBogies(Transform train,PlayerLocomotiveMotion motion,float centerDistance)
        {
            float d=centerDistance, scale=train.localScale.x;
            float derivative = Vector3.Distance(TabletopRailway.Point(d - .005f), TabletopRailway.Point(d + .005f)) / .01f;
            float offset = 4.15f * scale / Mathf.Max(.0001f, derivative);
            Vector3 center = (TabletopRailway.Point(d + .005f) - TabletopRailway.Point(d - .005f)).normalized;
            float Yaw(float at) => Vector3.SignedAngle(center,
                (TabletopRailway.Point(at + .005f) - TabletopRailway.Point(at - .005f)).normalized, Vector3.up);
            motion.SetBogieYaw(Yaw(d + offset), Yaw(d - offset));
        }
        private void OnDisable() { ready = false; }
    }
}
