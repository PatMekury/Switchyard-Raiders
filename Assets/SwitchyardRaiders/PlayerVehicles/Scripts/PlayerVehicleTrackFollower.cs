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
            float d = table.session.Match.players[playerSlot].distance;
            float scale = Mathf.Abs(transform.localScale.x);
            if (scale < .00001f) return;
            if (ready)
            {
                float delta = RaidersMatch.Delta(d, previous);
                // Integrate arc length, not the nonuniform ellipse parameter.
                int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(delta) / .01f), 1, 256);
                float length = 0; Vector3 last = RaidersMatch.Point(previous);
                for (int i = 1; i <= steps; i++)
                { var next = RaidersMatch.Point(previous + delta * i / steps); length += Vector3.Distance(last, next); last = next; }
                motion.AddTravelDistance(Mathf.Sign(delta) * length / scale);
            }
            previous = d; ready = true;
            // RaidersTable places the rail centerline; the current ribbon top is 3mm higher.
            var position=RaidersMatch.Point(d);position.y+=.003f;transform.localPosition=position;
            float derivative = Vector3.Distance(RaidersMatch.Point(d - .005f), RaidersMatch.Point(d + .005f)) / .01f;
            float offset = 4.15f * scale / Mathf.Max(.0001f, derivative);
            Vector3 center = (RaidersMatch.Point(d + .005f) - RaidersMatch.Point(d - .005f)).normalized;
            float Yaw(float at) => Vector3.SignedAngle(center,
                (RaidersMatch.Point(at + .005f) - RaidersMatch.Point(at - .005f)).normalized, Vector3.up);
            motion.SetBogieYaw(Yaw(d + offset), Yaw(d - offset));
        }
        private void OnDisable() { ready = false; }
    }
}
