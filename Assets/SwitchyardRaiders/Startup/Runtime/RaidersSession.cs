using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    public enum RaidersSessionPhase { Idle, Connecting, Waiting, Ready, Countdown, Playing, Finished, Leaving, Failed }
    public sealed class RaidersSession : MonoBehaviour, INetworkRunnerCallbacks
    {
#if UNITY_EDITOR
        [SerializeField, InspectorName("Solo 1v1 Test — Control Both Players")]
        [Tooltip("Local editor session only. Switch control between two independent players; simulation remains simultaneous. Never used by public/private matchmaking.")]
        private bool solo1v1TestControlBothPlayers;
#endif
        public RaidersSessionPhase Phase { get; private set; }
        public string Status { get; private set; } = "";
        public string RoomCode { get; private set; } = "";
        public RaidersMatch Match { get; private set; }
        public int ControlledSlot { get; private set; }
        public bool IsLocal { get; private set; }
        public bool LocalReady => ready[ControlledSlot];
        public int PlayerCount => occupied[0] + occupied[1];
        public float Countdown { get; private set; }
        public event Action Changed;
        public event Action<int, ulong, byte[]> AvatarPacket;
        public RaidersAvatars avatars;
        public RaidersVoice voice;
        public bool EditorTestEnabled
        {
            get
            {
#if UNITY_EDITOR
                return solo1v1TestControlBothPlayers;
#else
                return false;
#endif
            }
        }
        public bool CanSwitchPlayers
        {
            get
            {
#if UNITY_EDITOR
                return IsLocal && solo1v1TestControlBothPlayers && Match != null && !Match.practice;
#else
                return false;
#endif
            }
        }
        private NetworkRunner runner;
        private CancellationTokenSource connecting;
        private int generation, packetSequence;
        private float snapshotTime, lastPacket, countdownEnd;
        private readonly PlayerRef[] owners = new PlayerRef[2];
        private readonly int[] occupied = new int[2];
        private readonly bool[] ready = new bool[2];
        private readonly float[] nextCommand = new float[2], nextAvatar = new float[2];
        private bool busy, closing;
        private string lastRoute, lastCode;
        [Serializable] private sealed class Packet
        {
            public int kind, slot, action, index;
            public float value, countdown;
            public string status;
            public int phase, count0, count1;
            public bool ready0, ready1;
            public RaidersMatch match;
        }
        private void Set(RaidersSessionPhase phase, string message)
        { Phase = phase; Status = message; Changed?.Invoke(); }
        public void StartPractice() => StartLocal(true);
        public void StartEditorTest()
        {
#if UNITY_EDITOR
            if (solo1v1TestControlBothPlayers) StartLocal(false);
#endif
        }
        private void StartLocal(bool practice)
        {
            if (busy || runner || Phase != RaidersSessionPhase.Idle) return;
            IsLocal = true; ControlledSlot = 0; occupied[0] = 1; occupied[1] = practice ? 0 : 1;
            Match = new RaidersMatch(); Match.Reset(practice); ready[0] = ready[1] = false;
            RoomCode = ""; Set(RaidersSessionPhase.Ready, practice ? "Practice yard ready" : "Local 1v1 — two independent players");
        }
        public void SwitchPlayer()
        {
            if (!CanSwitchPlayers || Phase != RaidersSessionPhase.Playing) return;
            ControlledSlot = 1 - ControlledSlot; Status = "Controlling Player " + (ControlledSlot + 1); Changed?.Invoke();
        }
        public void FindMatch() => Begin("public", "");
        public void HostPrivate() => Begin("host", Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant());
        public void JoinPrivate(string code)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length != 6) { Set(RaidersSessionPhase.Failed, "Enter the six-character room code."); return; }
            foreach(char c in code) if (!Uri.IsHexDigit(c)) { Set(RaidersSessionPhase.Failed, "Room codes use 0–9 and A–F."); return; }
            Begin("join", code);
        }
        public void Retry() { if (!busy && Phase == RaidersSessionPhase.Failed && !string.IsNullOrEmpty(lastRoute)) Begin(lastRoute, lastCode); }
        private async void Begin(string route, string code)
        {
            if (busy || runner || (Phase != RaidersSessionPhase.Idle && Phase != RaidersSessionPhase.Failed)) return;
            busy = true; closing = false; IsLocal = false; lastRoute = route; lastCode = code;
            int ticket = ++generation;
            connecting = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                Set(RaidersSessionPhase.Connecting, route == "public" ? "Searching for a 1v1 match…" : "Connecting to private session…");
                if(!avatars)throw new InvalidOperationException("Meta identity service is not configured.");
                var identityTask=avatars.EnsureIdentity();
                if(await Task.WhenAny(identityTask,Task.Delay(30000,connecting.Token))!=identityTask)
                    throw new OperationCanceledException();
                await identityTask;
                if(ticket!=generation)return;
                var proof = await RaidersAuthentication.FreshProof(connecting.Token);
                if(ticket!=generation)return;
                var auth = new Photon.Realtime.AuthenticationValues(avatars.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture))
                { AuthType = Photon.Realtime.CustomAuthenticationType.Oculus };
                auth.AddAuthParameter("userid", auth.UserId);
                auth.AddAuthParameter("nonce", proof);
                if (!voice) throw new InvalidOperationException("Photon Voice service is not configured.");
                var active = voice.CreateRunner(transform); runner = active; active.AddCallbacks(this);
                var result = await active.StartGame(new StartGameArgs
                {
                    GameMode = route == "public" ? GameMode.AutoHostOrClient : route == "host" ? GameMode.Host : GameMode.Client,
                    SessionName = route == "public" ? null : "SR1_" + code,
                    CustomLobbyName = "SwitchyardRaiders_1v1_v1",
                    PlayerCount = 2, IsVisible = route == "public", IsOpen = true,
                    EnableClientSessionCreation = route != "join",
                    SessionProperties = new Dictionary<string, SessionProperty> { { "sr_rules", 1 }, { "sr_players", 2 } },
                    StartGameCancellationToken = connecting.Token,
                    AuthValues = auth
                });
                if (ticket != generation) return;
                if (!result.Ok) throw new InvalidOperationException("Connection failed: " + result.ShutdownReason);
                voice.Joined(avatars.UserId);
                RoomCode = route == "public" ? "Public 1v1" : code;
                lastPacket = Time.unscaledTime;
                if (active.IsServer)
                {
                    Match = new RaidersMatch();
                    foreach(var player in active.ActivePlayers) Assign(player);
                    UpdateLobby();
                }
                else Set(RaidersSessionPhase.Waiting, "Connected. Waiting for the host…");
            }
            catch (Exception ex)
            {
                if (ticket == generation)
                {
                    await StopRunner(); ResetState();
                    Set(RaidersSessionPhase.Failed, ex is OperationCanceledException ? "Connection timed out. Retry or return to the menu." : ex.Message);
                }
            }
            finally { if(ticket == generation) { connecting?.Dispose(); connecting = null; busy = false; } }
        }
        private void Assign(PlayerRef player)
        {
            for(int i=0;i<2;i++) if(occupied[i]!=0 && owners[i]==player) return;
            for(int i=0;i<2;i++) if(occupied[i]==0) { occupied[i]=1; owners[i]=player; ready[i]=false; if(player==runner.LocalPlayer)ControlledSlot=i; return; }
        }
        private int Slot(PlayerRef player)
        { for(int i=0;i<2;i++) if(occupied[i]!=0 && owners[i]==player)return i; return -1; }
        private void UpdateLobby()
        {
            if (Phase == RaidersSessionPhase.Playing || Phase == RaidersSessionPhase.Finished || Phase == RaidersSessionPhase.Countdown) return;
            Set(PlayerCount == 2 ? RaidersSessionPhase.Ready : RaidersSessionPhase.Waiting,
                PlayerCount == 2 ? "Both players connected. Ready when you are." : "Waiting for another player…");
            Broadcast();
        }
        public void ToggleReady()
        {
            if (Phase != RaidersSessionPhase.Ready && Phase != RaidersSessionPhase.Waiting) return;
            if(IsLocal || (runner && runner.IsServer)) SetReady(ControlledSlot, !LocalReady);
            else Send(new Packet {kind=1, value=LocalReady ? 0 : 1});
        }
        private void SetReady(int slot, bool value)
        {
            if (slot < 0 || slot > 1 || occupied[slot] == 0 || (Phase != RaidersSessionPhase.Ready && Phase != RaidersSessionPhase.Waiting)) return;
            ready[slot]=value;
            if(IsLocal && CanSwitchPlayers)ready[1]=value;
            if(ready[0] && ((Match.practice && IsLocal) || (occupied[1]!=0 && ready[1])))
            {
                if(runner && runner.IsServer) { runner.SessionInfo.IsOpen=false; runner.SessionInfo.IsVisible=false; }
                countdownEnd=Time.unscaledTime+3; Countdown=3; Set(RaidersSessionPhase.Countdown,"Departing in 3…");
            }
            else { Status=value?"Ready — waiting for the other player…":"Choose Ready to depart"; Changed?.Invoke(); }
            Broadcast();
        }
        public void Command(RaidersAction action, int index=0, float value=0)
        {
            if(Phase!=RaidersSessionPhase.Playing)return;
            if(IsLocal || (runner && runner.IsServer)) Apply(ControlledSlot, action,index,value);
            else Send(new Packet{kind=2,action=(int)action,index=index,value=value});
        }
        private void Apply(int slot,RaidersAction action,int index,float value)
        {
            if(Phase!=RaidersSessionPhase.Playing || Match==null || slot<0 || slot>1)return;
            bool accepted=Match.Apply(slot,action,index,value,out string error);
            if(!string.IsNullOrEmpty(error))
            {
                if(slot==ControlledSlot){Status=error;Changed?.Invoke();}
                else if(runner && runner.IsServer) SendTo(owners[slot],new Packet{kind=4,status=error});
            }
            if(accepted)Broadcast();
        }
        public void Rematch()
        {
            if(Phase!=RaidersSessionPhase.Finished)return;
            if(IsLocal || (runner && runner.IsServer)) RequestRematch(ControlledSlot);
            else Send(new Packet{kind=3});
        }
        private void RequestRematch(int slot)
        {
            if(Phase!=RaidersSessionPhase.Finished || slot<0 || slot>1)return;
            ready[slot]=true; if(IsLocal)ready[0]=ready[1]=true;
            if(ready[0] && ready[1]){Match.Reset(Match.practice);ready[0]=ready[1]=false;Set(RaidersSessionPhase.Ready,"Rematch ready. Both players must confirm departure.");}
            else {Status="Rematch requested — waiting for the other player…";Changed?.Invoke();}
            Broadcast();
        }
        private void FixedUpdate()
        {
            if(!(IsLocal || (runner && runner.IsServer)) || Match==null)return;
            if(Phase==RaidersSessionPhase.Countdown)
            {
                Countdown=Mathf.Max(0,countdownEnd-Time.unscaledTime);
                if(Countdown<=0)Set(RaidersSessionPhase.Playing,"Collect and deliver three wagons to win.");
            }
            if(Phase==RaidersSessionPhase.Playing)
            {
                Match.Step(Time.fixedDeltaTime);
                if(Match.winner>=0){ready[0]=ready[1]=false;Set(RaidersSessionPhase.Finished,"Player "+(Match.winner+1)+" wins!");}
            }
            if(Time.unscaledTime>=snapshotTime){snapshotTime=Time.unscaledTime+.1f;Broadcast();}
        }
        private void Update()
        {
            if(!IsLocal && runner && runner.IsRunning && !runner.IsServer && !closing && Time.unscaledTime-lastPacket>15)
                Fail("Connection lost. Return to the menu or retry.");
        }
        public async void Leave()
        {
            if(closing)return; closing=true; ++generation; connecting?.Cancel();
            Set(RaidersSessionPhase.Leaving,"Leaving session…");
            await StopRunner(); connecting?.Dispose();connecting=null; ResetState(); busy=false;closing=false;
            Set(RaidersSessionPhase.Idle,"");
        }
        private async void Fail(string message)
        {
            if(closing)return;closing=true;++generation;connecting?.Cancel();
            await StopRunner();connecting?.Dispose();connecting=null;ResetState();busy=false;closing=false;
            Set(RaidersSessionPhase.Failed,message);
        }
        private async Task StopRunner()
        {
            if (voice) voice.Stop();
            var previous=runner;runner=null;
            if(!previous)return;
            previous.RemoveCallbacks(this);
            try { await previous.Shutdown(); } catch(Exception ex){Debug.LogWarning("Session shutdown: "+ex.Message);}
            if(previous)Destroy(previous.gameObject);
        }
        private void ResetState()
        {
            Match=null;IsLocal=false;RoomCode="";ControlledSlot=0;Countdown=0;
            for(int i=0;i<2;i++){occupied[i]=0;ready[i]=false;owners[i]=PlayerRef.None;nextCommand[i]=nextAvatar[i]=0;}
        }
        private void OnDestroy(){if(voice)voice.Stop();++generation;connecting?.Cancel();connecting?.Dispose();if(runner){runner.RemoveCallbacks(this);_ = runner.Shutdown();}}
        private void Send(Packet p){if(runner && runner.IsRunning)runner.SendReliableDataToServer(ReliableKey.FromInts(1,++packetSequence,0,0),Encoding.UTF8.GetBytes(JsonUtility.ToJson(p)));}
        private void SendTo(PlayerRef player,Packet p){if(runner && runner.IsRunning)runner.SendReliableDataToPlayer(player,ReliableKey.FromInts(1,++packetSequence,0,0),Encoding.UTF8.GetBytes(JsonUtility.ToJson(p)));}
        private void Broadcast()
        {
            if(!runner || !runner.IsRunning || !runner.IsServer || Match==null)return;
            for(int i=0;i<2;i++)if(occupied[i]!=0 && owners[i]!=runner.LocalPlayer)
                SendTo(owners[i],new Packet{kind=0,slot=i,phase=(int)Phase,status=Status,count0=occupied[0],count1=occupied[1],ready0=ready[0],ready1=ready[1],match=Match,countdown=Countdown});
        }
        public void SendAvatar(ulong userId,byte[] data,int length)
        {
            if(IsLocal || !runner || !runner.IsRunning || length<=0 || length>8192)return;
            var packet=new byte[length+9];packet[0]=0xA7;BitConverter.GetBytes(userId).CopyTo(packet,1);Buffer.BlockCopy(data,0,packet,9,length);
            var key=ReliableKey.FromInts(7,++packetSequence,0,0);
            if(runner.IsServer){for(int i=0;i<2;i++)if(occupied[i]!=0 && owners[i]!=runner.LocalPlayer)runner.SendReliableDataToPlayer(owners[i],key,packet);}
            else runner.SendReliableDataToServer(key,packet);
        }
        public void OnReliableDataReceived(NetworkRunner source,PlayerRef player,ReliableKey key,ReadOnlySpan<byte> bytes)
        {
            if(source!=runner || closing || bytes.Length==0 || bytes.Length>32768)return;
            int slot=runner.IsServer?Slot(player):1-ControlledSlot;
            if(slot<0)return;
            if(bytes[0]==0xA7)
            {
                if(bytes.Length<10 || bytes.Length>8201 || Time.unscaledTime<nextAvatar[slot])return;
                nextAvatar[slot]=Time.unscaledTime+.04f;
                AvatarPacket?.Invoke(slot,BitConverter.ToUInt64(bytes.Slice(1,8)),bytes.Slice(9).ToArray());return;
            }
            try
            {
                var p=JsonUtility.FromJson<Packet>(Encoding.UTF8.GetString(bytes));if(p==null)return;
                if(runner.IsServer)
                {
                    // Sender identity comes exclusively from Fusion. Never trust a packet's slot.
                    if(Time.unscaledTime<nextCommand[slot])return;nextCommand[slot]=Time.unscaledTime+.04f;
                    if(p.kind==1)SetReady(slot,p.value!=0);
                    else if(p.kind==2)Apply(slot,(RaidersAction)p.action,p.index,p.value);
                    else if(p.kind==3)RequestRematch(slot);
                }
                else if(p.kind==0)
                {
                    if(p.slot<0 || p.slot>1 || p.match==null)return;
                    bool changed=Phase!=(RaidersSessionPhase)p.phase || ready[0]!=p.ready0 || ready[1]!=p.ready1;
                    ControlledSlot=p.slot;Match=p.match;occupied[0]=p.count0;occupied[1]=p.count1;ready[0]=p.ready0;ready[1]=p.ready1;Countdown=p.countdown;
                    Phase=(RaidersSessionPhase)p.phase;Status=p.status;lastPacket=Time.unscaledTime;if(changed)Changed?.Invoke();
                }
                else if(p.kind==4){Status=p.status;Changed?.Invoke();}
            }
            catch(Exception ex){Debug.LogWarning("Discarded invalid session packet: "+ex.GetType().Name);}
        }
        public void OnPlayerJoined(NetworkRunner r,PlayerRef p){if(r==runner && r.IsServer){Assign(p);if(Match!=null)UpdateLobby();}}
        public void OnPlayerLeft(NetworkRunner r,PlayerRef p){if(r==runner && !closing)Fail("The other player left. The match has ended.");}
        public void OnShutdown(NetworkRunner r,ShutdownReason reason){if(r==runner && !closing)Fail("Session ended: "+reason);}
        public void OnDisconnectedFromServer(NetworkRunner r,NetDisconnectReason reason){if(r==runner && !closing)Fail("Disconnected: "+reason);}
        public void OnConnectFailed(NetworkRunner r,NetAddress address,NetConnectFailedReason reason){}
        public void OnConnectRequest(NetworkRunner r,NetworkRunnerCallbackArgs.ConnectRequest request,byte[] token){if(r==runner && PlayerCount<2 && (Phase==RaidersSessionPhase.Connecting || Phase==RaidersSessionPhase.Waiting))request.Accept();else request.Refuse();}
        public void OnConnectedToServer(NetworkRunner r){}
        public void OnInput(NetworkRunner r,NetworkInput input){}
        public void OnInputMissing(NetworkRunner r,PlayerRef p,NetworkInput input){}
        public void OnObjectEnterAOI(NetworkRunner r,NetworkObject o,PlayerRef p){}
        public void OnObjectExitAOI(NetworkRunner r,NetworkObject o,PlayerRef p){}
        public void OnSessionListUpdated(NetworkRunner r,List<SessionInfo> list){}
        public void OnCustomAuthenticationResponse(NetworkRunner r,Dictionary<string,object> data){}
        public void OnHostMigration(NetworkRunner r,HostMigrationToken token){if(r==runner)Fail("Host left. Please start a new session.");}
        public void OnReliableDataProgress(NetworkRunner r,PlayerRef p,ReliableKey key,float progress){}
        public void OnSceneLoadDone(NetworkRunner r){}
        public void OnSceneLoadStart(NetworkRunner r){}
    }
}
