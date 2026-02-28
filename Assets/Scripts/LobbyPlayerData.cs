using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// Per-player data synced via NetworkList to every client in the lobby.
/// </summary>
public struct LobbyPlayerData : INetworkSerializable, System.IEquatable<LobbyPlayerData>
{
    public ulong              clientId;
    public FixedString32Bytes nickname;
    public int                colorIndex;
    public bool               isHost;
    public ulong              networkObjectId;   // Which NetworkObject this player controls

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref nickname);
        serializer.SerializeValue(ref colorIndex);
        serializer.SerializeValue(ref isHost);
        serializer.SerializeValue(ref networkObjectId);
    }

    public bool Equals(LobbyPlayerData other) => clientId == other.clientId;
    public override int GetHashCode()         => clientId.GetHashCode();
}
