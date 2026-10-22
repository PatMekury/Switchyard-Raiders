using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    public sealed class RaidersTable : MonoBehaviour
    {
        public RaidersSession session;
        public RoomChoiceMenu roomMenu;
        public RaidersRoom roomUnderstanding;
        public LaunchSequence launch;
        public Transform headset, root, company;
        public Transform[] locomotives, wagons, devices;
        public Transform[] firingBars;
        public Renderer[] wagonMarkers, deviceMarkers;
        public Material neutral, playerOne, playerTwo, spent;
        private bool positioned;
        public bool Show()
        {
            if(!positioned)
            {
                if(launch && launch.headset)headset=launch.headset;
                var forward=Vector3.ProjectOnPlane(headset.forward,Vector3.up).normalized;
                if (roomMenu && !roomMenu.IsVirtual)
                {
                    if (!roomUnderstanding || !roomUnderstanding.TryPlace(headset, out var pose)) return false;
                    root.SetPositionAndRotation(pose.position, pose.rotation);
                }
                else
                {
                    root.position=headset.position+forward*1.2f+Vector3.down*.5f;
                    root.rotation=Quaternion.LookRotation(forward,Vector3.up);
                }
                positioned=true;
            }
            root.gameObject.SetActive(true);
            return true;
        }
        public void Hide(){root.gameObject.SetActive(false);positioned=false;}
        private static void Place(Transform model,float distance)
        {
            model.localPosition=RaidersMatch.Point(distance);
            model.localRotation=Quaternion.LookRotation(RaidersMatch.Point(distance+.02f)-RaidersMatch.Point(distance),Vector3.up);
        }
        private void LateUpdate()
        {
            if(!root.gameObject.activeSelf || session.Match==null)return;
            var m=session.Match;Place(company,m.companyDistance);
            for(int i=0;i<2;i++){locomotives[i].gameObject.SetActive(!m.practice || i==0);Place(locomotives[i],m.players[i].distance);}
            for(int i=0;i<wagons.Length;i++)
            {
                var c=m.cargo[i];wagons[i].gameObject.SetActive(c.owner!=-3);Place(wagons[i],c.distance);
                var material=c.owner==0?playerOne:c.owner==1?playerTwo:neutral;
                if(wagonMarkers[i].sharedMaterial!=material)wagonMarkers[i].sharedMaterial=material;
            }
            for(int i=0;i<devices.Length;i++)
            {
                var d=m.devices[i];devices[i].gameObject.SetActive(d.owner>=0);
                var material=d.spent?spent:d.owner==0?playerOne:playerTwo;
                if(deviceMarkers[i].sharedMaterial!=material)deviceMarkers[i].sharedMaterial=material;
                devices[i].localRotation=Quaternion.Euler(0,i*45+d.aim*35,0);
                if(firingBars!=null && i<firingBars.Length)
                {
                    float fill=d.spent?0:m.Window(i);
                    firingBars[i].localScale=new Vector3(.06f*fill,.006f,.008f);
                    firingBars[i].localPosition=new Vector3(-.03f+.03f*fill,.065f,0);
                }
            }
        }
    }
}
