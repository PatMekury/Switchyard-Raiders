using System.Collections.Generic;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    /// <summary>World-space seated panel with a dead zone and interaction hold.</summary>
    public sealed class SeatedPanelFollow : MonoBehaviour
    {
        public Transform head;
        public float distance = .65f;
        public float heightOffset = -.08f;
        public float repositionThreshold = .6f;
        public float followSpeed = 3f;
        private readonly HashSet<Object> interactionOwners = new();
        private bool manualHold;
        public bool InteractionHeld { get => manualHold || interactionOwners.Count > 0; set => manualHold = value; }
        public void HoldFor(Object owner, bool held)
        {
            if (held) interactionOwners.Add(owner); else interactionOwners.Remove(owner);
        }
        private Vector3 forward = Vector3.forward;
        private bool initialized, moving;

        public void Snap(Transform target)
        {
            head = target;
            if (!head) return;
            forward = FlatForward();
            transform.SetPositionAndRotation(head.position + forward * distance + Vector3.up * heightOffset,
                Quaternion.LookRotation(forward, Vector3.up));
            initialized = true;
            moving = false;
        }

        private Vector3 FlatForward()
        {
            Vector3 f = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            return f.sqrMagnitude > .01f ? f.normalized : forward;
        }

        private void LateUpdate()
        {
            if (!head || InteractionHeld) return;
            if (!initialized) { Snap(head); return; }
            // Match Spirit Sling CanvasFollowPlayer: smoothed flat heading,
            // gravity-aligned vertical offset, and world-space movement hysteresis.
            float t = Mathf.Clamp01(followSpeed * Time.deltaTime);
            var flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            forward = Vector3.Lerp(forward, flat, t).normalized;
            Vector3 wanted = head.position + forward * distance + Vector3.up * heightOffset;
            float delta = Vector3.Distance(wanted, transform.position);
            if (delta < .02f) moving = false;
            else if (delta > repositionThreshold) moving = true;
            if (!moving) return;
            transform.position = Vector3.Lerp(transform.position, wanted, t);
            transform.rotation = Quaternion.LookRotation(forward);
        }
    }
}
