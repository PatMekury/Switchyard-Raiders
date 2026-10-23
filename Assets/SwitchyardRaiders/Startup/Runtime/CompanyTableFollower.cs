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
        {
            if(!table || table.session.Match==null || !motion)return;
            float d=table.session.Match.companyDistance;
            if(sampled)
            {
                float delta=RaidersMatch.Delta(d,previous);
                travel+=Vector3.Distance(RaidersMatch.Point(previous),RaidersMatch.Point(previous+delta))*Mathf.Sign(delta)/Mathf.Max(.00001f,transform.localScale.x);
            }
            sampled=true;previous=d;motion.SetTravelDistance(travel);
        }
        private void OnDisable(){sampled=false;travel=0;}
    }
}
