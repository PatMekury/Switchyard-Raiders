using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    // Presentation only: no railway distances or network state enter this calculation.
    public static class TabletopViewMath
    {
        public static Pose ReachablePose(Transform head)
        {
            var forward = Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.ProjectOnPlane(head.up,Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            // The 1.4m-deep table has its near rim 30cm ahead of the player.
            return new Pose(head.position+forward+Vector3.down*.5f,Quaternion.LookRotation(forward));
        }
        public static void Solve(Vector3 startPosition, Quaternion startRotation, float startScale,
            Vector3 startLeft, Vector3 startRight, Vector3 left, Vector3 right,
            float minimum, float maximum, out Vector3 position, out Quaternion rotation, out float scale)
        {
            var before = Vector3.ProjectOnPlane(startRight - startLeft, Vector3.up);
            var after = Vector3.ProjectOnPlane(right - left, Vector3.up);
            float yaw = before.sqrMagnitude > .0025f && after.sqrMagnitude > .0025f
                ? Vector3.SignedAngle(before, after, Vector3.up) : 0;
            var turn = Quaternion.AngleAxis(yaw, Vector3.up);
            scale = Mathf.Clamp(startScale * Vector3.Distance(left, right) /
                Mathf.Max(.08f, Vector3.Distance(startLeft, startRight)), minimum, maximum);
            var pivot = (startLeft + startRight) * .5f;
            position = (left + right) * .5f + turn * (startPosition - pivot) * (scale / startScale);
            rotation = turn * startRotation;
        }

        public static Vector3 SeatPosition(int slot, int count)
        {
            if (count == 2) return slot == 0 ? new Vector3(0, -.75f, -1.02f) : new Vector3(0, -.75f, 1.02f);
            return slot switch { 0 => new Vector3(0,-.75f,-1.02f), 1 => new Vector3(1.25f,-.75f,0),
                2 => new Vector3(0,-.75f,1.02f), _ => new Vector3(-1.25f,-.75f,0) };
        }
    }
}
