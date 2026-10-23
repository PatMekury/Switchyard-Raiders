using System;
using System.IO;
using UnityEditor;
using SwitchyardRaiders.Startup;

namespace SwitchyardRaiders.Editor
{
    public static class RaidersRulesChecks
    {
        private static int passed;
        private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);passed++;}
        [MenuItem("Switchyard Raiders/Main Menu/Check match rules (Edit Mode)")]
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Run outside Play Mode.");
            passed=0;var m=new RaidersMatch();
            Check(!ReferenceEquals(m.players[0],m.players[1]),"Player state must be independent");
            Check(m.Apply(0,RaidersAction.Throttle,0,1,out _),"P1 drive rejected");
            Check(m.Apply(1,RaidersAction.Throttle,0,-1,out _),"P2 drive rejected");
            float a=m.players[0].distance,b=m.players[1].distance;m.Step(.02f);
            Check(m.players[0].distance!=a && m.players[1].distance!=b,"Simulation must advance both trains simultaneously");
            Check(!m.Apply(2,RaidersAction.Brake,0,0,out _),"Invalid logical player accepted");
            Check(!m.Apply(0,RaidersAction.Throttle,0,float.NaN,out _),"NaN accepted");
            Check(m.Apply(0,RaidersAction.Place,0,0,out _),"Legal emplacement rejected");
            Check(m.players[0].devices==5 && m.players[1].devices==6,"Inventory crossed player ownership");
            Check(!m.Apply(1,RaidersAction.Aim,0,.5f,out _),"Opponent aimed owned device");
            Check(!m.Apply(1,RaidersAction.Fire,0,0,out _),"Opponent fired owned device");
            for(int i=0;i<m.cargo.Length;i++)m.cargo[i].owner=-2;
            m.cargo[2].distance=RaidersMatch.Site(0)-RaidersMatch.Spacing*.5f;
            Check(m.Apply(0,RaidersAction.Fire,0,0,out _),"Full-window fire rejected");
            Check(m.devices[0].spent && m.cargo[2].owner==-1 && m.cargo[5].owner==-1 && m.cargo[1].owner==-2,"Cut must release the downstream chain");
            Check(!m.Apply(0,RaidersAction.Fire,0,0,out _),"Spent device fired twice");
            m.Reset(false);
            for(int i=0;i<3;i++){m.cargo[i].owner=1;m.cargo[i].order=i+1;}
            m.players[1].distance=17;m.Step(.02f);
            Check(m.winner==1 && m.players[1].score==3 && m.players[0].score==0,"Complete home delivery did not resolve P2 victory");
            Check(!m.Apply(0,RaidersAction.Throttle,0,1,out _),"Input accepted after match completion");
            m.Reset(false);
            Check(m.winner==-1 && m.players[0].score==0 && m.players[1].score==0 && m.players[0].devices==6 && m.cargo[0].owner==-4,"Rematch retained stale state");
            m.Reset(true);Check(!m.Apply(1,RaidersAction.Throttle,0,1,out _),"Practice opponent accepted input");
            Directory.CreateDirectory("Deliverables/MainMenuFlow");
            File.WriteAllText("Deliverables/MainMenuFlow/rules-checks.txt",passed+" Edit Mode assertions passed. Production RaidersMatch model only; no Play Mode, network, UI or headset execution.\n");
        }
    }
}
