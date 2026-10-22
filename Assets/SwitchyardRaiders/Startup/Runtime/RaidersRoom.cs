using System;
using System.Threading;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    public sealed class RaidersRoom : MonoBehaviour
    {
        public MRUK mruk;
        public string Status { get; private set; } = "Real Room uses your saved room scan to place the yard.";
        public bool Loading => loading != null;
        private Task<bool> loading;
        private CancellationTokenSource selection;
        public void CancelSelection() => selection?.Cancel();
        public Task<bool> Load()
        {
            if (loading != null) return loading;
            selection?.Dispose();
            selection = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            return loading = LoadRoom();
        }
        private async Task<bool> LoadRoom()
        {
            // Yield first so even synchronous permission results cannot leave a completed task cached.
            await Task.Yield();
            try
            {
                Status = "Allow spatial data access to use your room scan.";
                var cancellation = selection.Token;
                if (!await RaidersPermissions.Request(RaidersPermissions.Scene, cancellation))
                { Status = "Room access denied. Enable Spatial Data in app permissions and retry, or choose Virtual Room."; return false; }
                if (!this) return false;
                if (!mruk) throw new InvalidOperationException("MRUK is not configured.");
                Status = "Loading your room. Complete Space Setup if prompted; Virtual Room is also available.";
                var result = await mruk.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound: false);
                cancellation.ThrowIfCancellationRequested();
                if (result == MRUK.LoadDeviceResult.NoRoomsFound)
                {
                    Status = "Complete Space Setup, then return to place the yard.";
                    await OVRScene.RequestSpaceSetup();
                    cancellation.ThrowIfCancellationRequested();
                    result = await mruk.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound: false);
                }
                if (!this) return false;
                if (result != MRUK.LoadDeviceResult.Success || !mruk.GetCurrentRoom())
                { Status = "Room scan unavailable (" + result + "). Retry Real Room, or choose Virtual Room."; return false; }
                Status = "Room scan loaded. Placing the yard inside your room.";
                return true;
            }
            catch (OperationCanceledException) { if (this) Status = "Room loading cancelled. Choose Real Room to retry."; return false; }
            catch (Exception ex)
            { if (this) Status = "Room loading failed. Retry Real Room, or choose Virtual Room."; Debug.LogWarning("[SR Room] " + ex.Message); return false; }
            finally { if (this) loading = null; }
        }
        public bool TryPlace(Transform head, out Pose pose)
        {
            pose = default;
            var room = mruk ? mruk.GetCurrentRoom() : null;
            if (!room || !head || room.FloorAnchors.Count == 0) return false;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            float floor = room.FloorAnchors[0].transform.position.y;
            float height = Mathf.Clamp(head.position.y - .5f, floor + .65f, floor + 1.2f);
            // Test the board's full footprint against room walls and scanned furniture, not just its centre.
            // Keep it within comfortable seated reach and prefer the area directly in front of the player.
            for (int ring = 0; ring < 5; ring++)
            for (int step = 0; step < (ring == 0 ? 1 : 12); step++)
            {
                float angle = step * Mathf.PI / 6;
                var position = head.position + forward * 1.05f + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ring * .18f;
                position.y = height;
                // Prefer a scanned tabletop under the candidate, keeping the board above the physical surface.
                var ray = new Ray(position + Vector3.up * 1.5f, Vector3.down);
                if (room.Raycast(ray, 2f, new LabelFilter(MRUKAnchor.SceneLabels.TABLE), out var hit, out _)
                    && Vector3.Dot(hit.normal, Vector3.up) > .9f && hit.point.y >= floor + .5f && hit.point.y < head.position.y)
                    position.y = hit.point.y + .1f;
                if (!Fits(room, position, rotation)) continue;
                pose = new Pose(position, rotation); return true;
            }
            Status = "No clear space for the yard here. Move to an open area and retry, or choose Virtual Room.";
            return false;
        }
        private static bool Fits(MRUKRoom room, Vector3 centre, Quaternion rotation)
        {
            for (int x = -3; x <= 3; x++)
            for (int z = -2; z <= 2; z++)
            {
                var point = centre + rotation * new Vector3(x * .27f, 0, z * .29f);
                if (!room.IsPositionInRoom(point) || room.IsPositionInSceneVolume(point, .08f)) return false;
            }
            return true;
        }
        private void OnDestroy() { selection?.Cancel(); selection?.Dispose(); }
    }
}
