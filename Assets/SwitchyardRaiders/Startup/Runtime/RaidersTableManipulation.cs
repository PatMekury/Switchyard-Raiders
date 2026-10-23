using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    // ISDK recognizes contact and palm closure. This transformer controls only the personal view.
    public sealed class RaidersTableManipulation : MonoBehaviour, ITransformer
    {
        public RaidersTable table;
        public OVRHand leftHand, rightHand;
        public OVRVignette vignette;
        public HandGrabInteractable handGrab;
        public Grabbable grabbable;
        public BoxCollider[] grabEdges;
        [Range(0,1)] public float comfort = .5f;
        public bool Dragging { get; private set; }
        public int GrabCount => source == null ? 0 : source.GrabPoints.Count;
        public bool BlocksGameplay => GrabCount > 0 || Dragging || Time.unscaledTime < releaseUntil;
        private IGrabbable source;
        private bool focused = true, paused;
        private float releaseUntil, startScale;
        private Vector3 startLeft, startRight, startPosition;
        private Quaternion startRotation;

        public void Initialize(IGrabbable value) => source = value;
        public void BeginTransform()
        {
            EndTransform();
            HoldTray(GrabCount > 0);
            // One closed hand holds its contact, but never moves the table.
            if (!table || !table.CanManipulate || GrabCount != 2 || !focused || paused) return;
            startLeft = source.GrabPoints[0].position;
            startRight = source.GrabPoints[1].position;
            if (Vector3.Distance(startLeft,startRight) < .08f) return;
            startPosition = table.presentation.position;
            startRotation = table.presentation.rotation;
            startScale = table.presentation.localScale.x;
            Dragging = true;
        }
        public void UpdateTransform()
        {
            if (!Dragging || GrabCount != 2 || !table.CanManipulate || !focused || paused) { EndTransform(); return; }
            TabletopViewMath.Solve(startPosition,startRotation,startScale,startLeft,startRight,
                source.GrabPoints[0].position,source.GrabPoints[1].position,.2f,12f,out var pos,out var rot,out float scale);
            if (!table.roomMenu.IsVirtual && !table.Exploration) pos.y = table.SurfaceHeight;
            if (table.AcceptView(pos,rot,scale))
            {
                bool moving = Vector3.Distance(pos,table.presentation.position) > .0005f ||
                    Quaternion.Angle(rot,table.presentation.rotation) > .05f || Mathf.Abs(scale-table.presentation.localScale.x) > .0005f;
                table.presentation.SetPositionAndRotation(pos,rot);
                table.presentation.localScale = Vector3.one*scale;
                SyncGrabSurface();
                if (vignette) { vignette.VignetteFieldOfView = Mathf.Lerp(110,65,comfort); vignette.enabled = moving && table.roomMenu.IsVirtual && comfort > 0; }
            }
            else if (vignette) vignette.enabled = false;
        }
        public void EndTransform()
        {
            if (Dragging) releaseUntil = Time.unscaledTime+.25f;
            Dragging = false;
            if (vignette) vignette.enabled = false;
            HoldTray(false);
        }
        private void HoldTray(bool hold)
        {
            if (table && table.roomMenu && table.roomMenu.follower) table.roomMenu.follower.HoldFor(this,hold);
        }
        public void Suspend()
        {
            if (handGrab) handGrab.enabled = false;
            EndTransform();
        }
        public void SyncGrabSurface()
        {
            if (!table || !handGrab || grabEdges == null || grabEdges.Length != 4) return;
            var target = table.presentation;
            handGrab.transform.SetPositionAndRotation(target.position,target.rotation);
            // Keep the SDK Rigidbody at unit scale and resize its contact colliders with the view.
            float scale = target.lossyScale.x;
            bool rail = table.roomMenu.IsVirtual && target.localScale.x < 1.6f;
            float x = rail ? .95f : .75f, z = rail ? .7f : .525f, y = rail ? .015f : .15f;
            for (int i=0;i<4;i++)
            {
                bool side = i < 2;
                grabEdges[i].center = new Vector3(side ? (i==0?-x:x) : 0,y,side ? 0 : (i==2?-z:z))*scale;
                grabEdges[i].size = new Vector3(side ? .08f : x*2,.10f,side ? z*2 : .08f)*scale;
            }
        }
        private void LateUpdate()
        {
            SyncGrabSurface();
            if (handGrab) handGrab.enabled = table && table.CanManipulate && focused && !paused;
            HoldTray(GrabCount > 0);
        }
        private void OnApplicationFocus(bool focus) { focused=focus; if(!focus)Suspend(); }
        private void OnApplicationPause(bool pause) { paused=pause; if(pause)Suspend(); }
        private void OnDisable() => Suspend();
    }
}
