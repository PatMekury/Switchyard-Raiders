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
        public Transform humanPresentation;
        public Transform environmentMotion;
        public RaidersTableManipulation manipulation;
        public LineRenderer placementPreview;
        public bool Exploration { get; private set; }
        public float SurfaceHeight => homePose.position.y;
        public float NormalViewScale => roomMenu.IsVirtual ? TabletopViewContract.NormalScale : homeScale;
        public float MaximumViewScale => NormalViewScale*TabletopViewContract.MaximumZoom;
        public float RelativeZoom => presentation.localScale.x/NormalViewScale;
        public void EnforceViewBounds()
        {
            if(!presentation)return;
            float scale=TabletopViewMath.ClampScale(presentation.localScale.x,NormalViewScale);
            if(presentation.localScale!=Vector3.one*scale)presentation.localScale=Vector3.one*scale;
        }
        public float AnchoredOriginHeight(float scale) => homePose.position.y+TabletopViewContract.TrayBottom*(homeScale-scale);
        private Transform tableSupports;
        public bool InputBlocked => roomMenu.IsOpen || roomMenu.Transitioning || (manipulation && manipulation.BlocksGameplay);
        public bool CanManipulate => session.Phase==RaidersSessionPhase.Playing && !roomMenu.IsOpen && !roomMenu.Transitioning;
        private Pose vrPose, vrHomePose, homePose;
        private Pose environmentHome;
        private bool environmentCaptured;
        public void ApplyPersonalView(Vector3 position,Quaternion rotation,float scale,Vector3 pivot,Vector3 translation,Quaternion yaw)
        {
            // The room receives only rigid motion, never scale compensation of the board origin.
            if(roomMenu.IsVirtual && environmentMotion)
            {
                var room=TabletopViewMath.RigidMotion(new Pose(environmentMotion.position,environmentMotion.rotation),pivot,translation,yaw);
                environmentMotion.SetPositionAndRotation(room.position,room.rotation);
                environmentMotion.localScale=Vector3.one;
            }
            presentation.SetPositionAndRotation(position,rotation);
            presentation.localScale=Vector3.one*TabletopViewMath.ClampScale(scale,NormalViewScale);
        }
        private float vrScale=1, homeScale=1;
        private bool vrSaved, positioned, matchShown;
        private float nextRoomCheck;
        private Transform[] wagonFront, wagonRear, engineRear;
        private LineRenderer[] couplingLinks;
        private static Transform Anchor(Transform parent,string name)
        {
            foreach(var t in parent.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;
            return null;
        }
        public void UpdateCouplings(RaidersMatch match)
        {
            if(couplingLinks==null)
            {
                wagonFront=new Transform[wagons.Length];wagonRear=new Transform[wagons.Length];couplingLinks=new LineRenderer[wagons.Length];engineRear=new Transform[locomotives.Length+1];
                engineRear[0]=Anchor(company,"Coupler_Rear");
                for(int i=0;i<locomotives.Length;i++)engineRear[i+1]=Anchor(locomotives[i],"Coupler_Rear");
                for(int i=0;i<wagons.Length;i++){wagonFront[i]=Anchor(wagons[i],"Coupler_Front");wagonRear[i]=Anchor(wagons[i],"Coupler_Rear");couplingLinks[i]=wagons[i].GetComponentInChildren<LineRenderer>(true);}
            }
            for(int i=0;i<wagons.Length;i++)
            {
                var c=match.cargo[i];var line=couplingLinks[i];if(!line)continue;Transform ahead=null;
                if(c.owner>=-2)
                {
                    if(c.owner!=-1 && c.order==1)ahead=engineRear[c.owner==-2?0:c.owner+1];
                    else for(int j=0;j<match.cargo.Length;j++)if(match.cargo[j].owner==c.owner && match.cargo[j].order==c.order-1 && (c.owner!=-1||match.cargo[j].leader==c.leader))ahead=wagonRear[j];
                }
                line.enabled=ahead && wagonFront[i];
                if(line.enabled){line.SetPosition(0,line.transform.InverseTransformPoint(wagonFront[i].position));line.SetPosition(1,line.transform.InverseTransformPoint(ahead.position));}
            }
        }
        private void Awake()
        {
            if(virtualTable)tableSupports=virtualTable.transform.Find("Supports");
            foreach(var follower in root.GetComponentsInChildren<SwitchyardRaiders.PlayerVehicles.PlayerVehicleTrackFollower>(true))follower.table=this;
            foreach(var follower in root.GetComponentsInChildren<CompanyTableFollower>(true))follower.table=this;
        }
        public void PrepareModeChange()
        {
            if(manipulation)manipulation.Suspend();
            if(positioned && roomMenu.IsVirtual){vrPose=new Pose(presentation.position,presentation.rotation);vrScale=TabletopViewMath.ClampScale(presentation.localScale.x,1);vrSaved=true;}
        }
        public void ModeChosen()
        {
            if(manipulation)manipulation.Suspend();
            headset=launch.headset;
            if(roomMenu.IsVirtual && !environmentCaptured && environmentMotion)
            {environmentHome=new Pose(environmentMotion.position,environmentMotion.rotation);environmentCaptured=true;}
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
            presentation.SetPositionAndRotation(restored.position,restored.rotation);presentation.localScale=Vector3.one*TabletopViewMath.ClampScale(roomMenu.IsVirtual?vrScale:homeScale,NormalViewScale);
            positioned=true;Exploration=false;presentation.gameObject.SetActive(true);
            UpdateTableVisibility();
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
                var next=TabletopViewMath.ReachablePose(headset);
                if(environmentCaptured && environmentMotion)
                {
                    var yaw=next.rotation*Quaternion.Inverse(vrHomePose.rotation);
                    environmentHome=TabletopViewMath.RigidMotion(environmentHome,vrHomePose.position,next.position-vrHomePose.position,yaw);
                    environmentMotion.SetPositionAndRotation(environmentHome.position,environmentHome.rotation);
                    environmentMotion.localScale=Vector3.one;
                }
                homePose=vrHomePose=vrPose=next;
                homeScale=vrScale=1;vrSaved=true;
            }
            if(!roomMenu.IsVirtual && roomUnderstanding.TryConfirmed(headset,out var pose,out float scale)){homePose=pose;homeScale=scale;}
            presentation.SetPositionAndRotation(homePose.position,homePose.rotation);presentation.localScale=Vector3.one*homeScale;
            Exploration=false;UpdateTableVisibility();
        }
        public void ToggleExploration(){if(manipulation)manipulation.Suspend();Exploration=!Exploration;if(!Exploration)ResetView();}
        public bool AcceptView(Vector3 position,Quaternion rotation,float scale)
        {
            if(!TabletopViewMath.Finite(position) || !TabletopViewMath.Finite(rotation*Vector3.forward))return false;
            if(float.IsNaN(scale)||float.IsInfinity(scale)||scale<NormalViewScale||scale>MaximumViewScale)return false;
            if(roomMenu.IsVirtual || Exploration)return true;
            return roomUnderstanding.FitsConfirmed(position,rotation,scale);
        }
        public void Preview(Pose pose,float scale)
        {
            if(!placementPreview)return;
            placementPreview.transform.SetPositionAndRotation(pose.position+Vector3.up*(TabletopViewContract.TrayBottom*scale+.005f),pose.rotation);
            placementPreview.transform.localScale=Vector3.one*scale;placementPreview.gameObject.SetActive(true);
        }
        public void CancelPreview(){if(placementPreview)placementPreview.gameObject.SetActive(false);}
        public void UpdateTableVisibility()
        {
            if(!virtualTable)return;
            if(!tableSupports)tableSupports=virtualTable.transform.Find("Supports");
            // The load-bearing tray remains around the land in MR and close inspection.
            virtualTable.SetActive(roomMenu.HasSelection);
            if(tableSupports)tableSupports.gameObject.SetActive(roomMenu.IsVirtual && presentation.localScale.x<1.6f);
        }
        private void UpdateSeats()
        {
            // Human avatars follow the normal seating origin, never the inspection-scale branch.
            if(humanPresentation){humanPresentation.SetPositionAndRotation(homePose.position,homePose.rotation);humanPresentation.localScale=Vector3.one;humanPresentation.gameObject.SetActive(presentation.gameObject.activeSelf);}
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
        private void LateUpdate()
        {
            if(!presentation)return;
            EnforceViewBounds();
            UpdateSeats();
            if(roomMenu.HasSelection && !roomMenu.IsVirtual && !roomMenu.IsOpen && Time.unscaledTime>=nextRoomCheck)
            {
                nextRoomCheck=Time.unscaledTime+.5f;
                if(!roomUnderstanding.TryConfirmed(headset,out _,out _))
                {if(manipulation)manipulation.Suspend();roomMenu.mainMenu.navigation.PlacementLost();}
            }
            if(manipulationHandles)manipulationHandles.SetActive(false);
            UpdateTableVisibility();
            if(!root.gameObject.activeSelf || session.Match==null)return;
            var m=session.Match;TabletopRailway.Place(company,m.companyDistance,true,.0185f);
            for(int i=0;i<locomotives.Length;i++){bool active=i<m.playerCount && (!m.practice || i==0);locomotives[i].gameObject.SetActive(active);if(active)TabletopRailway.Place(locomotives[i],m.players[i].distance,true,.0415f);}
            for(int i=0;i<wagons.Length;i++)
            {
                var c=m.cargo[i];wagons[i].gameObject.SetActive(c.owner>=-2);TabletopRailway.Place(wagons[i],c.distance,false,.016f);
                var material=c.owner>=0?(m.Team(c.owner)==0?playerOne:playerTwo):neutral;
                if(wagonMarkers[i].sharedMaterial!=material)wagonMarkers[i].sharedMaterial=material;
            }
            UpdateCouplings(m);
            for(int i=0;i<devices.Length;i++)
            {
                var d=m.devices[i];devices[i].gameObject.SetActive(d.owner>=0);
                var material=d.spent?spent:m.Team(d.owner)==0?playerOne:playerTwo;
                if(deviceMarkers[i].sharedMaterial!=material)deviceMarkers[i].sharedMaterial=material;
                var aim=TabletopRailway.Point(RaidersMatch.Site(i)+d.aim)-devices[i].parent.localPosition;aim.y=0;
                devices[i].localRotation=Quaternion.LookRotation(aim);
                if(firingBars!=null && i<firingBars.Length)
                {
                    float fill=d.spent?0:m.Window(i);
                    firingBars[i].localScale=new Vector3(.03f*fill,.003f,.004f);
                    firingBars[i].localPosition=new Vector3(-.015f+.015f*fill,.0325f,0);
                }
            }
        }
    }
}
