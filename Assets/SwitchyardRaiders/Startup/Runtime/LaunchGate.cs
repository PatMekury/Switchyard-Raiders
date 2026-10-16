namespace SwitchyardRaiders.Startup
{
    /// <summary>Independent of XR so readiness, focus and repeat completion can be verified.</summary>
    public sealed class LaunchGate
    {
        public enum Phase { WaitingForRoom, Playing, Holding }
        public Phase Current { get; private set; }
        public float RoomVisibleTime { get; private set; }
        public bool AdvanceRoom(bool roomReady, bool focused, float deltaTime)
        {
            if (Current != Phase.WaitingForRoom || !focused) return false;
            if (!roomReady) { RoomVisibleTime = 0f; return false; }
            RoomVisibleTime += deltaTime;
            if (RoomVisibleTime < 0.35f) return false;
            Current = Phase.Playing;
            return true;
        }
        public bool Complete()
        {
            if (Current != Phase.Playing) return false;
            Current = Phase.Holding;
            return true;
        }
    }
}
