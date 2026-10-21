using System.Collections;
using Oculus.Avatar2;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    public sealed class RaidersAvatarEntity : OvrAvatarEntity
    {
        public bool local;
        public ulong userId;
        public RaidersAvatarInput tracking;
        public GameObject fallback;
        private bool initialized;
        protected override void Awake() { } // Configure before native entity creation.
        private IEnumerator Start()
        {
            float deadline=Time.realtimeSinceStartup+20;
            while(!OvrAvatarManager.initialized && Time.realtimeSinceStartup<deadline)yield return null;
            if(!OvrAvatarManager.initialized)yield break;
            SetIsLocal(local);
            _creationInfo.features=local?CAPI.ovrAvatar2EntityFeatures.Preset_Default:CAPI.ovrAvatar2EntityFeatures.Preset_Remote;
            if(local)SetInputManager(tracking);
            base.Awake();initialized=true;
            // Keep the established interaction hands; the invisible local entity still streams its pose.
            Hidden=local;
            if(userId!=0 && OvrAvatarEntitlement.AccessTokenIsValid())
            {
                _userId=userId;
                var request=OvrAvatarManager.Instance.UserHasAvatarAsync(userId);
                while(!request.IsCompleted && Time.realtimeSinceStartup<deadline)yield return null;
                if(request.IsCompleted && !request.IsFaulted && request.Result==OvrAvatarManager.HasAvatarRequestResultCode.HasAvatar)
                {LoadUser();yield break;}
            }
            // An explicit player marker stays visible if identity/avatar loading is unavailable.
        }
        protected override void OnUserAvatarLoaded()
        {base.OnUserAvatarLoaded();if(fallback)fallback.SetActive(false);}
        public void Receive(byte[] data)
        {if(initialized && !local && HasJoints)ApplyStreamData(data);}
    }
}
