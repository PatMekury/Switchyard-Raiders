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
        [Header("Reusable presentation references")]
        public Transform presentation, terrainMount;
        public GameObject virtualTable, terrainAssembly, manipulationHandles;
        public Transform[] seats;
        public RaidersTableManipulation manipulation;
        public LineRenderer placementPreview;
        public bool Exploration { get; private set; }
        public float SurfaceHeight => homePose.position.y;
        public bool InputBlocked => roomMenu.IsOpen || roomMenu.Transitioning || (manipulation && manipulation.BlocksGameplay);
        public bool CanManipulate => session.Phase==RaidersSessionPhase.Playing && !roomMenu.IsOpen && !roomMenu.Transitioning;
        private Pose vrPose, vrHomePose, homePose;
        private float vrScale=1, homeScale=1;
        private bool vrSaved, positioned, matchShown;
        private float nextRoomCheck;
        private void Awake()
        {
            foreach(var follower in root.GetComponentsInChildren<SwitchyardRaiders.PlayerVehicles.PlayerVehicleTrackFollower>(true))follower.table=this;
            foreach(var follower in root.GetComponentsInChildren<CompanyTableFollower>(true))follower.table=this;
        }
        public void PrepareModeChange()
        {
            if(manipulation)manipulation.Suspend();
            if(positioned && roomMenu.IsVirtual){vrPose=new Pose(presentation.position,presentation.rotation);vrScale=presentation.localScale.x;vrSaved=true;}
        }
        public void ModeChosen()
        {
            headset=launch.headset;
            if(roomMenu.IsVirtual)
            {
                if(!vrSaved)
                {
                    vrPose=TabletopViewMath.ReachablePose(headset);
                    vrScale=1;vrHomePose=vrPose;vrSaved=true;
                }
                homePose=vrHomePose;homeScale=1;
            }
            else
            {
                if(!roomUnderstanding.TryConfirmed(headset,out homePose,out homeScale))return;
            }
            var restored=roomMenu.IsVirtual?vrPose:homePose;
            presentation.SetPositionAndRotation(restored.position,restored.rotation);presentation.localScale=Vector3.one*(roomMenu.IsVirtual?vrScale:homeScale);
            positioned=true;Exploration=false;presentation.gameObject.SetActive(true);
            virtualTable.SetActive(roomMenu.IsVirtual);
            bool playing=session.Phase==RaidersSessionPhase.Playing || session.Phase==RaidersSessionPhase.Finished;
            terrainAssembly.SetActive(playing);root.gameObject.SetActive(playing);
            if(placementPreview)placementPreview.gameObject.SetActive(false);
            UpdateSeats();
        }
        public bool Show()
        {
            if(!positioned)ModeChosen();
            if(!positioned)return false;
            if(!roomMenu.IsVirtual && !roomUnderstanding.TryConfirmed(headset,out _,out _))return false;
            if(!matchShown && roomMenu.IsVirtual)ResetView();
            matchShown=true;
            terrainAssembly.SetActive(true);root.gameObject.SetActive(true);return true;
        }
        public void Hide()
        {
            matchShown=false;
            if(manipulation)manipulation.Suspend();
            terrainAssembly.SetActive(false);root.gameObject.SetActive(false);Exploration=false;
            if(positioned)ResetView();
            presentation.gameObject.SetActive(roomMenu.HasSelection);
        }
        public void ResetView()
        {
            if(manipulation)manipulation.Suspend();
            if(roomMenu.IsVirtual)
            {
                headset=launch.headset;
                homePose=vrHomePose=vrPose=TabletopViewMath.ReachablePose(headset);
                homeScale=vrScale=1;vrSaved=true;
            }
            if(!roomMenu.IsVirtual && roomUnderstanding.TryConfirmed(headset,out var pose,out float scale)){homePose=pose;homeScale=scale;}
            presentation.SetPositionAndRotation(homePose.position,homePose.rotation);presentation.localScale=Vector3.one*homeScale;
            Exploration=false;virtualTable.SetActive(roomMenu.IsVirtual);
        }
        public void ToggleExploration(){if(manipulation)manipulation.Suspend();Exploration=!Exploration;if(!Exploration)ResetView();}
        public bool AcceptView(Vector3 position,Quaternion rotation,float scale)
        {
            if(roomMenu.IsVirtual || Exploration)return true;
            return roomUnderstanding.FitsConfirmed(position,rotation,scale);
        }
        public void Preview(Pose pose,float scale)
        {
            if(!placementPreview)return;
            placementPreview.transform.SetPositionAndRotation(pose.position+Vector3.up*.005f,pose.rotation);
            placementPreview.transform.localScale=Vector3.one*scale;placementPreview.gameObject.SetActive(true);
        }
        public void CancelPreview(){if(placementPreview)placementPreview.gameObject.SetActive(false);}
        private void UpdateSeats()
        {
            if(seats==null)return;
            for(int i=0;i<seats.Length;i++)
            {
                var seat=seats[i];if(!seat)continue;
                int relative=session.IsLocal?i:(i-session.ControlledSlot+session.Capacity)%session.Capacity;
                seat.localPosition=TabletopViewMath.SeatPosition(relative,session.Capacity);
                var inward=-seat.localPosition;inward.y=0;seat.localRotation=Quaternion.LookRotation(inward);
                seat.gameObject.SetActive(session.IsOccupied(i) && (session.IsLocal || i!=session.ControlledSlot));
            }
        }
        private static void Place(Transform model,float distance)
        {
            model.localPosition=RaidersMatch.Point(distance);
            model.localRotation=Quaternion.LookRotation(RaidersMatch.Point(distance+.02f)-RaidersMatch.Point(distance),Vector3.up);
        }
        private void LateUpdate()
        {
            if(!presentation)return;
            UpdateSeats();
            if(roomMenu.HasSelection && !roomMenu.IsVirtual && !roomMenu.IsOpen && Time.unscaledTime>=nextRoomCheck)
            {
                nextRoomCheck=Time.unscaledTime+.5f;
                if(!roomUnderstanding.TryConfirmed(headset,out _,out _))
                {if(manipulation)manipulation.Suspend();roomMenu.mainMenu.navigation.PlacementLost();}
            }
            if(manipulationHandles)manipulationHandles.SetActive(false);
            if(virtualTable)virtualTable.SetActive(roomMenu.HasSelection && roomMenu.IsVirtual && presentation.localScale.x<1.6f);
            if(!root.gameObject.activeSelf || session.Match==null)return;
            var m=session.Match;Place(company,m.companyDistance);
            for(int i=0;i<locomotives.Length;i++){bool active=i<m.playerCount && (!m.practice || i==0);locomotives[i].gameObject.SetActive(active);if(active)Place(locomotives[i],m.players[i].distance);}
            for(int i=0;i<wagons.Length;i++)
            {
                var c=m.cargo[i];wagons[i].gameObject.SetActive(c.owner>=-2);Place(wagons[i],c.distance);
                var material=c.owner>=0?(m.Team(c.owner)==0?playerOne:playerTwo):neutral;
                if(wagonMarkers[i].sharedMaterial!=material)wagonMarkers[i].sharedMaterial=material;
            }
            for(int i=0;i<devices.Length;i++)
            {
                var d=m.devices[i];devices[i].gameObject.SetActive(d.owner>=0);
                var material=d.spent?spent:m.Team(d.owner)==0?playerOne:playerTwo;
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
