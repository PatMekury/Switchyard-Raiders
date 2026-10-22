using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    public sealed class RaidersVoice : MonoBehaviour
    {
        public GameObject runnerPrefab;
        private FusionVoiceClient client;
        private Recorder recorder;
        private CancellationTokenSource lifetime;
        private bool requesting;
        private bool authenticating;
        private ulong userId;
        private string connectionStatus = "";
        private string permissionStatus = "";
        public bool Muted { get; private set; }
        public bool Active => client != null;
        public string Status
        {
            get
            {
                if (!client) return Muted ? "Microphone muted" : "Voice starts in online sessions";
                if (!client.Client.InRoom && !string.IsNullOrEmpty(connectionStatus)) return connectionStatus;
                if (requesting) return "Allow microphone access to talk";
                if (!string.IsNullOrEmpty(permissionStatus)) return permissionStatus;
                if (client.Client.InRoom)
                {
                    if (Muted) return "Voice connected • microphone muted";
                    if (!recorder || recorder.LevelMeter == null) return "Listen only • microphone unavailable; Retry voice";
                    return recorder.IsCurrentlyTransmitting ? "Voice connected • speaking" : "Voice connected • microphone on";
                }
                return "Voice: " + client.ClientState + (client.Client.IsConnected ? "" : " (" + client.Client.DisconnectedCause + ") • Retry voice");
            }
        }
        private void Awake() { Muted = PlayerPrefs.GetInt("SR.MicMuted", 0) != 0; }
        public NetworkRunner CreateRunner(Transform parent)
        {
            Stop();
            if (!runnerPrefab) throw new InvalidOperationException("Voice runner prefab is missing.");
            var settings = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings;
            if (string.IsNullOrWhiteSpace(settings.AppIdFusion) || string.IsNullOrWhiteSpace(settings.AppIdVoice))
                throw new InvalidOperationException("Configure the Fusion and Voice app IDs before matchmaking.");
            var go = Instantiate(runnerPrefab, parent);
            go.name = "Raiders session runner";
            client = go.GetComponent<FusionVoiceClient>();
            recorder = client.PrimaryRecorder;
            // The prefab is inactive with recording disabled, so Awake cannot open the microphone early.
            recorder.RecordWhenJoined = false;
            recorder.RecordingEnabled = recorder.TransmitEnabled = false;
            client.AutoConnectAndJoin = false;
            client.UseFusionAuthValues = false;
            lifetime = new CancellationTokenSource();
            go.SetActive(true);
            return go.GetComponent<NetworkRunner>();
        }
        public void Joined(ulong metaUserId)
        {
            userId = metaUserId;
            ConnectAuthenticated();
            if (!Muted) RequestMicrophone();
        }
        private async void ConnectAuthenticated()
        {
            if (!client || lifetime == null || authenticating || client.Client.IsConnected || userId == 0) return;
            var current = lifetime;
            authenticating = true; connectionStatus = "Verifying voice identity…";
            try
            {
                // Meta nonces are single use: never copy the nonce consumed by Fusion into Voice.
                var proof = await RaidersAuthentication.FreshProof(current.Token);
                if (!this || lifetime != current || !client) return;
                var auth = new Photon.Realtime.AuthenticationValues(userId.ToString(System.Globalization.CultureInfo.InvariantCulture))
                { AuthType = Photon.Realtime.CustomAuthenticationType.Oculus };
                auth.AddAuthParameter("userid", auth.UserId); auth.AddAuthParameter("nonce", proof);
                client.Client.AuthValues = auth;
                connectionStatus = client.ConnectAndJoinRoom() ? "" : "Voice connection failed • Retry voice";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            { if (this && lifetime == current) connectionStatus = "Voice verification failed • Retry voice"; Debug.LogWarning("[SR Voice] " + ex.Message); }
            finally { if (this && lifetime == current) authenticating = false; }
        }
        public void ToggleMute()
        {
            Muted = !Muted;
            PlayerPrefs.SetInt("SR.MicMuted", Muted ? 1 : 0);
            if (recorder) recorder.RecordingEnabled = recorder.TransmitEnabled = false;
            if (!Muted && client) RequestMicrophone();
        }
        public void Retry()
        {
            if (!client) return;
            if (!client.Client.IsConnected) ConnectAuthenticated();
            if (!Muted) RequestMicrophone();
        }
        private async void RequestMicrophone()
        {
            if (requesting || !client || lifetime == null) return;
            var current = lifetime;
            requesting = true; permissionStatus = "";
            try
            {
                bool granted = await RaidersPermissions.Request(RaidersPermissions.Microphone, current.Token);
                if (!this || lifetime != current || !recorder) return;
                permissionStatus = granted ? "" : "Listen only • enable Microphone in app permissions, then Retry voice";
                recorder.RecordingEnabled = recorder.TransmitEnabled = granted && !Muted;
                if (granted && !Muted) recorder.RestartRecording();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (this && lifetime == current) permissionStatus = "Microphone unavailable • Retry voice"; Debug.LogWarning("[SR Voice] " + ex.Message); }
            finally { if (this && lifetime == current) requesting = false; }
        }
        private void OnApplicationFocus(bool focus)
        {
            if (focus && recorder && !RaidersPermissions.Has(RaidersPermissions.Microphone))
            {
                recorder.RecordingEnabled = recorder.TransmitEnabled = false;
                permissionStatus = "Listen only • microphone permission required";
            }
        }
        public void Stop()
        {
            lifetime?.Cancel(); lifetime?.Dispose(); lifetime = null;
            requesting = authenticating = false; permissionStatus = connectionStatus = ""; userId = 0;
            if (recorder) recorder.RecordingEnabled = recorder.TransmitEnabled = false;
            if (client)
            {
                client.AutoConnectAndJoin = false;
                if (client.Client.IsConnected) client.Disconnect();
            }
            recorder = null; client = null;
        }
        private void OnDestroy() => Stop();
    }
}
