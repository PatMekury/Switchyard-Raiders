using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    // Presentation only: no railway distances or network state enter this calculation.
    public static class TabletopViewMath
    {
        public static float ClampScale(float scale,float normal)
        {
            if(float.IsNaN(scale)||float.IsInfinity(scale))return normal;
            return Mathf.Clamp(scale,normal,normal*TabletopViewContract.MaximumZoom);
        }
        public static Pose ReachablePose(Transform head)
        {
            var forward = Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.ProjectOnPlane(head.up,Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            return new Pose(head.position+forward*TabletopViewContract.PlacementDistance+
                Vector3.down*(TabletopViewContract.RimBelowEyes+TabletopViewContract.RimHeight),Quaternion.LookRotation(forward));
        }
        public static void Solve(Vector3 startPosition, Quaternion startRotation, float startScale,
            Vector3 startLeft, Vector3 startRight, Vector3 left, Vector3 right,
            float minimum, float maximum, out Vector3 position, out Quaternion rotation, out float scale)
        {
            SolveAtPivot(startPosition,startRotation,startScale,Pivot(startPosition,startRotation,startScale,(startLeft+startRight)*.5f),
                startLeft,startRight,left,right,minimum,maximum,out position,out rotation,out scale,out _,out _);
        }

        public static bool Finite(Vector3 p)=>!float.IsNaN(p.x+p.y+p.z)&&!float.IsInfinity(p.x+p.y+p.z);
        public static float PlanarSpan(Vector3 a,Vector3 b)=>Vector3.ProjectOnPlane(b-a,Vector3.up).magnitude;
        public static Vector3 Pivot(Vector3 position,Quaternion rotation,float scale,Vector3 midpoint)
        {
            var local=Quaternion.Inverse(rotation)*(midpoint-position)/scale;
            float half=TabletopViewContract.UsableLandSize*.5f;
            local=new Vector3(Mathf.Clamp(local.x,-half,half),TabletopViewContract.TerrainDatum,Mathf.Clamp(local.z,-half,half));
            return position+rotation*local*scale;
        }
        public static void SolveAtPivot(Vector3 startPosition,Quaternion startRotation,float startScale,Vector3 pivot,
            Vector3 startLeft,Vector3 startRight,Vector3 left,Vector3 right,float minimum,float maximum,
            out Vector3 position,out Quaternion rotation,out float scale,out Vector3 translation,out Quaternion turn)
        {
            var before=Vector3.ProjectOnPlane(startRight-startLeft,Vector3.up);
            var after=Vector3.ProjectOnPlane(right-left,Vector3.up);
            bool span=before.magnitude>=.08f && after.magnitude>=.08f;
            turn=span?Quaternion.AngleAxis(Vector3.SignedAngle(before,after,Vector3.up),Vector3.up):Quaternion.identity;
            scale=Mathf.Clamp(span?startScale*after.magnitude/before.magnitude:startScale,minimum,maximum);
            translation=(left+right-startLeft-startRight)*.5f;
            float ly=left.y-startLeft.y,ry=right.y-startRight.y;
            // Only the vertical motion shared by BOTH hands counts. Opposing height changes,
            // one raised hand and rotation of a sloped hand segment cannot lift the board.
            translation.y=ly*ry>0?Mathf.Sign(ly)*Mathf.Min(Mathf.Abs(ly),Mathf.Abs(ry)):0;
            position=pivot+translation+turn*(startPosition-pivot)*(scale/startScale);
            rotation=turn*startRotation;
        }
        public static Pose RigidMotion(Pose pose,Vector3 pivot,Vector3 translation,Quaternion yaw)=>
            new Pose(pivot+translation+yaw*(pose.position-pivot),yaw*pose.rotation);

        public static Vector3 SeatPosition(int slot, int count)
        {
            float distance=TabletopViewContract.PlacementDistance;
            if (count == 2) return slot == 0 ? new Vector3(0, -.75f, -distance) : new Vector3(0, -.75f, distance);
            return slot switch { 0 => new Vector3(0,-.75f,-distance), 1 => new Vector3(distance,-.75f,0),
                2 => new Vector3(0,-.75f,distance), _ => new Vector3(-distance,-.75f,0) };
        }
    }
}
