using Oculus.Avatar2;

namespace SwitchyardRaiders.Startup
{
    // SDK 40's native providers consume the existing OVR tracking service.
    // This component never creates another rig or changes tracking origin.
    public sealed class RaidersAvatarInput : OvrAvatarInputManager { }
}
