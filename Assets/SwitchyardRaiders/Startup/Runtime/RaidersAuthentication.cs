using System;
using System.Threading;
using System.Threading.Tasks;
using Oculus.Platform;

namespace SwitchyardRaiders.Startup
{
    public static class RaidersAuthentication
    {
        public static async Task<string> FreshProof(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var proof = Fetch();
            if (await Task.WhenAny(proof, Task.Delay(15000, cancellation)) != proof)
            {
                cancellation.ThrowIfCancellationRequested();
                throw new TimeoutException("Meta user verification timed out. Retry the connection.");
            }
            cancellation.ThrowIfCancellationRequested();
            var value = await proof;
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException("Meta user verification failed. Check User ID access and test-account entitlement.");
            return value;
        }
        private static async Task<string> Fetch()
        {
            var result = await Users.GetUserProof();
            return result.IsError || result.Data == null ? null : result.Data.Value;
        }
    }
}
