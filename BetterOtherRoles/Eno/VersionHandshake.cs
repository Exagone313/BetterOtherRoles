using System;
using System.Collections.Generic;
using System.Text.Json;
using BetterOtherRoles.Modules;
using BetterOtherRoles.Patches;
using BetterOtherRoles.Players;
using Hazel;
using UnityEngine;

namespace BetterOtherRoles.Eno;

public class VersionHandshake
{
    public const float HandshakeGracePeriodSeconds = 5f;

    public const float MinRebroadcastIntervalSeconds = 2f;

    public static VersionHandshake Instance
    {
        get
        {
            if (_instance == null)
            {
                GenerateLocalVersion();
            }

            return _instance;
        }

        private set => _instance = value;
    }
    private static VersionHandshake _instance { get; set; }
    private static readonly Dictionary<int, VersionHandshake> AllVersions = new();

    private static readonly Dictionary<int, float> FirstObservedAt = new();

    private static float _lastShareAt = float.NegativeInfinity;
    
    public Version Version { get; private set; }
    public int ClientId { get; private set; }
    public Guid Guid { get; private set; }
    public Dictionary<string, string> Flags { get; private set; }

    public static VersionHandshake Find(int clientId)
    {
        if (Instance.ClientId == clientId) return Instance;
        return AllVersions.TryGetValue(clientId, out var value) ? value : null;
    }

    public static bool Has(int clientId)
    {
        return Instance.ClientId == clientId || AllVersions.ContainsKey(clientId);
    }

    public static bool IsWithinGracePeriod(int clientId)
    {
        if (Has(clientId)) return false;
        if (!FirstObservedAt.TryGetValue(clientId, out var firstSeen))
        {
            // We've never observed this client before. Start the clock now;
            // the panel will treat them as in-grace this frame.
            FirstObservedAt[clientId] = Time.time;
            return true;
        }
        return Time.time - firstSeen < HandshakeGracePeriodSeconds;
    }

    public static void Clear()
    {
        Instance = null;
        AllVersions.Clear();
        FirstObservedAt.Clear();
        _lastShareAt = float.NegativeInfinity;
    }

    public static void HandleRpcHandshake(MessageReader reader)
    {
        try
        {
            var major = reader.ReadByte();
            var minor = reader.ReadByte();
            var build = reader.ReadByte();
            var timer = reader.ReadSingle();
            var clientId = reader.ReadPackedInt32();
            var guid = Guid.Parse(reader.ReadString());
            var flags = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadString());
            if (!AmongUsClient.Instance.AmHost && timer > 0f)
            {
                GameStartManagerPatch.timer = timer;
            }

            var handshake = new VersionHandshake
            {
                Version = new Version(major, minor, build),
                ClientId = clientId,
                Guid = guid,
                Flags = flags,
            };

            AllVersions[handshake.ClientId] = handshake;

            // Ack-style retry: if we just learned about a peer who has the mod,
            // it's likely they just joined or just loaded into the lobby. If
            // our last outbound broadcast was a while ago they may not have
            // received it, so re-broadcast (rate-limited).
            ShareIfStale();
        }
        catch (Exception e)
        {
            BetterOtherRolesPlugin.Logger.LogWarning($"Legacy version handshake cannot be parsed, error: {e}");
        }
    }

    public static void GenerateLocalVersion()
    {
        Instance = new VersionHandshake
        {
            Version = BetterOtherRolesPlugin.Version,
            ClientId = AmongUsClient.Instance.ClientId,
            Guid = DevConfig.CurrentGuid,
            Flags = DevConfig.Flags,
        };
    }

    public void Share()
    {
        var writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.VersionHandshake, Hazel.SendOption.Reliable, -1);
        writer.Write((byte)Version.Major);
        writer.Write((byte)Version.Minor);
        writer.Write((byte)Version.Build);
        writer.Write(AmongUsClient.Instance.AmHost ? GameStartManagerPatch.timer : -1f);
        writer.WritePacked(AmongUsClient.Instance.ClientId);
        writer.Write(Guid.ToString());
        writer.Write(JsonSerializer.Serialize(Flags));
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        _lastShareAt = Time.time;
    }

    public static void ShareIfStale()
    {
        if (CachedPlayer.LocalPlayer == null || !CachedPlayer.LocalPlayer.PlayerControl) return;
        if (Time.time - _lastShareAt < MinRebroadcastIntervalSeconds) return;
        Instance.Share();
    }

    public bool GuidMatch()
    {
        return Flags.ContainsKey("NO_GUID_CHECK") || Instance.Flags.ContainsKey("NO_GUID_CHECK") || Guid.Equals(Instance.Guid);
    }
}
