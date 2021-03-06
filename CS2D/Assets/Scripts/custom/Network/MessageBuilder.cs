using System.Net;
using custom.Server;
using JetBrains.Annotations;
using UnityEngine;

namespace custom.Network
{
    public class MessageBuilder
    {
        private readonly int _gameMemberId;
        private readonly string _destinationIP;
        
        private readonly Channel _channel;
        private readonly int _channelPortD;

        public MessageBuilder(int gameMemberId, int sourcePortBase, int destinationPortBase, string destinationIp)
        {
            Debug.Log("Listening on " + sourcePortBase);
            _channel = new Channel(sourcePortBase);
            this._gameMemberId = gameMemberId;
            this._channelPortD = destinationPortBase;
            this._destinationIP = destinationIp;
        }

        
        // Client Side
        
        public JoinGameMessage GenerateJoinGameMessage(int id)
        {
            return new JoinGameMessage(id, _channel, _destinationIP, _channelPortD);
        }
        
        public GoodbyeMessage GenerateGoodbye(int id)
        {
            return new GoodbyeMessage(id, _channel, _destinationIP, _channelPortD);
        }

        public ClientUpdateMessage GenerateClientUpdateMessage()
        {
            return new ClientUpdateMessage(_gameMemberId, _channel, _destinationIP, _channelPortD);
        }
        
        public HitEnemyMessage GenerateHitEnemyMessage(int fromId, int toId)
        {
            return new HitEnemyMessage(fromId, toId, _channel, _destinationIP, _channelPortD);
        }
        
        // Server Side
        public PlayerJoinedMessage GeneratePlayerJoinedMessage(PlayerInfo player)
        {
            return new PlayerJoinedMessage(-1, _channel, player.EndPoint.Address.ToString(), Constants.clients_base_port + 10*player.Id);
        }
        
        public GoodbyeMessage GenerateGoodbye(PlayerInfo player)
        {
            return new GoodbyeMessage(-1, _channel, player.EndPoint.Address.ToString(), Constants.clients_base_port + 10*player.Id);
        }

        public ServerUpdateMessage GenerateServerUpdateMessage(PlayerInfo player)
        {
            return new ServerUpdateMessage(-1, _channel, player.EndPoint.Address.ToString(), Constants.clients_base_port + 10*player.Id);
        }
        
        public InitStatusMessage GenerateInitStatusMessage(PlayerInfo player)
        {
            return new InitStatusMessage(-1, _channel, player.EndPoint.Address.ToString(), Constants.clients_base_port + 10*player.Id);
        }

        public ServerACKMessage GenerateServerAckMessage(PlayerInfo player)
        {
            return new ServerACKMessage(-1, _channel, player.EndPoint.Address.ToString(), Constants.clients_base_port + 10*player.Id);
        }
        
        // Read Messages from channels
        
        [CanBeNull]
        public Message GETChannelMessage()
        {
            Packet packet = _channel.GetPacket();
            return packet != null ? Message.getMessage(packet) : null;
        }

        private Message GETMessage(Packet incomePacket)
        {
            return Message.getMessage(incomePacket);
        }

        public void Disconnect()
        {
            _channel.Disconnect();
        }
    }
}