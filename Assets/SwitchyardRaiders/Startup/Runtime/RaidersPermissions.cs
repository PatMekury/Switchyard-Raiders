using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;

namespace SwitchyardRaiders.Startup
{
    // One owner for system dialogs: OVRManager, MRUK and Unity automatic prompts are disabled.
    public static class RaidersPermissions
    {
        public const string Scene = "com.oculus.permission.USE_SCENE";
        public const string Microphone = "android.permission.RECORD_AUDIO";
        private static readonly SemaphoreSlim Dialog = new(1, 1);
        public static bool Has(string permission)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(permission);
#else
            return permission != Microphone || Application.HasUserAuthorization(UserAuthorization.Microphone);
#endif
        }
        public static async Task<bool> Request(string permission, CancellationToken cancellation = default)
        {
            await Dialog.WaitAsync(cancellation);
            try
            {
                cancellation.ThrowIfCancellationRequested();
                if (Has(permission)) return true;
#if UNITY_ANDROID && !UNITY_EDITOR
                var completion = new TaskCompletionSource<bool>();
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => completion.TrySetResult(true);
                callbacks.PermissionDenied += _ => completion.TrySetResult(false);
                Permission.RequestUserPermission(permission, callbacks);
                // Keep the queue locked until the actual system dialog finishes, even if its caller leaves.
                bool granted = await completion.Task;
                cancellation.ThrowIfCancellationRequested();
                return granted;
#else
                if (permission == Microphone)
                {
                    var request = Application.RequestUserAuthorization(UserAuthorization.Microphone);
                    while (!request.isDone) { cancellation.ThrowIfCancellationRequested(); await Task.Yield(); }
                }
                return Has(permission);
#endif
            }
            finally { Dialog.Release(); }
        }
    }
}
