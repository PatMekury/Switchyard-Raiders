using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    // Presentation adapter only. The 32-unit simulation and its coupling/firing windows are unchanged.
    public static class TabletopRailway
    {
        public static float MetresPerUnit => 2 * Mathf.PI * TabletopViewContract.RailRadius / RaidersMatch.Length;
        public static float WagonPitch => RaidersMatch.Spacing * MetresPerUnit;
        // Simulation engine distance denotes the same half-pitch coupling reference as a wagon.
        public static float EngineOffset => (TabletopViewContract.LocomotiveHalfLength - WagonPitch * .5f) / MetresPerUnit;
        public static Vector3 Point(float distance)
        {
            float a = distance * 2 * Mathf.PI / RaidersMatch.Length;
            return new Vector3(Mathf.Sin(a) * TabletopViewContract.RailRadius,
                TabletopViewContract.RailRise * (1-Mathf.Cos(2*a)), Mathf.Cos(a) * TabletopViewContract.RailRadius);
        }
        public static Vector3 Tangent(float distance) => (Point(distance+.005f)-Point(distance-.005f)).normalized;
        public static float ArcLength(float from, float delta)
        {
            int steps=Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(delta)/.01f),1,4096);
            float length=0;var last=Point(from);
            for(int i=1;i<=steps;i++){var next=Point(from+delta*i/steps);length+=Vector3.Distance(last,next);last=next;}
            return length*Mathf.Sign(delta);
        }
        public static Pose VehiclePose(float simulationDistance, bool engine, float axleHalfSpan)
        {
            float d=simulationDistance+(engine?EngineOffset:0), offset=axleHalfSpan/MetresPerUnit;
            var front=Point(d+offset);var rear=Point(d-offset);
            // A chord through the axles seats their tyres on the curve, including its grade.
            return new Pose((front+rear)*.5f+Vector3.up*TabletopViewContract.RailHeadHeight,
                Quaternion.LookRotation(front-rear,Vector3.up));
        }
        public static void Place(Transform model,float distance,bool engine,float axleHalfSpan)
        {
            var pose=VehiclePose(distance,engine,axleHalfSpan);model.SetLocalPositionAndRotation(pose.position,pose.rotation);
        }
    }
}
