using System;
using System.Threading.Tasks;
using Oculus.Avatar2;
using Oculus.Platform;
using Oculus.Interaction.Input;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    public sealed class RaidersAvatars : MonoBehaviour
    {
        public enum LocalPresentation { Menu, Identity, Loading, Ready, TrackingPaused, Fallback }
        public RaidersSession session;
        public GameObject managerPrefab;
        public Transform trackingSpace, remoteSeat;
        public GameObject remoteFallback;
        public Transform headset;
        public Transform[] playerSeats;
        public GameObject[] playerFallbacks;
        public UnityEngine.Object leftHandSource,rightHandSource,hmdSource;
        public Renderer[] outlineRenderers=Array.Empty<Renderer>();
        private readonly RaidersAvatarEntity[] remoteAvatars=new RaidersAvatarEntity[4];
        private Vector3 trackingOrigin;
        private float trackingYaw;
        public ulong UserId { get; private set; }
        public LocalPresentation Presentation { get; private set; }=LocalPresentation.Menu;
        public string Failure { get; private set; }="";
        public string Status => Presentation switch
        {
            LocalPresentation.Identity=>"Avatar: verifying account — temporary hands",
            LocalPresentation.Loading=>"Avatar: loading — temporary hands",
            LocalPresentation.Ready=>"Your Meta avatar",
            LocalPresentation.TrackingPaused=>"Avatar tracking paused — temporary hands",
            LocalPresentation.Fallback=>"Avatar unavailable - temporary hands; Table view > Avatar",
            _=>"Menu hand visuals"
        };
        public bool CanRetry=>WantsGameplay && Presentation==LocalPresentation.Fallback;
        public bool WantsGameplay=>session && session.Phase==RaidersSessionPhase.Playing;
        private RaidersAvatarEntity localAvatar;
        private RaidersAvatarInput input;
        private GameObject manager;
        private bool ownsManager,ownsInput;
        // SDK reads Length before resizing. Preserve this on startup, retry and cleanup.
        private byte[] buffer=Array.Empty<byte>();
        private float nextSend;
        private Task identity;
        private int identityGeneration,generation;
        private bool destroyed,attempted,visualsHidden;
        private bool[] previousForceOff;
#if UNITY_EDITOR
        // Nonserialized Edit Mode seams exercise the async lifecycle without credentials/native
        // avatar creation. They do not exist in players and never alter EnsureIdentity's gate.
        private Func<Task> identityFixture;
        private Action createFixture;
#endif
        private void Awake(){session.Changed+=SessionChanged;session.AvatarPacket+=Receive;}
        public async Task EnsureIdentity()
        {
            if(UserId!=0 && OvrAvatarEntitlement.AccessTokenIsValid())return;
            if(identity==null)identity=IdentityWithDeadline(++identityGeneration);
            var pending=identity;
            try{await pending;}finally{if(identity==pending)identity=null;}
        }
        private async Task IdentityWithDeadline(int ticket)
        {
            var pending=InitializeIdentity(ticket);
            if(await Task.WhenAny(pending,Task.Delay(15000))!=pending)
            {
                if(ticket==identityGeneration)identityGeneration++;
                _=pending.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
                throw new InvalidOperationException("Meta account lookup timed out. Check account access, then retry.");
            }
            await pending;
        }
        private void CheckIdentityTicket(int ticket)
        {if(destroyed || ticket!=identityGeneration)throw new OperationCanceledException();}
        private async Task InitializeIdentity(int ticket)
        {
            UserId=0;
            if(!Core.IsInitialized())
            {
                var init=await Core.AsyncInitialize();CheckIdentityTicket(ticket);
                if(init.IsError)throw new InvalidOperationException("Meta Platform is unavailable. Use an entitled headset account or authorized Editor test user.");
            }
            var entitlement=await Entitlements.IsUserEntitledToApplication();CheckIdentityTicket(ticket);
            if(entitlement.IsError)throw new InvalidOperationException("This account is not entitled to the app. Check release-channel or authorized test-user access.");
            var user=await Users.GetLoggedInUser();CheckIdentityTicket(ticket);
            if(user.IsError || user.Data==null || user.Data.ID==0)throw new InvalidOperationException("Meta user identity is unavailable. Check account setup and User ID access.");
            var token=await Users.GetAccessToken();CheckIdentityTicket(ticket);
            if(token.IsError || string.IsNullOrEmpty(token.Data))throw new InvalidOperationException("Meta avatar access is unavailable. Check account permissions, then retry.");
            OvrAvatarEntitlement.SetAccessToken(token.Data);UserId=user.Data.ID;
        }
        private void SessionChanged()
        {
            if(!WantsGameplay){ClearPresentation();UpdateMarkers();return;}
            UpdateMarkers();
            if(!attempted)BeginLocalPresentation();
        }
        private async void BeginLocalPresentation()
        {
            if(!WantsGameplay || attempted || destroyed)return;
            attempted=true;int ticket=++generation;
            Presentation=LocalPresentation.Identity;Failure="";SetOutlineHidden(false);
            try
            {
                // Also used by solo: only Platform identity/entitlement, never Photon or fake users.
#if UNITY_EDITOR
                await (identityFixture!=null?identityFixture():EnsureIdentity());
#else
                await EnsureIdentity();
#endif
                if(!Current(ticket))return;
#if UNITY_EDITOR
                if(createFixture!=null){Presentation=LocalPresentation.Loading;createFixture();return;}
#endif
                if(!trackingSpace || !(leftHandSource is IHand) || !(rightHandSource is IHand) || !(hmdSource is IHmd))
                    throw new InvalidOperationException("Avatar tracking bindings are missing. Temporary hands remain available.");
                EnsureManager();
                if(!Current(ticket))return;
                input.Configure((IHand)leftHandSource,(IHand)rightHandSource,(IHmd)hmdSource,trackingSpace);
                if(headset){trackingOrigin=trackingSpace.InverseTransformPoint(headset.position);trackingOrigin.y=0;trackingYaw=(Quaternion.Inverse(trackingSpace.rotation)*headset.rotation).eulerAngles.y;}
                var go=new GameObject("Local account avatar — first person");go.SetActive(false);go.transform.SetParent(trackingSpace,false);
                localAvatar=go.AddComponent<RaidersAvatarEntity>();localAvatar.local=true;localAvatar.userId=UserId;localAvatar.tracking=input;
                Presentation=LocalPresentation.Loading;go.SetActive(true);
            }
            catch(Exception error)
            {
                if(Current(ticket)){Presentation=LocalPresentation.Fallback;Failure=error is InvalidOperationException?error.Message:"Account avatar could not initialize. Check account access and retry.";SetOutlineHidden(false);}
            }
        }
        private bool Current(int ticket)=>!destroyed && ticket==generation && WantsGameplay;
        private void EnsureManager()
        {
            if(!manager)
            {
                var existing=FindObjectsByType<OvrAvatarManager>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                if(existing.Length>1)throw new InvalidOperationException("Multiple avatar services found. Resolve the duplicate before retrying.");
                if(existing.Length==1){manager=existing[0].gameObject;ownsManager=false;}
                else
                {
                    if(!managerPrefab)throw new InvalidOperationException("Avatar service prefab is missing.");
                    manager=Instantiate(managerPrefab,transform);ownsManager=true;
                }
            }
            if(!input)
            {
                input=manager.GetComponent<RaidersAvatarInput>();
                if(!input){input=gameObject.AddComponent<RaidersAvatarInput>();ownsInput=true;}
            }
        }
        public void RetryLocalAvatar()
        {
            if(!CanRetry)return;
            generation++;RemoveLocal();attempted=false;BeginLocalPresentation();
        }
        private void Update()
        {
            if(!WantsGameplay){if(attempted || localAvatar)ClearPresentation();return;}
            if(!attempted)BeginLocalPresentation();
            if(localAvatar)
            {
                if(localAvatar.PresentationState==RaidersAvatarEntity.Readiness.Failed)
                {Presentation=LocalPresentation.Fallback;Failure=localAvatar.Failure;}
                else if(localAvatar.AvatarLoaded)
                {Presentation=input && input.TrackingReady?LocalPresentation.Ready:LocalPresentation.TrackingPaused;}
                else Presentation=LocalPresentation.Loading;
                bool show=Presentation==LocalPresentation.Ready;
                localAvatar.SetPresentationVisible(show);SetOutlineHidden(show);
            }
            UpdateMarkers();
            // Presentation works in solo; the transport and native snapshot allocation do not run.
            if(session.IsLocal || !localAvatar || !localAvatar.AvatarLoaded || !localAvatar.HasJoints || Time.unscaledTime<nextSend)return;
            nextSend=Time.unscaledTime+.1f;
            int count=checked((int)localAvatar.RecordStreamData_AutoBuffer(OvrAvatarEntity.StreamLOD.Medium,ref buffer));
            if(count>0)session.SendAvatar(UserId,buffer,count,trackingOrigin,trackingYaw);
        }
        // forceRenderingOff is independent of HandVisual's tracking-driven Renderer.enabled.
        // Never disable a tracking parent, provider, interactor, or the HandVisual component.
        public void SetOutlineHidden(bool hide)
        {
            if(hide==visualsHidden)return;
            if(hide)
            {
                previousForceOff=new bool[outlineRenderers.Length];
                for(int i=0;i<outlineRenderers.Length;i++)if(outlineRenderers[i]){previousForceOff[i]=outlineRenderers[i].forceRenderingOff;outlineRenderers[i].forceRenderingOff=true;}
            }
            else if(previousForceOff!=null)
                for(int i=0;i<outlineRenderers.Length && i<previousForceOff.Length;i++)if(outlineRenderers[i])outlineRenderers[i].forceRenderingOff=previousForceOff[i];
            visualsHidden=hide;
        }
        private void UpdateMarkers()
        {
            if(playerFallbacks==null)return;
            for(int i=0;i<playerFallbacks.Length;i++)
            {
                bool occupied=session.IsOccupied(i);
                if(!occupied && remoteAvatars[i]){Release(remoteAvatars[i].gameObject);remoteAvatars[i]=null;}
                if(!playerFallbacks[i])continue;
                bool visible=occupied && (session.IsLocal || i!=session.ControlledSlot);
                playerFallbacks[i].SetActive(visible && (!remoteAvatars[i] || !remoteAvatars[i].AvatarLoaded));
                var label=playerFallbacks[i].GetComponentInChildren<TextMesh>();
                if(label)label.text="P"+(i+1)+(session.IsLocal?" SIMULATED SLOT":" — avatar loading / unavailable");
            }
        }
        private void Receive(int slot,ulong userId,byte[] bytes,Vector3 origin,float yaw)
        {
            // The session verifies the sender's Photon identity before invoking this callback.
            if(session.IsLocal || !WantsGameplay || userId==0 || !manager || bytes==null || bytes.Length==0 || slot<0 || slot>=playerSeats.Length || slot==session.ControlledSlot || !session.IsOccupied(slot))return;
            var remote=remoteAvatars[slot];
            if(!remote || remote.userId!=userId)
            {
                if(remote)Release(remote.gameObject);
                var go=new GameObject("Authenticated remote Meta avatar P"+(slot+1));go.SetActive(false);go.transform.SetParent(playerSeats[slot],false);
                remote=go.AddComponent<RaidersAvatarEntity>();remote.local=false;remote.userId=userId;remote.fallback=playerFallbacks[slot];remoteAvatars[slot]=remote;
                if(remote.fallback)remote.fallback.SetActive(true);go.SetActive(true);
            }
            // Sender tracking-local metres -> receiver's unit-scale human seat; never board space.
            var rotation=Quaternion.Euler(0,-yaw,0);remote.transform.localRotation=rotation;remote.transform.localPosition=rotation*(-origin);remote.transform.localScale=Vector3.one;
            remote.Receive(bytes);
        }
        private void RemoveLocal()
        {
            SetOutlineHidden(false);
            if(localAvatar){localAvatar.SetPresentationVisible(false);Release(localAvatar.gameObject);}
            localAvatar=null;buffer=Array.Empty<byte>();
        }
        private void ClearPresentation()
        {
            if(attempted || localAvatar)generation++;
            RemoveLocal();attempted=false;Presentation=LocalPresentation.Menu;Failure="";
            for(int i=0;i<remoteAvatars.Length;i++){if(remoteAvatars[i])Release(remoteAvatars[i].gameObject);remoteAvatars[i]=null;}
            // Keep a single warm service between matches/retries. Dispose only what we own.
            if(playerFallbacks!=null)foreach(var fallback in playerFallbacks)if(fallback)fallback.SetActive(false);
        }
        private static void Release(UnityEngine.Object value)
        {
#if UNITY_EDITOR
            if(!UnityEngine.Application.isPlaying){DestroyImmediate(value);return;}
#endif
            Destroy(value);
        }
        private void OnDisable()=>ClearPresentation();
        private void OnDestroy()
        {
            destroyed=true;identityGeneration++;generation++;
            if(session){session.Changed-=SessionChanged;session.AvatarPacket-=Receive;}
            ClearPresentation();
            if(ownsInput && input)Release(input);
            if(ownsManager && manager)Release(manager);
        }
    }
}
