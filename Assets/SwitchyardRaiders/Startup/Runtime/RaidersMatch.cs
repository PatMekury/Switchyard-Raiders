using System;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    // Authoritative, simultaneous simulation shared by network and local sessions.
    // Distances are railway units, independent of the player's tabletop transform.
    [Serializable] public sealed class RaidersTrain
    {
        public float distance, speed, throttle;
        public int score, devices = 6;
    }
    [Serializable] public sealed class RaidersCargo
    {
        public float distance, speed;
        // -2 company train, -1 loose, 0/1 player, -3 delivered.
        public int owner = -2;
        public int order, leader = -1;
        public float chainOffset;
    }
    [Serializable] public sealed class RaidersDevice
    {
        public int owner = -1;
        public bool spent;
        public float aim;
    }
    public enum RaidersAction { Throttle, Brake, Reverse, Place, Aim, Fire }
    [Serializable] public sealed class RaidersMatch
    {
        public const float Length = 32, Spacing = .65f;
        public const int SiteCount = 8, CargoCount = 6, DeliveryTarget = 3;
        public RaidersTrain[] players = { new(), new() };
        public RaidersCargo[] cargo = new RaidersCargo[CargoCount];
        public RaidersDevice[] devices = new RaidersDevice[SiteCount];
        public float companyDistance = 8, elapsed;
        public int winner = -1, revision;
        public bool practice;
        public int playerCount=2;
        public int[] teamScores = new int[2];
        public float nextFreight;
        // Provisional integration cadence; freight never appears all at once.
        public const float FreightInterval=20;
        public int Target => playerCount==4 ? 6 : DeliveryTarget;
        public int Team(int slot) => playerCount==4 ? slot%2 : slot;
        public float HomeFor(int slot) => Home(Team(slot));

        public RaidersMatch() { Reset(false); }
        public void Reset(bool solo, int count=2)
        {
            playerCount=count==4?4:2; players=new RaidersTrain[playerCount]; teamScores=new int[2]; nextFreight=0;
            practice = solo; winner = -1; elapsed = 0; companyDistance = 8; revision++;
            for (int i = 0; i < playerCount; i++) players[i] = new RaidersTrain { distance = Wrap(HomeFor(i)+(i/2)*2.2f) };
            for (int i = 0; i < CargoCount; i++) cargo[i] = new RaidersCargo { owner=-4, distance = Wrap(8 - (i + 1) * Spacing), order = i + 1 };
            for (int i = 0; i < SiteCount; i++) devices[i] = new RaidersDevice();
        }
        public static float Wrap(float d) => Mathf.Repeat(d, Length);
        public static float Delta(float a, float b) => Mathf.Repeat(a - b + Length * .5f, Length) - Length * .5f;
        public static float Home(int slot) => slot * Length * .5f;
        public static float Site(int site) => 2 + site * 4;
        public static float Height(float d) => .18f * (1 - Mathf.Cos(d * Mathf.PI / 8));
        public static Vector3 Point(float d)
        {
            float a = d / Length * Mathf.PI * 2;
            return new Vector3(Mathf.Sin(a) * .65f, Height(d) * .25f, Mathf.Cos(a) * .42f);
        }
        public float Window(int site)
        {
            if (site < 0 || site >= SiteCount || devices[site].owner < 0 || devices[site].spent) return 0;
            float nearest = 10;
            for (int i = 0; i < cargo.Length; i++)
                if (cargo[i].owner >= -2 && cargo[i].owner != -1)
                    nearest = Mathf.Min(nearest, Mathf.Abs(Delta(cargo[i].distance + Spacing * .5f, Site(site))));
            return Mathf.Clamp01(1.3f - nearest / 1.2f);
        }
        public bool Apply(int slot, RaidersAction action, int index, float value, out string reason)
        {
            reason = "";
            if (slot < 0 || slot >= playerCount || (practice && slot != 0) || winner >= 0) { reason = "This player cannot act."; return false; }
            if (float.IsNaN(value) || float.IsInfinity(value)) { reason = "Invalid control value."; return false; }
            var train = players[slot];
            switch (action)
            {
                case RaidersAction.Throttle: train.throttle = Mathf.Clamp(value, -1, 1); break;
                case RaidersAction.Brake: train.throttle = 0; train.speed = 0; break;
                case RaidersAction.Reverse: train.throttle = train.throttle > 0 ? -.35f : .35f; break;
                case RaidersAction.Place:
                    if (index < 0 || index >= SiteCount || train.devices <= 0) { reason = "No decoupler available."; return false; }
                    if (devices[index].owner >= 0 && !devices[index].spent) { reason = "That emplacement is occupied."; return false; }
                    devices[index] = new RaidersDevice { owner = slot }; train.devices--; break;
                case RaidersAction.Aim:
                case RaidersAction.Fire:
                    if (index < 0 || index >= SiteCount || devices[index].owner != slot || devices[index].spent) { reason = "Select your unused decoupler."; return false; }
                    if (action == RaidersAction.Aim) { devices[index].aim = Mathf.Clamp(value, -1, 1); break; }
                    if (Window(index) < .99f) { reason = "Wait for the coupling's firing window."; return false; }
                    devices[index].spent = true;
                    int hit = -1; float best = .25f;
                    for (int i = 0; i < cargo.Length; i++)
                    {
                        var c = cargo[i];
                        if (c.owner == -1 || c.owner <= -3) continue;
                        float error = Mathf.Abs(Delta(c.distance + Spacing * .5f, Site(index) + devices[index].aim));
                        if (error < best) { best = error; hit = i; }
                    }
                    if (hit >= 0)
                    {
                        int previousOwner=cargo[hit].owner, firstOrder=cargo[hit].order;
                        // Cutting a coupling releases the complete chain behind it.
                        for(int i=0;i<cargo.Length;i++)if(cargo[i].owner==previousOwner && cargo[i].order>=firstOrder)
                        {cargo[i].chainOffset=(cargo[i].order-firstOrder)*Spacing;cargo[i].leader=hit;cargo[i].owner=-1;}
                    }
                    else reason = "Shot missed. Device spent.";
                    break;
                default: reason = "Unknown action."; return false;
            }
            revision++; return true;
        }
        public void Step(float dt)
        {
            if (winner >= 0 || dt <= 0 || dt > .1f) return;
            elapsed += dt;
            if(elapsed>=nextFreight)
            {
                int order=0;for(int i=0;i<cargo.Length;i++)if(cargo[i].owner==-2)order=Mathf.Max(order,cargo[i].order);
                for(int i=0;i<cargo.Length;i++)if(cargo[i].owner<=-3)
                {cargo[i]=new RaidersCargo{owner=-2,order=order+1,distance=Wrap(companyDistance-(order+1)*Spacing),speed=.65f};revision++;break;}
                nextFreight=elapsed+FreightInterval;
            }
            companyDistance = Wrap(companyDistance + .65f * dt);
            for (int p = 0; p < (practice ? 1 : playerCount); p++)
            {
                var t = players[p]; t.speed = Mathf.MoveTowards(t.speed, t.throttle * 1.5f, dt * 1.5f);
                t.distance = Wrap(t.distance + t.speed * dt);
            }
            for (int i = 0; i < cargo.Length; i++)
            {
                var c = cargo[i];
                if (c.owner <= -3) continue;
                if (c.owner == -2) { c.distance = Wrap(companyDistance - c.order * Spacing); c.speed = .65f; }
                else if (c.owner >= 0)
                {
                    var t = players[c.owner]; c.speed = t.speed;
                    c.distance = Wrap(t.distance - c.order * Spacing);
                }
                else
                {
                    if(c.leader!=i)continue;
                    float slope = .18f * Mathf.PI / 8 * Mathf.Sin(c.distance * Mathf.PI / 8);
                    c.speed = Mathf.Clamp(c.speed - slope * dt * 3, -1, 1);
                    c.speed = Mathf.MoveTowards(c.speed, 0, .035f * dt);
                    c.distance = Wrap(c.distance + c.speed * dt);
                    for (int p = 0; p < (practice ? 1 : playerCount); p++)
                    {
                        int count = 0; for (int j = 0; j < cargo.Length; j++) if (cargo[j].owner == p) count++;
                        float rear = Wrap(players[p].distance - (count + 1) * Spacing);
                        if (Mathf.Abs(Delta(c.distance, rear)) < .12f && Mathf.Abs(c.speed - players[p].speed) < .45f)
                        {
                            for(int j=0;j<cargo.Length;j++)if(cargo[j].owner==-1 && cargo[j].leader==i)
                            {cargo[j].owner=p;cargo[j].order=count+1+Mathf.RoundToInt(cargo[j].chainOffset/Spacing);cargo[j].leader=-1;}
                            revision++; break;
                        }
                    }
                }
            }
            for(int i=0;i<cargo.Length;i++)
            {
                var c=cargo[i];
                if(c.owner==-1 && c.leader>=0 && c.leader!=i){c.distance=Wrap(cargo[c.leader].distance-c.chainOffset);c.speed=cargo[c.leader].speed;}
            }
            for (int p = 0; p < (practice ? 1 : playerCount); p++)
            {
                bool full = true; int count = 0;
                for (int i = 0; i < cargo.Length; i++) if (cargo[i].owner == p)
                { count++; if (Mathf.Abs(Delta(cargo[i].distance, HomeFor(p))) > 2.4f) full = false; }
                if (count == 0 || !full || Mathf.Abs(Delta(players[p].distance, HomeFor(p))) > 2.4f) continue;
                for (int i = 0; i < cargo.Length; i++) if (cargo[i].owner == p) cargo[i].owner = -3;
                players[p].score += count; teamScores[Team(p)] += count; revision++;
                if (teamScores[Team(p)] >= Target) { winner = Team(p); foreach(var train in players)train.throttle=0; }
            }
        }
    }
}
