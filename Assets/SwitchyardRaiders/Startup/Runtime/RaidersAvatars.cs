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
        public ulong UserId { get; private set; }
        public string Status { get; private set; }="Avatars not initialized";
        private RaidersAvatarEntity localAvatar, remoteAvatar;
        private GameObject manager;
        private byte[] buffer;
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
            if(session.IsLocal)
            {
                // No second authenticated account is fabricated in local tests.
                if(remoteFallback)remoteFallback.SetActive(session.CanSwitchPlayers);
                Status=session.CanSwitchPlayers?"Player 2 — local test slot":"Local practice";return;
            }
            if(UserId==0 || localAvatar)return;
            if(!managerPrefab){Status="Avatar manager prefab is missing";return;}
            manager=Instantiate(managerPrefab,transform);
            var input=manager.GetComponent<RaidersAvatarInput>();if(!input)input=manager.AddComponent<RaidersAvatarInput>();
            var go=new GameObject("Local Meta avatar (interaction hands preserved)");go.SetActive(false);go.transform.SetParent(trackingSpace,false);
            localAvatar=go.AddComponent<RaidersAvatarEntity>();localAvatar.local=true;localAvatar.userId=UserId;localAvatar.tracking=input;go.SetActive(true);
            if(remoteFallback)remoteFallback.SetActive(true);Status="Loading Meta avatars…";
        }
        private void Update()
        {
            if(!localAvatar || !localAvatar.HasJoints || Time.unscaledTime<nextSend)return;
            nextSend=Time.unscaledTime+.1f;
            int count=checked((int)localAvatar.RecordStreamData_AutoBuffer(OvrAvatarEntity.StreamLOD.Medium,ref buffer));
            if(count>0)session.SendAvatar(UserId,buffer,count);
        }
        private void Receive(int slot,ulong userId,byte[] bytes)
        {
            if(session.IsLocal || userId==0 || !manager)return;
            if(!remoteAvatar || remoteAvatar.userId!=userId)
            {
                if(remoteAvatar)Destroy(remoteAvatar.gameObject);
                var go=new GameObject("Remote Meta avatar");go.SetActive(false);go.transform.SetParent(remoteSeat,false);
                remoteAvatar=go.AddComponent<RaidersAvatarEntity>();remoteAvatar.local=false;remoteAvatar.userId=userId;remoteAvatar.fallback=remoteFallback;go.SetActive(true);
            }
            remoteAvatar.Receive(bytes);
        }
        private void Clear()
        {
            if(localAvatar)Destroy(localAvatar.gameObject);if(remoteAvatar)Destroy(remoteAvatar.gameObject);if(manager)Destroy(manager);
            localAvatar=remoteAvatar=null;manager=null;buffer=null;
            if(remoteFallback)remoteFallback.SetActive(false);
        }
        private void OnDestroy(){destroyed=true;session.Changed-=SessionChanged;session.AvatarPacket-=Receive;Clear();}
    }
}
