using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    public sealed class TicketArrival : MonoBehaviour
    {
        public float delay;
        private RectTransform rect;
        private Vector2 rest;
        private float elapsed;
        private void Awake() { rect=(RectTransform)transform; rest=rect.anchoredPosition; }
        private void OnEnable() { elapsed=0; enabled=true; }
        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float t=Mathf.Clamp01((elapsed-delay)/.42f);
            float eased=1-Mathf.Pow(1-t,3);
            rect.anchoredPosition=rest+Vector2.down*(1-eased)*22;
            rect.localRotation=Quaternion.Euler(0,0,(1-eased)*(delay>0 ? -3 : 3));
            if(t>=1) { rect.anchoredPosition=rest; rect.localRotation=Quaternion.identity; enabled=false; }
        }
    }
}
