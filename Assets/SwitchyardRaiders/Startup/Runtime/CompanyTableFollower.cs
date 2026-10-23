using UnityEngine;
using SwitchyardRaiders.CompanyTrain;
namespace SwitchyardRaiders.Startup
{
    [DefaultExecutionOrder(210)]
    public sealed class CompanyTableFollower : MonoBehaviour
    {
        public RaidersTable table;
        private CompanyLocomotiveMotion motion;
        private float previous, travel;
        private bool sampled;
        private void Awake()=>motion=GetComponent<CompanyLocomotiveMotion>();
        private void LateUpdate()
        { Evaluate(); }
        public static void FitPilotAxles(Transform train,CompanyLocomotiveMotion motion,float simulationDistance)
        {
            float scale=train.localScale.x;
            for(int i=0;i<4;i++)
            {
                float z=i==0?5.2f:i==1?3.85f:i==2?-4.1f:-5.45f;
                float at=simulationDistance+TabletopRailway.EngineOffset+z*scale/TabletopRailway.MetresPerUnit;
                var ground=TabletopRailway.Point(at)+Vector3.up*(TabletopViewContract.RailHeadHeight+.5f*scale);
                var local=train.InverseTransformPoint(train.parent.TransformPoint(ground));
                var rotation=Quaternion.Inverse(train.localRotation)*Quaternion.LookRotation(TabletopRailway.Tangent(at),Vector3.up);
                motion.SetPilotTrackPose(i,local-new Vector3(0,.5f,z),rotation);
            }
        }
        public void Evaluate()
        {
            if(!table || table.session.Match==null || !motion)return;
            float d=table.session.Match.companyDistance;
            if(sampled)
            {
                float delta=RaidersMatch.Delta(d,previous);
                travel+=TabletopRailway.ArcLength(previous+TabletopRailway.EngineOffset,delta)/Mathf.Max(.00001f,transform.localScale.x);
            }
            sampled=true;previous=d;FitPilotAxles(transform,motion,d);motion.SetTravelDistance(travel);
        }
        private void OnDisable(){sampled=false;travel=0;}
    }
}
