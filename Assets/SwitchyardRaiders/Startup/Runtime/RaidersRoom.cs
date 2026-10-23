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
        public MRUKRoom FindContainingRoom(Transform head)
        {
            if (!mruk || !head) return null;
            // GetCurrentRoom can return a previous/first saved room even when the user is outside it.
            foreach (var room in mruk.Rooms)
                if (room && room.IsPositionInRoom(head.position, testVerticalBounds: true)) return room;
            return null;
        }
        private MRUKAnchor candidate, confirmed;
        private MRUKRoom confirmedRoom;
        private Pose candidatePose, confirmedLocal;
        private float candidateScale, confirmedScale;
        public bool HasPreview => candidate;
        public bool TryPlace(Transform head,out Pose pose)
        {
            pose=default; candidate=null;
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
                for(float scale=1;scale>=.349f;scale-=.05f)
                {
                    var pos=hit.point+Vector3.up*.006f;
                    if(!FitsSurface(room,anchor,pos,rotation,scale))continue;
                    float score=Vector3.Distance(pos,head.position)+(1-scale);
                    if(score<best){best=score;candidate=anchor;candidatePose=new Pose(pos,rotation);candidateScale=scale;}
                    break;
                }
                }
            }
            if(!candidate){Status="No scanned tabletop fits the terrain. Scan a clear table, Retry, or Play in VR.";return false;}
            pose=candidatePose;Status="Preview on your tabletop ("+Mathf.RoundToInt(candidateScale*100)+"%). Choose Use your room again to confirm.";return true;
        }
        public float PreviewScale => candidateScale;
        public bool Confirm(Transform head)
        {
            var room=FindContainingRoom(head);
            if(!candidate || !room || !room.Anchors.Contains(candidate) || !FitsSurface(room,candidate,candidatePose.position,candidatePose.rotation,candidateScale))
            {Status="Table placement changed. Retry to find a suitable surface.";candidate=null;return false;}
            confirmed=candidate;confirmedRoom=room;confirmedScale=candidateScale;
            confirmedLocal=new Pose(confirmed.transform.InverseTransformPoint(candidatePose.position),Quaternion.Inverse(confirmed.transform.rotation)*candidatePose.rotation);
            candidate=null;return true;
        }
        public void CancelPreview()=>candidate=null;
        public bool TryConfirmed(Transform head,out Pose pose,out float scale)
        {
            pose=default;scale=confirmedScale;
            if(!confirmed || FindContainingRoom(head)!=confirmedRoom){Status="Confirmed tabletop is no longer available. Retry placement or Play in VR.";return false;}
            pose=new Pose(confirmed.transform.TransformPoint(confirmedLocal.position),confirmed.transform.rotation*confirmedLocal.rotation);
            return FitsConfirmed(pose.position,pose.rotation,scale);
        }
        public bool FitsConfirmed(Vector3 position,Quaternion rotation,float scale)=>confirmed && confirmedRoom && FitsSurface(confirmedRoom,confirmed,position,rotation,scale);
        public static bool FitsAnchor(MRUKAnchor anchor,Vector3 position,Quaternion rotation,float scale)
        {
            if(!anchor || scale<.2f || scale>1.01f)return false;
            for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)
            {
                var point=FootprintPoint(position,rotation,scale,x,z);
                if(!anchor.Raycast(new Ray(point+Vector3.up*.2f,Vector3.down),.3f,out var hit) ||
                    Vector3.Dot(hit.normal,Vector3.up)<.98f || Mathf.Abs(hit.point.y-(position.y-.006f))>.015f)return false;
            }
            return true;
        }
        private static Vector3 FootprintPoint(Vector3 position,Quaternion rotation,float scale,int x,int z) =>
            position+rotation*new Vector3(x/3f*(.75f*scale+.015f),0,z/3f*(.525f*scale+.015f));
        public static bool FitsSurface(MRUKRoom room,MRUKAnchor anchor,Vector3 position,Quaternion rotation,float scale)
        {
            if(!room || !FitsAnchor(anchor,position,rotation,scale))return false;
            for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)
            {
                var point=FootprintPoint(position,rotation,scale,x,z);
                if(!room.IsPositionInRoom(point,testVerticalBounds:true))return false;
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
