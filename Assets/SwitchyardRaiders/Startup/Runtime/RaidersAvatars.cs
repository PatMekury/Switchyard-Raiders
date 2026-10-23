using System;
using System.Threading.Tasks;
using Oculus.Avatar2;
using Oculus.Platform;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    public sealed class RaidersAvatars : MonoBehaviour
    {
        public RaidersSession session;
        public GameObject managerPrefab;
        public Transform trackingSpace, remoteSeat;
        public GameObject remoteFallback;
        public Transform headset;
        public Transform[] playerSeats;
        public GameObject[] playerFallbacks;
        private RaidersAvatarEntity[] remoteAvatars = new RaidersAvatarEntity[4];
        private Vector3 trackingOrigin;
        private float trackingYaw;
        public ulong UserId { get; private set; }
        public string Status { get; private set; }="Avatars not initialized";
        private RaidersAvatarEntity localAvatar;
        private GameObject manager;
        // The SDK reads Length before resizing, so even the first snapshot needs a non-null buffer.
        private byte[] buffer = Array.Empty<byte>();
        private float nextSend;
        private Task identity;
        private bool destroyed;
        private void Awake(){session.Changed+=SessionChanged;session.AvatarPacket+=Receive;}
        public async Task EnsureIdentity()
        {
            if(UserId!=0)return;
            if(identity==null)identity=InitializeIdentity();
            try { await identity; } finally { identity=null; }
        }
        private async Task InitializeIdentity()
        {
            Status="Initializing Meta identity…";
            if(!Core.IsInitialized())
            {
                var init=await Core.AsyncInitialize();
                if(init.IsError)throw new InvalidOperationException("Meta Platform initialization failed. Configure the project's Meta App ID and test-user access.");
            }
            var entitlement=await Entitlements.IsUserEntitledToApplication();
            if(entitlement.IsError)throw new InvalidOperationException("This Meta user is not entitled to the app. Add the account to its release channel or configure an authorized editor test user.");
            var user=await Users.GetLoggedInUser();
            if(user.IsError || user.Data.ID==0)throw new InvalidOperationException("Meta user identity is unavailable. Check platform account setup and User ID access.");
            var token=await Users.GetAccessToken();
            if(token.IsError || string.IsNullOrEmpty(token.Data))throw new InvalidOperationException("Meta avatar access token is unavailable.");
            if(destroyed)return;
            OvrAvatarEntitlement.SetAccessToken(token.Data);UserId=user.Data.ID;Status="Meta identity ready";
        }
        private void SessionChanged()
        {
            if(session.Phase==RaidersSessionPhase.Idle || session.Phase==RaidersSessionPhase.Failed || session.Phase==RaidersSessionPhase.Leaving){Clear();return;}
            if(session.Phase!=RaidersSessionPhase.Ready && session.Phase!=RaidersSessionPhase.Waiting)return;
            for(int i=0;i<playerFallbacks.Length;i++)
            {
                bool visible=session.IsOccupied(i) && (session.IsLocal || i!=session.ControlledSlot);
                if(playerFallbacks[i])
                {
                    playerFallbacks[i].SetActive(visible && (!remoteAvatars[i] || !remoteAvatars[i].AvatarLoaded));
                    var label=playerFallbacks[i].GetComponentInChildren<TextMesh>();
                    if(label)label.text="P"+(i+1)+(session.IsLocal?" SIMULATED":" - avatar loading / unavailable");
                }
                if(!session.IsOccupied(i) && remoteAvatars[i]){Destroy(remoteAvatars[i].gameObject);remoteAvatars[i]=null;}
            }
            if(session.IsLocal){Status="Local test - simulated presence markers only";return;}
            if(UserId==0 || localAvatar)return;
            if(!managerPrefab){Status="Avatar manager prefab is missing";return;}
            manager=Instantiate(managerPrefab,transform);
            if(headset && trackingSpace)
            {trackingOrigin=trackingSpace.InverseTransformPoint(headset.position);trackingOrigin.y=0;trackingYaw=(Quaternion.Inverse(trackingSpace.rotation)*headset.rotation).eulerAngles.y;}
            var input=manager.GetComponent<RaidersAvatarInput>();if(!input)input=manager.AddComponent<RaidersAvatarInput>();
            var go=new GameObject("Local Meta avatar (interaction hands preserved)");go.SetActive(false);go.transform.SetParent(trackingSpace,false);
            localAvatar=go.AddComponent<RaidersAvatarEntity>();localAvatar.local=true;localAvatar.userId=UserId;localAvatar.tracking=input;go.SetActive(true);
            Status="Loading Meta avatars…";
        }
        private void Update()
        {
            if(!localAvatar || !localAvatar.HasJoints || Time.unscaledTime<nextSend)return;
            nextSend=Time.unscaledTime+.1f;
            int count=checked((int)localAvatar.RecordStreamData_AutoBuffer(OvrAvatarEntity.StreamLOD.Medium,ref buffer));
            if(count>0)session.SendAvatar(UserId,buffer,count,trackingOrigin,trackingYaw);
        }
        private void Receive(int slot,ulong userId,byte[] bytes,Vector3 origin,float yaw)
        {
            if(session.IsLocal || userId==0 || !manager || slot<0 || slot>=playerSeats.Length || slot==session.ControlledSlot)return;
            var remote=remoteAvatars[slot];
            if(!remote || remote.userId!=userId)
            {
                if(remote)Destroy(remote.gameObject);
                var go=new GameObject("Authenticated Meta avatar P"+(slot+1));go.SetActive(false);go.transform.SetParent(playerSeats[slot],false);
                remote=go.AddComponent<RaidersAvatarEntity>();remote.local=false;remote.userId=userId;remote.fallback=playerFallbacks[slot];remoteAvatars[slot]=remote;
                if(remote.fallback)remote.fallback.SetActive(true);go.SetActive(true);
            }
            // Normalize the sender's tracking origin into the receiver's local seat. Never move the rig.
            var rotation=Quaternion.Euler(0,-yaw,0);remote.transform.localRotation=rotation;remote.transform.localPosition=rotation*(-origin);
            remote.Receive(bytes);
        }
        private void Clear()
        {
            if(localAvatar)Destroy(localAvatar.gameObject);
            for(int i=0;i<remoteAvatars.Length;i++){if(remoteAvatars[i])Destroy(remoteAvatars[i].gameObject);remoteAvatars[i]=null;}
            if(manager)Destroy(manager);localAvatar=null;manager=null;buffer=Array.Empty<byte>();
            if(playerFallbacks!=null)foreach(var fallback in playerFallbacks)if(fallback)fallback.SetActive(false);
        }
        private void OnDestroy(){destroyed=true;session.Changed-=SessionChanged;session.AvatarPacket-=Receive;Clear();}
    }
}
