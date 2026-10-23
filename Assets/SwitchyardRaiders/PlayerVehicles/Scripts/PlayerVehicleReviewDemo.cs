using UnityEngine;
namespace SwitchyardRaiders.PlayerVehicles
{
    /// <summary>Review-scene only, deterministic forward / brake / reverse / interactions.</summary>
    public sealed class PlayerVehicleReviewDemo : MonoBehaviour
    {
        public Vector3 basePosition;
        private PlayerLocomotiveMotion motion;
        private float clock;
        private void Awake(){motion=GetComponent<PlayerLocomotiveMotion>();}
        private void Update(){clock+=Time.deltaTime;Evaluate(clock);}
        public void Evaluate(float seconds)
        {
            if(!motion)motion=GetComponent<PlayerLocomotiveMotion>();
            float t=Mathf.Repeat(seconds,14),d=t<3?t:t<5?3:t<8?8-t:0;
            transform.localPosition=basePosition+Vector3.forward*d*transform.localScale.x;
            motion.SetTravelDistance(d);
            float open=t>9&&t<13?Mathf.Clamp01(Mathf.Min(t-9,13-t)):0;
            foreach(bool front in new[]{true,false})foreach(bool left in new[]{true,false})motion.SetDoorOpen(front,left,open);
            motion.SetControl(true,t>9?Mathf.Sin(t*2):0);motion.SetControl(false,t>9?Mathf.Sin(t*2):0);
        }
    }
}
