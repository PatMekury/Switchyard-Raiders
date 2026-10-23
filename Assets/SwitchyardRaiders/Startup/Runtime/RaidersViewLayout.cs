using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    // Content placement targets, never overrides for the runtime's stereo camera projection.
    // 52 x 48 leaves a margin inside Meta's 58 x 54 perceived FoV test target.
    public static class RaidersViewLayout
    {
        public const float HorizontalHalfAngle = 26f;
        public const float VerticalHalfAngle = 24f;
        public const float YardDistance = 2.3f;
        public static bool YardFitsView(Vector3 eye, Vector3 forward, Vector3 centre, Quaternion rotation)
        {
            var view = Quaternion.Inverse(Quaternion.LookRotation(forward, Vector3.up));
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var p = view * (centre + rotation * new Vector3(x * .81f, -.05f, z * .58f) - eye);
                if (p.z <= 0 || Mathf.Abs(Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg) > HorizontalHalfAngle ||
                    Mathf.Abs(Mathf.Atan2(p.y, p.z) * Mathf.Rad2Deg) > VerticalHalfAngle) return false;
            }
            return true;
        }
    }
}
