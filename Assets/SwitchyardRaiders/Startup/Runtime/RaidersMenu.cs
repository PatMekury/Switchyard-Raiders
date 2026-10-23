using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SwitchyardRaiders.Startup
{
    public sealed class RaidersMenu : MonoBehaviour
    {
        public StationMainMenu main;
        public RaidersSession session;
        public RaidersTable table;
        public GameObject screen;
        public Text heading, message, detail;
        public RaidersMenuAction[] actions;
        public RaidersMenuAction[] keypad;
        public CanvasGroup screenGroup;
        private string page="", joinCode="";
        private int site, tutorialStep;
        private float aim, nextDisplay;
        private RaidersSessionPhase previousPhase;
        private Coroutine motion;
        private readonly string[] lessons =
        {
            "Watch the company train. Cargo is scored only after you bring it home.",
            "Select a numbered emplacement and place a decoupler. Aim at the coupling, then fire when its bar is full.",
            "Use slow speed and reverse to approach loose cargo with your rear coupling. A fast impact will not attach it.",
            "Bring the entire cargo chain into your coloured home. Deliver three wagons to finish the exercise."
        };
        private void Awake(){AudioListener.volume=PlayerPrefs.GetFloat("SR.Volume",1);screen.SetActive(false);session.Changed+=Refresh;}
        private void OnDestroy(){session.Changed-=Refresh;}
        public void OpenPublic()
        {
            Show("public","FIND A MATCH");message.text=session.Capacity==4?"2v2 - shared home. Six team deliveries.":"1v1 - three deliveries.";
            Set(0,"Find "+session.Capacity+"-player match","find");Set(1,"Players: "+session.Capacity+" (change)","players");Back();
        }
        public void OpenFriends(){Show("friends","PLAY WITH FRIENDS");message.text="Host a private yard or enter a friend's code.";Set(0,"Host private "+session.Capacity+" players","host");Set(1,"Join with code","code");Set(2,"Players: "+session.Capacity+" (change)","players");Back();}
        public void OpenPractice()
        {
            Show("learn","LEARN & PRACTICE");message.text="Learn to intercept, collect and deliver freight.";
            Set(0,"Learn the controls","lesson");Set(1,"Practice yard","practice");
            if(session.DeveloperTestingEnabled)Set(2,"Developer solo test","testmenu");
            Back();
        }
        public void OpenSettings()
        {
            Show("settings","SETTINGS");message.text="Menu sounds use the SR_Startup mixer.";
            Set(0,AudioListener.volume>.01f?"Sound: on":"Sound: muted","sound");Set(1,"Change room","room");VoiceButtons();Back();
        }
        private void Show(string next,string title)
        {
            main.roomMenu.SetGameplayView(session.Phase == RaidersSessionPhase.Playing);
            if(page!=next && main.roomMenu.menuAudio)main.roomMenu.menuAudio.MenuOpened();
            main.Hide();screen.SetActive(true);page=next;heading.text=title;message.text="";detail.text="";
            foreach(var a in actions)a.gameObject.SetActive(false);
            for(int i=0;i<actions.Length;i++)((RectTransform)actions[i].transform).anchoredPosition=new Vector2(i%2==0?-154:154,28-i/2*77);
            if(next=="code")for(int i=4;i<6;i++)((RectTransform)actions[i].transform).anchoredPosition=new Vector2(i%2==0?-154:154,-166);
            foreach(var a in keypad)a.gameObject.SetActive(false);
            if(motion!=null)StopCoroutine(motion);motion=StartCoroutine(Reveal());
        }
        private IEnumerator Reveal()
        {
            screenGroup.interactable=screenGroup.blocksRaycasts=false;
            float t=0;while(t<.18f){t+=Time.unscaledDeltaTime;screenGroup.alpha=Mathf.SmoothStep(0,1,t/.18f);yield return null;}
            screenGroup.alpha=1;screenGroup.interactable=screenGroup.blocksRaycasts=true;motion=null;
        }
        private void Set(int index,string label,string command)
        {var a=actions[index];a.command=command;a.label.text=label;a.gameObject.SetActive(true);a.GetComponent<Button>().interactable=true;}
        private void Back()=>Set(5,"Back","back");
        private void GoHome()
        {screen.SetActive(false);page="";main.roomMenu.SetGameplayView(false);main.Show();}
        public void Invoke(string command)
        {
            switch(command)
            {
                case "players":session.SelectPlayerCount(session.Capacity==2?4:2);if(page=="public")OpenPublic();else OpenFriends();break;
                case "testmenu":Show("test","SIMULATED PLAYERS");message.text="Local simulation only. No extra Meta accounts or network clients.";Set(0,"Two simulated slots","test2");Set(1,"Four simulated slots (2v2)","test4");Back();break;
                case "test2":session.SelectPlayerCount(2);session.StartEditorTest();break;
                case "test4":session.SelectPlayerCount(4);session.StartEditorTest();break;
                case "find": session.FindMatch();break;
                case "host": session.HostPrivate();break;
                case "code": Show("code","JOIN A PRIVATE YARD");joinCode="";foreach(var a in keypad)a.gameObject.SetActive(true);Set(4,"Join room","join");Back();UpdateCode();break;
                case "join":session.JoinPrivate(joinCode);break;
                case "practice":session.StartPractice();break;
                case "test":session.StartEditorTest();break;
                case "ready":session.ToggleReady();break;
                case "mute":session.voice.ToggleMute();VoiceButtons();break;
                case "voice_retry":session.voice.Retry();break;
                case "reposition":table.ResetView();OpenYard();break;
                case "view":ShowView();break;
                case "resetview":table.ResetView();ShowView();break;
                case "explore":table.ToggleExploration();ShowView();break;
                case "comfort":table.manipulation.comfort=Mathf.Repeat(table.manipulation.comfort+.25f,1.25f);ShowView();break;
                case "leave":session.Leave();break;
                case "retry":session.Retry();break;
                case "rematch":session.Rematch();break;
                case "switch":session.SwitchPlayer();ShowControls();break;
                case "sound":AudioListener.volume=AudioListener.volume>.01f?0:1;PlayerPrefs.SetFloat("SR.Volume",AudioListener.volume);OpenSettings();break;
                case "room":screen.SetActive(false);main.ChangeRoom();break;
                case "lesson":tutorialStep=0;ShowLesson();break;
                case "next":tutorialStep++;if(tutorialStep>=lessons.Length)session.StartPractice();else ShowLesson();break;
                case "back":if(page=="code")OpenFriends();else GoHome();break;
                case "drive":session.Command(RaidersAction.Throttle,0,1);break;
                case "slow":session.Command(RaidersAction.Throttle,0,.2f);break;
                case "brake":session.Command(RaidersAction.Brake);break;
                case "reverse":session.Command(RaidersAction.Reverse);break;
                case "site":site=(site+1)%RaidersMatch.SiteCount;aim=0;break;
                case "place":session.Command(RaidersAction.Place,site);break;
                case "aimleft":aim=Mathf.Clamp(aim-.2f,-1,1);session.Command(RaidersAction.Aim,site,aim);break;
                case "aimright":aim=Mathf.Clamp(aim+.2f,-1,1);session.Command(RaidersAction.Aim,site,aim);break;
                case "fire":session.Command(RaidersAction.Fire,site);break;
                case "controls":if(table.manipulation)table.manipulation.Suspend();ShowControls();break;
                case "devices":ShowDevices();break;
                case "pause":ShowPause();break;
                default:
                    if(command=="erase"){if(joinCode.Length>0)joinCode=joinCode.Substring(0,joinCode.Length-1);UpdateCode();}
                    else if(command.StartsWith("key:") && joinCode.Length<6){joinCode+=command.Substring(4);UpdateCode();}
                    break;
            }
        }
        private void UpdateCode(){message.text=joinCode.PadRight(6,'–');actions[4].GetComponent<Button>().interactable=joinCode.Length==6;}
        private void ShowLesson(){Show("lesson","LEARN THE YARD");message.text=lessons[tutorialStep];detail.text=(tutorialStep+1)+" / "+lessons.Length;Set(0,tutorialStep==lessons.Length-1?"Enter practice yard":"Next","next");Back();}
        public void ResumeSessionView()
        {
            main.roomMenu.panelRoot.SetActive(true);main.roomMenu.group.alpha=1;main.roomMenu.group.interactable=main.roomMenu.group.blocksRaycasts=true;
            if(session.Phase==RaidersSessionPhase.Playing)OpenYard();else Refresh();
        }
        private void ShowPause()
        {
            if(table.manipulation)table.manipulation.Suspend();
            Show("pause","MATCH MENU");message.text="The shared yard keeps running.";
            Set(0,"Train controls","controls");Set(1,"Table view / comfort","view");Set(2,"Switch VR / MR","room");VoiceButtons();
            if(session.CanSwitchPlayers)Set(4,"Next simulated slot","switch");Set(5,"Leave match","leave");
        }
        private void ShowView()
        {
            Show("view","PERSONAL TABLE VIEW");message.text="Touch the table rim with both hands and close both hands to grab. Move, turn, or spread your hands to resize. Open either hand to stop.";
            Set(0,table.roomMenu.IsVirtual?"Recenter table in front":"Return to scanned table","resetview");
            Set(2,table.Exploration?"Return to tabletop":"Explore beyond table","explore");
            Set(3,"Comfort: "+Mathf.RoundToInt(table.manipulation.comfort*100)+"%","comfort");Set(4,"Match menu","pause");Set(5,"Train controls","controls");
        }
        private void Refresh()
        {
            var phase=session.Phase;
            if(phase==RaidersSessionPhase.Idle || phase==RaidersSessionPhase.Failed || phase==RaidersSessionPhase.Leaving)main.roomMenu.AbortPendingSelection();
            if(main.roomMenu.IsOpen && phase!=RaidersSessionPhase.Idle && phase!=RaidersSessionPhase.Failed && phase!=RaidersSessionPhase.Leaving){previousPhase=phase;return;}
            if(phase==RaidersSessionPhase.Idle){table.Hide();GoHome();}
            else if(phase==RaidersSessionPhase.Playing)
            {
                if(previousPhase!=phase){site=0;aim=0;OpenYard();}
            }
            else
            {
                if(phase==RaidersSessionPhase.Ready || phase==RaidersSessionPhase.Waiting)table.Hide();
                Show("session",phase==RaidersSessionPhase.Finished?"DELIVERIES COMPLETE":"DEPARTURE BOARD");
                message.text=session.Status;detail.text=session.RoomCode;
                if (!session.IsLocal && phase != RaidersSessionPhase.Leaving) VoiceButtons();
                if(phase==RaidersSessionPhase.Ready || phase==RaidersSessionPhase.Waiting)
                {Set(0,session.LocalReady?"Not ready":session.CanSwitchPlayers?"Ready all simulated slots":"Ready","ready");
                message.text=session.Status+"\n";for(int i=0;i<session.Capacity;i++)message.text+=session.SlotLabel(i)+(i%2==0?"   ":"\n");}
                if(phase==RaidersSessionPhase.Failed)Set(0,"Retry connection","retry");
                if(phase==RaidersSessionPhase.Finished)Set(0,"Rematch","rematch");
                if(phase!=RaidersSessionPhase.Leaving)Set(5,phase==RaidersSessionPhase.Failed?"Main menu":"Cancel / leave","leave");
                if(phase==RaidersSessionPhase.Countdown && previousPhase!=phase)main.roomMenu.menuAudio.PlayCountdown();
                if(phase==RaidersSessionPhase.Failed || phase==RaidersSessionPhase.Leaving)table.Hide();
            }
            previousPhase=phase;
        }
        private void VoiceButtons()
        {
            if (!session.voice) return;
            Set(3,session.voice.Muted ? "Unmute microphone" : "Mute microphone","mute");
            if (session.voice.Active && !(page=="pause" && session.CanSwitchPlayers)) Set(4,"Retry voice / microphone","voice_retry");
        }
        public void PlacementLost(){if(page!="placement") {Show("placement","TABLETOP UNAVAILABLE");message.text=table.roomUnderstanding.Status;Set(0,"Retry / switch VR","room");Set(5,"Leave match","leave");}}
        private void OpenYard()
        {
            if (table.Show()) { ShowControls(); return; }
            Show("placement","YARD PLACEMENT");
            message.text=table.roomUnderstanding ? table.roomUnderstanding.Status : "Room placement unavailable.";
            Set(0,"Retry / change room","room");Set(5,"Leave yard","leave");
        }
        private void ShowControls()
        {
            Show("drive","LOCOMOTIVE CONTROLS");Set(0,"Forward","drive");Set(1,"Slow approach","slow");Set(2,"Brake","brake");Set(3,"Reverse","reverse");Set(4,"Decouplers","devices");Set(5,"Match menu","pause");
        }
        private void ShowDevices()
        {
            Show("devices","INTERCEPT FREIGHT");Set(0,"Next emplacement","site");Set(1,"Place decoupler","place");Set(2,"Aim left","aimleft");Set(3,"Aim right","aimright");Set(4,"Fire","fire");Set(5,"Train controls","controls");
        }
        private void Update()
        {
            if(!screen.activeSelf || Time.unscaledTime<nextDisplay)return;nextDisplay=Time.unscaledTime+.1f;
            if(session.Phase==RaidersSessionPhase.Countdown)message.text="Departing in "+Mathf.CeilToInt(session.Countdown)+"…";
            if (session.voice && (page=="settings" || page=="session" || page=="pause"))
            {
                detail.text=(page=="session" ? session.RoomCode+"   " : "")+session.voice.Status;
                VoiceButtons();
            }
            if(session.Phase!=RaidersSessionPhase.Playing || session.Match==null)return;
            var m=session.Match;int p=session.ControlledSlot;
            detail.text=(session.CanSwitchPlayers?"SIMULATED ":"")+"P"+(p+1)+"   TEAM 1 "+m.teamScores[0]+"/"+m.Target+"   TEAM 2 "+m.teamScores[1]+"/"+m.Target;
            if(page=="devices")
            {
                message.text="Emplacement "+(site+1)+"   •   "+m.players[p].devices+" devices left\nFiring window: "+Mathf.RoundToInt(m.Window(site)*100)+"%   Aim: "+aim.ToString("0.0");
                actions[4].GetComponent<Button>().interactable=m.devices[site].owner==p && !m.devices[site].spent && m.Window(site)>=.99f;
            }
            else if(page=="drive")message.text="Speed "+m.players[p].speed.ToString("0.0")+"   •   "+session.Status+"\n"+(table.manipulation.Dragging?"Both hands grabbed - move, turn, or spread to resize.":table.manipulation.GrabCount==1?"One hand grabbed - grab the rim with your other hand.":"Touch the rim and close BOTH hands to move / resize.");
            if(page=="pause" && session.voice)detail.text+="\n"+session.voice.Status;
            if(page=="pause" && session.CanSwitchPlayers)Set(4,"Next simulated slot","switch");
        }
    }
}
