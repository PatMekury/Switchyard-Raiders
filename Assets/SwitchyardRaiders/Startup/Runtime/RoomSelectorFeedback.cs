using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SwitchyardRaiders.Startup
{
    public sealed class RoomSelectorFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform face;
        public UnityEngine.UI.Image artwork;
        public Text detail;
        public RoomChoiceMenu menu;
        public bool virtualChoice;
        public float arrivalDelay;
        private readonly HashSet<int> hovering=new(), pressing=new();
        private UnityEngine.UI.Button button;
        private Vector3 rest;
        private float age, depth, brightness=.94f;
        private bool selected;
        private void Awake() { rest=face.localPosition; button=GetComponent<UnityEngine.UI.Button>(); }
        private void OnEnable() { age=0; }
        private void OnDisable()
        {
            hovering.Clear(); pressing.Clear();
            if(menu && menu.follower) menu.follower.HoldFor(this,false);
            if(face) face.localPosition=rest;
            depth=0;
        }
        private void Update()
        {
            age+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01((age-arrivalDelay)/.45f);
            float arrival=1-Mathf.Pow(1-t,3);
            float target=pressing.Count>0 ? 3: hovering.Count>0 ? -2:0;
            float rate=1-Mathf.Exp(-22*Time.unscaledDeltaTime);
            depth=Mathf.Lerp(depth,target,rate); if(Mathf.Abs(depth-target)<.001f) depth=target;
            Vector3 position=rest+new Vector3(0,-12*(1-arrival),depth);
            if(face.localPosition!=position) face.localPosition=position;
            bool nowSelected=menu.HasSelection && menu.IsVirtual==virtualChoice;
            if(nowSelected!=selected)
            {
                selected=nowSelected;
                detail.text=selected ? "SELECTED" : virtualChoice ? "A quiet space to play":"Play where you are";
            }
            float targetBrightness=hovering.Count>0 || selected ? 1:.94f;
            brightness=Mathf.Lerp(brightness,targetBrightness,rate);
            if(Mathf.Abs(brightness-targetBrightness)<.001f) brightness=targetBrightness;
            Color color=new(brightness,brightness,brightness,1);
            if(artwork.color!=color) artwork.color=color;
        }
        public void OnPointerEnter(PointerEventData e) => hovering.Add(e.pointerId);
        public void OnPointerExit(PointerEventData e) { hovering.Remove(e.pointerId); pressing.Remove(e.pointerId); menu.follower.HoldFor(this,pressing.Count>0); }
        public void OnPointerDown(PointerEventData e) { if(!button.IsInteractable())return; pressing.Add(e.pointerId);menu.follower.HoldFor(this,true); }
        public void OnPointerUp(PointerEventData e) {pressing.Remove(e.pointerId);menu.follower.HoldFor(this,pressing.Count>0);}
    }
}
