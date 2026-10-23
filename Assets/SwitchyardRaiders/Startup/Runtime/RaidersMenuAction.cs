using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SwitchyardRaiders.Startup
{
    public sealed class RaidersMenuAction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RaidersMenu menu;
        public Text label;
        public string command;
        private int pressedSlot=-1;
        private string pressedCommand;
        private int pressedEpoch;
        private readonly System.Collections.Generic.HashSet<int> pointers=new();
        public void OnPointerDown(PointerEventData data)
        {
            if(!GetComponent<Button>().IsInteractable() || (menu.table.manipulation && menu.table.manipulation.BlocksGameplay))return;
            pressedEpoch=menu.table.manipulation?menu.table.manipulation.InteractionEpoch:0;
            pressedSlot=menu.session.ControlledSlot;pressedCommand=command;pointers.Add(data.pointerId);Hold();
        }
        public void OnPointerUp(PointerEventData data){pointers.Remove(data.pointerId);Hold();}
        public void OnPointerExit(PointerEventData data){pointers.Remove(data.pointerId);Hold();}
        private void Hold(){if(menu && menu.main && menu.main.roomMenu.follower)menu.main.roomMenu.follower.HoldFor(this,pointers.Count>0);}
        private void OnDisable(){pointers.Clear();Hold();pressedSlot=-1;pressedCommand=null;}
        public void Invoke()
        {
            if(menu.table.manipulation && (menu.table.manipulation.BlocksGameplay || pressedEpoch!=menu.table.manipulation.InteractionEpoch))return;
            if(pressedCommand!=null && (pressedSlot!=menu.session.ControlledSlot || pressedCommand!=command))return;
            var sound=GetComponent<MenuButtonAudio>();
            if(sound && sound.audioBus)sound.clickClip=command=="back" || command=="leave" || command=="room"?sound.audioBus.backClip:sound.audioBus.confirmClip;
            menu.Invoke(command);
        }
    }
}
