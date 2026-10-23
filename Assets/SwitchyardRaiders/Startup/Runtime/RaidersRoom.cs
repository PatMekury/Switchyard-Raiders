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
        public enum RoomAvailability { Unknown, PermissionRequired, Checking, Available, NotFound, Unavailable }
        public RoomAvailability Availability { get; private set; }
        public string Status { get; private set; } = "Real Room uses your saved room scan to place the yard.";
        public bool Loading => loading != null;
        private Task<bool> loading;
        private CancellationTokenSource selection;
        private Task<MRUK.LoadDeviceResult> sceneLoad;
        public void CancelSelection() => selection?.Cancel();
        public async void CheckOnStartup(Transform head)
        {
            // VR is usable without spatial-data access. Only the room button may prompt.
            if (!RaidersPermissions.Has(RaidersPermissions.Scene))
            { Availability = RoomAvailability.PermissionRequired; return; }
            await Load(head, false);
        }
        public Task<bool> Load(Transform head, bool requestPermission = true)
        {
            if (loading != null) return loading;
            selection?.Dispose();
            selection = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            return loading = LoadRoom(head, requestPermission);
        }
        private async Task<bool> LoadRoom(Transform head, bool requestPermission)
        {
            // Yield first so even synchronous permission results cannot leave a completed task cached.
            await Task.Yield();
            try
            {
                Availability = RoomAvailability.Checking;
                Status = "Checking your saved room…";
                var cancellation = selection.Token;
                bool allowed = RaidersPermissions.Has(RaidersPermissions.Scene);
                if (!allowed && requestPermission)
                {
                    Status = "Allow spatial data access to use your room scan.";
                    allowed = await RaidersPermissions.Request(RaidersPermissions.Scene, cancellation);
                }
                cancellation.ThrowIfCancellationRequested();
                if (!allowed)
                { Availability = RoomAvailability.PermissionRequired; Status = "Room access is off. Play in VR, or enable Spatial Data in app permissions."; return false; }
                if (!this) return false;
                if (!mruk) throw new InvalidOperationException("MRUK is not configured.");
                Status = "Checking your saved room…";
                // A timed-out native load may still finish. Reuse it instead of starting overlapping loads.
                if (sceneLoad == null || sceneLoad.IsCompleted)
                    sceneLoad = mruk.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound: false);
                var completed = await Task.WhenAny(sceneLoad, Task.Delay(15000, cancellation));
                cancellation.ThrowIfCancellationRequested();
                if (completed != sceneLoad) throw new TimeoutException("Room loading timed out.");
                var result = await sceneLoad;
                if (!this) return false;
                if (result == MRUK.LoadDeviceResult.NoRoomsFound ||
                    (result == MRUK.LoadDeviceResult.Success && !FindContainingRoom(head)))
                { Availability = RoomAvailability.NotFound; Status = "Your saved room isn't available here. Play in VR instead."; return false; }
                if (result != MRUK.LoadDeviceResult.Success)
                { Availability = RoomAvailability.Unavailable; Status = "Room scan unavailable. Play in VR instead, or try again."; return false; }
                Availability = RoomAvailability.Available;
                Status = "Room scan loaded. Placing the yard inside your room.";
                return true;
            }
            catch (OperationCanceledException) { if (this) { Availability = RoomAvailability.Unknown; Status = "Room check cancelled."; } return false; }
            catch (Exception ex)
            { if (this) { Availability = RoomAvailability.Unavailable; Status = "Room scan unavailable. Play in VR instead, or try again."; } Debug.LogWarning("[SR Room] " + ex.Message); return false; }
            finally { if (this) loading = null; }
        }
#if UNITY_EDITOR
        // Private, nonserialized synthetic room-containment seam. Native room validation is
        // unavailable in Edit Mode. Never used in Play Mode and absent from player builds.
        private static Func<MRUKRoom,Vector3,bool> containsFixture;
#endif
        private static bool Contains(MRUKRoom room,Vector3 point)
        {
#if UNITY_EDITOR
            if(!Application.isPlaying && containsFixture!=null)return containsFixture(room,point);
#endif
            return room.IsPositionInRoom(point,testVerticalBounds:true);
        }
        public MRUKRoom FindContainingRoom(Transform head)
        {
            if (!mruk || !head) return null;
            // GetCurrentRoom can return a previous/first saved room even when the user is outside it.
            foreach (var room in mruk.Rooms)
                if (room && Contains(room,head.position)) return room;
            return null;
        }
        [Header("MR baseline calibration (calculated, not headset measured)")]
        [Range(.35f,1f), Tooltip("Previous rectangular board's confirmed scale, if known. Default 1 is the recorded full-scale calculated reference. Not relative zoom.")]
        public float previousMRReferenceScale=1f;
        public float NormalScale=>TabletopViewContract.MRBaselineScale(previousMRReferenceScale);
        public enum PlacementKind { None, Surface, Free }
        public PlacementKind PreviewKind { get; private set; }
        public PlacementKind ConfirmedKind { get; private set; }
        public bool IsFree=>ConfirmedKind==PlacementKind.Free;
        public int ConfirmationVersion { get; private set; }
        private MRUKRoom candidateRoom;
        private MRUKAnchor candidate, confirmed;
        private MRUKRoom confirmedRoom;
        private Pose candidatePose, confirmedLocal;
        private float candidateScale, confirmedScale;
        public bool HasPreview => PreviewKind!=PlacementKind.None;
        public bool TryPlace(Transform head,out Pose pose,bool surfaceOnly=false)
        {
            pose=default; CancelPreview();
            var room=FindContainingRoom(head);
            if(!room){Status="You are outside the loaded room. Return to the scanned room, Retry, or Play in VR.";return false;}
            float best=float.MaxValue;
            foreach(var anchor in room.Anchors)
            {
                if(!anchor || !anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))continue;
                var centre=anchor.VolumeBounds.HasValue ? anchor.transform.TransformPoint(anchor.VolumeBounds.Value.center) :
                    anchor.PlaneRect.HasValue ? anchor.transform.TransformPoint(new Vector3(anchor.PlaneRect.Value.center.x,anchor.PlaneRect.Value.center.y,0)) : anchor.transform.position;
                if(!anchor.Raycast(new Ray(centre+Vector3.up*2,Vector3.down),4,out var hit) || Vector3.Dot(hit.normal,Vector3.up)<.98f)continue;
                float floor=room.FloorAnchors.Count>0?room.FloorAnchors[0].transform.position.y:0;
                if(hit.point.y<floor+.45f || hit.point.y>head.position.y-.12f || Vector3.Distance(hit.point,head.position)>3)continue;
                var x=Vector3.ProjectOnPlane(anchor.transform.right,Vector3.up);
                if(x.sqrMagnitude<.1f)x=Vector3.ProjectOnPlane(anchor.transform.up,Vector3.up);
                var baseRotation=Quaternion.LookRotation(Vector3.Cross(x.normalized,Vector3.up),Vector3.up);
                for(int orientation=0;orientation<2;orientation++)
                {
                var rotation=baseRotation*Quaternion.Euler(0,orientation*90,0);
                {
                    float scale=NormalScale; // Never shrink to satisfy a scanned table.
                    var pos=hit.point+Vector3.up*(.006f-TabletopViewContract.TrayBottom*scale);
                    if(!SurfaceClearance(head,pos,scale))continue;
                    if(!FitsSurface(room,anchor,pos,rotation,scale))continue;
                    float score=Vector3.Distance(pos,head.position);
                    if(score<best){best=score;candidate=anchor;candidatePose=new Pose(pos,rotation);candidateScale=scale;candidateRoom=room;PreviewKind=PlacementKind.Surface;}
                }
                }
            }
            if(!candidate)
            {
                if(surfaceOnly){Status="No suitable table at this size and height. Free MR stays full size. Recenter or retry a larger, lower table.";return false;}
                candidateRoom=room;candidateScale=NormalScale;candidatePose=FreePose(head,room);PreviewKind=PlacementKind.Free;
            }
            pose=candidatePose;
            Status=PreviewKind==PlacementKind.Surface?
                "Surface preview: full-size tray fits. Choose Use your room again to confirm.":
                "Free MR preview. Not attached to furniture. Use your room again to confirm.";
            return true;
        }
        public static bool SurfaceClearance(Transform head,Vector3 origin,float scale)=>head &&
            origin.y+TabletopViewContract.RimHeight*scale<=head.position.y-TabletopViewContract.MinimumMRRimBelowEyes;
        private static float FloorHeight(MRUKRoom room)=>room.FloorAnchors.Count>0?room.FloorAnchors[0].transform.position.y:room.transform.position.y;
        public bool TryFloorHeight(out float floor)
        {floor=confirmedRoom?FloorHeight(confirmedRoom):0;return confirmedRoom;}
        private Pose FreePose(Transform head,MRUKRoom room)=>TabletopViewMath.ReachablePose(head,NormalScale,FloorHeight(room));
        public float PreviewScale => candidateScale;
        public bool Confirm(Transform head)
        {
            var room=FindContainingRoom(head);
            if(!HasPreview || !room || room!=candidateRoom || !Mathf.Approximately(candidateScale,NormalScale) ||
                (PreviewKind==PlacementKind.Surface && (!candidate || !room.Anchors.Contains(candidate) || !SurfaceClearance(head,candidatePose.position,candidateScale) || !FitsSurface(room,candidate,candidatePose.position,candidatePose.rotation,candidateScale))))
            {Status="Placement changed. Retry to preview the enlarged board.";CancelPreview();return false;}
            confirmed=candidate;confirmedRoom=room;confirmedScale=candidateScale;ConfirmedKind=PreviewKind;
            var frame=IsFree?room.transform:confirmed.transform;
            confirmedLocal=new Pose(frame.InverseTransformPoint(candidatePose.position),Quaternion.Inverse(frame.rotation)*candidatePose.rotation);
            ConfirmationVersion++;CancelPreview();return true;
        }
        public void CancelPreview(){candidate=null;candidateRoom=null;PreviewKind=PlacementKind.None;}
        public bool TryConfirmed(Transform head,out Pose pose,out float scale)
        {
            pose=default;scale=confirmedScale;
            if(ConfirmedKind==PlacementKind.None || !confirmedRoom || FindContainingRoom(head)!=confirmedRoom || !Mathf.Approximately(confirmedScale,NormalScale) ||
                (!IsFree && (!confirmed || !confirmedRoom.Anchors.Contains(confirmed))))
            {Status="MR placement is no longer available. Return to the scanned room or retry placement.";return false;}
            var frame=IsFree?confirmedRoom.transform:confirmed.transform;
            pose=new Pose(frame.TransformPoint(confirmedLocal.position),frame.rotation*confirmedLocal.rotation);
            if(IsFree || (SurfaceClearance(head,pose.position,scale) && FitsConfirmed(pose.position,pose.rotation,scale)))return true;
            Status="Confirmed table no longer fits the enlarged tray. Retry placement or switch to free MR.";return false;
        }
        public bool UseFreePlacement(Transform head,Pose pose)
        {
            var room=FindContainingRoom(head);if(!room)return false;
            CancelPreview();confirmed=null;confirmedRoom=room;confirmedScale=NormalScale;ConfirmedKind=PlacementKind.Free;
            confirmedLocal=new Pose(room.transform.InverseTransformPoint(pose.position),Quaternion.Inverse(room.transform.rotation)*pose.rotation);
            ConfirmationVersion++;Status="Free MR - not attached to furniture. Grab anywhere with both hands; Recenter returns the near edge to you.";return true;
        }
        public bool RecenterFree(Transform head)
        {var room=FindContainingRoom(head);return room && UseFreePlacement(head,FreePose(head,room));}
        public bool FitsConfirmed(Vector3 position,Quaternion rotation,float scale)=>!IsFree && confirmed && confirmedRoom && FitsSurface(confirmedRoom,confirmed,position,rotation,scale);
        public static bool FitsAnchor(MRUKAnchor anchor,Vector3 position,Quaternion rotation,float scale)
        {
            if(!anchor || float.IsNaN(scale) || float.IsInfinity(scale) || scale<=0 || !TabletopViewMath.Finite(position) || !TabletopViewMath.Finite(rotation*Vector3.forward) || Vector3.Dot(rotation*Vector3.up,Vector3.up)<.999f)return false;
            for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)
            {
                var point=FootprintPoint(position,rotation,scale,x,z);
                point.y=position.y+TabletopViewContract.TrayBottom*scale-.006f;
                if(!anchor.Raycast(new Ray(point+Vector3.up*.2f,Vector3.down),.3f,out var hit) ||
                    Vector3.Dot(hit.normal,Vector3.up)<.98f || Mathf.Abs(hit.point.y-(position.y+TabletopViewContract.TrayBottom*scale-.006f))>.015f)return false;
            }
            return true;
        }
        private static Vector3 FootprintPoint(Vector3 position,Quaternion rotation,float scale,int x,int z) =>
            position+rotation*new Vector3(x/3f*(TabletopViewContract.OuterSize*.5f*scale+.015f),0,z/3f*(TabletopViewContract.OuterSize*.5f*scale+.015f));
        public static bool FitsSurface(MRUKRoom room,MRUKAnchor anchor,Vector3 position,Quaternion rotation,float scale)
        {
            if(!room || !FitsAnchor(anchor,position,rotation,scale))return false;
            for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)
            {
                var point=FootprintPoint(position,rotation,scale,x,z);
                if(!Contains(room,point))return false;
                foreach(var obstruction in room.Anchors)
                {
                    if(!obstruction || obstruction==anchor || !obstruction.VolumeBounds.HasValue)continue;
                    if(obstruction.VolumeBounds.Value.Contains(obstruction.transform.InverseTransformPoint(point+Vector3.up*.08f)))return false;
                }
            }
            return true;
        }
        private void OnDestroy() { selection?.Cancel(); selection?.Dispose(); }
    }
}
