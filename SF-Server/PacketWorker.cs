using Lidgren.Network;

namespace SFServer;

public class PacketWorker
{
    private readonly Server _server;
    private readonly SfPacketType[] _ignoredPacketTypes =
    {
		SfPacketType.PlayerUpdate, // Very frequent packets, usually not needed for debugging
	};

    public PacketWorker(Server server) => _server = server;

    public void ParseGamePacket(NetIncomingMessage msg)
    {
        //uint lastTimeStamp = MultiplayerManager.LastTimeStamp;
        var timeSent = msg.ReadUInt32(); // <--- Time packet was sent
        var msgType = (SfPacketType)msg.ReadByte();
        var msgChannel = msg.SequenceChannel;

        if (!_ignoredPacketTypes.Contains(msgType) && _server.Config.EnableLogging && _server.Config.EnableDebugPacketLogging)
        {
            var packetDebugInfo = $"""
	                               
	                               Raw Data length: {msg.Data.Length}
	                               Packet sent at time: {timeSent}
	                               Parsed StickFight packet of type: {msgType}
	                               Got channel of: {msgChannel}
	                               """;

            File.AppendAllText(_server.ServerLogPath, packetDebugInfo);
        }

        // if (msgChannel is > 1 and < 10) // Is update or event packet
        // {
        //  var senderID = msgChannel % 2 == 0 ? (msgChannel - 2) / 2 : (msgChannel - 3) / 2;
        //  var senderAddress = msg.SenderConnection.RemoteEndPoint.Address;
        //  
        //  if ( _server.GetClient(senderAddress).PlayerIndex != senderID)
        //  {
        //   Console.WriteLine("Sender channel is not from the same client, ignoring...");
        //   return;
        //  }
        // }

        // Check if packet is obsolete based on timestamp
        if (_server.IsPacketObsolete(timeSent))
        {
            if (_server.Config.EnableLogging)
            {
                Console.WriteLine($"Discarding obsolete packet of type: {msgType}");
            }
            return; // Don't process obsolete packets
        }
        
        ExecutePacketData(msg, msgType, msg.SenderConnection);
    }

    public void ExecutePacketData(NetIncomingMessage msg, SfPacketType messageType, NetConnection user)
    {
        switch (messageType)
        {
            case SfPacketType.Ping:
                _server.OnPingReceived(user, msg);
                return;
            case SfPacketType.PingResponse:
                _server.OnPingResponseReceived(user, msg);
                return;
            case SfPacketType.ClientJoined:
                _server.OnClientJoined(user, msg);
                return;
            case SfPacketType.ClientRequestingAccepting:
                _server.SendPacketToUser(user, Array.Empty<byte>(), SfPacketType.ClientAccepted);
                return;
            case SfPacketType.ClientAccepted:
                _server.OnClientAcceptedByServer(user, msg);
                return;
            case SfPacketType.ClientInit:
                //_server.OnInitFromServer(data);
                return;
            case SfPacketType.ClientRequestingIndex:
                _server.OnPlayerRequestingIndex(user);
                return;
            case SfPacketType.ClientRequestingToSpawn:
                _server.OnPlayerRequestingToSpawn(user, msg);
                return;
            case SfPacketType.PlayerUpdate:
                _server.OnPlayerUpdate(user, msg);
                return;
            case SfPacketType.PlayerTalked:
                _server.OnPlayerTalked(user, msg);
                return;
            case SfPacketType.PlayerForceAdded:
                _server.OnPlayerForceAdded(user, msg);
                return;
            case SfPacketType.PlayerTookDamage:
                _server.OnPlayerTookDamage(user, msg);
                return;
            case SfPacketType.ClientSpawned:
                _server.OnPlayerSpawned(user, msg);
                return;
            case SfPacketType.ClientReadyUp:
                _server.OnClientReadyUp(user, msg);
                return;
            case SfPacketType.MapChange:
                _server.OnMapChanged(user, msg);
                return;
            case SfPacketType.WeaponSpawned:
                //this.mNetworkHandler.OnWeaponSpawned(data);
                return;
            case SfPacketType.ClientRequestWeaponDrop:
                //this.mNetworkHandler.OnPlayerRequestingWeaponDrop(data);
                return;
            case SfPacketType.WeaponDropped:
                //this.mNetworkHandler.OnWeaponDropped(data);
                return;
            case SfPacketType.WeaponWasPickedUp:
                //this.mNetworkHandler.OnWeaponWasPickedUp(data);
                return;
            case SfPacketType.ClientRequestingWeaponPickUp:
                //this.mNetworkHandler.OnPlayerRequestingWeaponPickUp(data);
                return;
            case SfPacketType.ObjectSpawned:
                //this.mNetworkHandler.OnObjectSpawned(data);
                return;
            case SfPacketType.GroundWeaponsInit:
                //this.mNetworkHandler.OnGroundWeaponsInit(data);
                return;
            case SfPacketType.MapInfo:
                //this.mNetworkHandler.OnMapInfoRecieved(data);
                return;
            case SfPacketType.MapInfoSync:
                //this.mNetworkHandler.OnMapDataRecieved(data);
                return;
            case SfPacketType.WorkshopMapsLoaded:
                //this.mNetworkHandler.OnNewWorkshopMapsRecieved(data);
                return;
            case SfPacketType.StartMatch:
                //this.mNetworkHandler.OnMatchStart(data);
                return;
            case SfPacketType.OptionsChanged:
                //OptionsHolder.NetworkOptionsChanged(data);
                return;
            case SfPacketType.KickPlayer:
                //this.mNetworkHandler.OnKicked(data);
                return;
            default:
                Console.WriteLine("Message type: " + messageType + " Is not setup!!!");
                return;
        }
    }
}

public enum SfPacketType
{
    Ping,
    PingResponse,
    ClientJoined,
    ClientRequestingAccepting,
    ClientAccepted,
    ClientInit,
    ClientRequestingIndex,
    ClientRequestingToSpawn,
    ClientSpawned,
    ClientReadyUp,
    PlayerUpdate,
    PlayerTookDamage,
    PlayerTalked,
    PlayerForceAdded,
    PlayerForceAddedAndBlock,
    PlayerLavaForceAdded,
    PlayerFallOut,
    PlayerWonWithRicochet,
    MapChange,
    WeaponSpawned,
    WeaponThrown,
    RequestingWeaponThrow,
    ClientRequestWeaponDrop,
    WeaponDropped,
    WeaponWasPickedUp,
    ClientRequestingWeaponPickUp,
    ObjectUpdate,
    ObjectSpawned,
    ObjectSimpleDestruction,
    ObjectInvokeDestructionEvent,
    ObjectDestructionCollision,
    GroundWeaponsInit,
    MapInfo,
    MapInfoSync,
    WorkshopMapsLoaded,
    StartMatch,
    ObjectHello,
    OptionsChanged,
    KickPlayer
}
