using System;
using System.Collections;
using Oculus.Avatar2;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    public sealed class RaidersAvatarEntity : OvrAvatarEntity
    {
        public enum Readiness { Waiting, Loading, Ready, Failed }
        public bool local;
        public ulong userId;
        public RaidersAvatarInput tracking;
        public GameObject fallback;
        private bool initialized;
        public bool AvatarLoaded { get; private set; }
        public Readiness PresentationState { get; private set; }
        public string Failure { get; private set; }="";
        public static CAPI.ovrAvatar2EntityCreateInfo CreationFor(bool firstPerson)
        {
            var filters=CAPI.OvrAvatar2_DefaultLoadFilters;
            filters.viewFlags=firstPerson?CAPI.ovrAvatar2EntityViewFlags.FirstPerson:CAPI.ovrAvatar2EntityViewFlags.ThirdPerson;
            if(firstPerson)filters.subMeshInclusionFlags=CAPI.ovrAvatar2EntitySubMeshInclusionFlags.Body|CAPI.ovrAvatar2EntitySubMeshInclusionFlags.Outfit;
            return new CAPI.ovrAvatar2EntityCreateInfo
            {
                // Never silently display a preset/default model as the account's avatar.
                features=(firstPerson?CAPI.ovrAvatar2EntityFeatures.Preset_Default:CAPI.ovrAvatar2EntityFeatures.Preset_Remote)&~CAPI.ovrAvatar2EntityFeatures.UseDefaultModel,
                renderFilters=filters
            };
        }
        protected override CAPI.ovrAvatar2EntityCreateInfo? ConfigureCreationInfo()=>CreationFor(local);
        protected override void Awake() { } // Configure before native entity creation.
        private IEnumerator Start()
        {
            float deadline=Time.realtimeSinceStartup+20;
            while(!OvrAvatarManager.initialized && Time.realtimeSinceStartup<deadline)yield return null;
            if(!OvrAvatarManager.initialized){Fail("Avatar service did not initialize. Retry avatar.");yield break;}
            if(userId==0 || !OvrAvatarEntitlement.AccessTokenIsValid()){Fail("Account avatar identity is unavailable. Retry avatar.");yield break;}
            SetIsLocal(local);
            SetInitialManifestatation(local?CAPI.ovrAvatar2EntityManifestationFlags.Half:CAPI.ovrAvatar2EntityManifestationFlags.Full);
            if(local)SetInputManager(tracking);
            bool created=false;
            try
            {
                base.Awake();initialized=IsCreated;created=initialized;
                if(created){SetActiveView(local?CAPI.ovrAvatar2EntityViewFlags.FirstPerson:CAPI.ovrAvatar2EntityViewFlags.ThirdPerson);Hidden=true;}
            }
            catch(Exception){Fail("Avatar renderer could not initialize. Retry avatar.");}
            if(!created){Fail("Avatar renderer could not initialize. Retry avatar.");yield break;}
            PresentationState=Readiness.Loading;_userId=userId;
            deadline=Time.realtimeSinceStartup+20;
            var request=OvrAvatarManager.Instance.UserHasAvatarAsync(userId);
            while(!request.IsCompleted && Time.realtimeSinceStartup<deadline)yield return null;
            if(!request.IsCompleted || request.IsCanceled || request.IsFaulted){Fail("Account avatar lookup failed or timed out. Retry avatar.");yield break;}
            if(request.Result!=OvrAvatarManager.HasAvatarRequestResultCode.HasAvatar){Fail("No account avatar is available. Create one in Meta, then retry.");yield break;}
            try{LoadUser();}catch(Exception){Fail("Account avatar download failed. Retry avatar.");}
            deadline=Time.realtimeSinceStartup+30;
            while(PresentationState==Readiness.Loading && Time.realtimeSinceStartup<deadline)yield return null;
            if(PresentationState==Readiness.Loading)Fail("Account avatar download timed out. Retry avatar.");
        }
        private void Fail(string reason)
        {Failure=reason;PresentationState=Readiness.Failed;AvatarLoaded=false;if(initialized)Hidden=true;if(fallback)fallback.SetActive(true);}
        protected override void OnLoadFailed(CAPI.ovrAvatar2LoadRequestInfo request)
        {base.OnLoadFailed(request);Fail("Account avatar could not load. Retry avatar.");}
        protected override void OnUserAvatarLoaded()
        {
            base.OnUserAvatarLoaded();if(PresentationState==Readiness.Failed)return;
            AvatarLoaded=true;PresentationState=Readiness.Ready;
            if(!local){Hidden=false;if(fallback)fallback.SetActive(false);}
        }
        public void SetPresentationVisible(bool visible){if(initialized)Hidden=!visible;}
        public void Receive(byte[] data)
        {if(initialized && !local && HasJoints && data!=null && data.Length>0)ApplyStreamData(data);}
    }
}
