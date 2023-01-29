using System.Collections.Generic;
using HBMP.Messages;
using HBMP.Messages.Handlers.Network;
using Steamworks;
using Steamworks.Data;

namespace HBMP.Nodes
{
    public class SteamPacketNode
    {
        public static Dictionary<ulong, Connection> cachedConnections = new Dictionary<ulong, Connection>();

        public static void BroadcastMessage(NetworkChannel channel, byte[] packetByteBuf, bool ignoreSelf = true)
        {
            if (!SteamIntegration.hasLobby) return;
            
            SendType p2PSend = SteamIntegration.networkChannels[channel];
            if (SteamIntegration.isHost)
            {
                foreach (var connection in cachedConnections)
                {
                    if (ignoreSelf)
                    {
                        if (connection.Key == SteamIntegration.currentId) continue;
                    }

                    connection.Value.SendMessage(packetByteBuf, p2PSend);
                }
            }
            else
            {
                List<ulong> players = new List<ulong>();
                foreach (var playerId in SteamIntegration.connectedIds)
                {
                    if (playerId != SteamIntegration.currentId)
                    {
                        players.Add(playerId);
                    }
                }

                ClientDistributionData clientDistributionData = new ClientDistributionData()
                {
                    playerIds = players,
                    channel = channel,
                    data = new PacketByteBuf(packetByteBuf)
                };

                PacketByteBuf finalBuff =
                    PacketHandler.CompressMessage(PacketType.ClientDistributionMessage, clientDistributionData);

                SteamIntegration.clientSocket?.Connection.SendMessage(finalBuff.getBytes());
            }
        }

        public static void BroadcastMessageExcept(NetworkChannel channel, byte[] packetByteBuf, ulong excluded,
            bool ignoreSelf = true)
        {
            if (!SteamIntegration.hasLobby) return;
            
            SendType p2PSend = SteamIntegration.networkChannels[channel];
            if (SteamIntegration.isHost)
            {
                foreach (var connectedUser in cachedConnections)
                {
                    if (connectedUser.Key == excluded) return;
                    if (ignoreSelf)
                    {
                        if (connectedUser.Key == SteamIntegration.currentId) return;
                    }

                    
                    
                    Connection connection = cachedConnections[connectedUser.Key];
                    connection.SendMessage(packetByteBuf, p2PSend);
                }
            }
            else
            {
                
                List<ulong> players = new List<ulong>();
                foreach (var playerId in SteamIntegration.connectedIds)
                {
                    if (playerId == SteamIntegration.currentId) return;
                    if (playerId == excluded) return;
                    players.Add(playerId);
                }

                ClientDistributionData clientDistributionData = new ClientDistributionData()
                {
                    playerIds = players,
                    channel = channel,
                    data = new PacketByteBuf(packetByteBuf)
                };

                PacketByteBuf finalBuff =
                    PacketHandler.CompressMessage(PacketType.ClientDistributionMessage, clientDistributionData);

                SteamIntegration.clientSocket?.Connection.SendMessage(finalBuff.getBytes());
            }
        }

        public static void BroadcastMessageToSetGroup(NetworkChannel channel, byte[] packetByteBuf, List<ulong> restOfIds, bool ignoreSelf = true)
        {
            if (!SteamIntegration.hasLobby) return;
            
            SendType p2PSend = SteamIntegration.networkChannels[channel];
            if (SteamIntegration.isHost)
            {
                foreach (var connectedUser in restOfIds)
                {
                    if (ignoreSelf)
                    {
                        if (connectedUser == SteamIntegration.currentId) return;
                    }
                    
                    if (cachedConnections.ContainsKey(connectedUser))
                    {
                        Connection connection = cachedConnections[connectedUser];
                        connection.SendMessage(packetByteBuf, p2PSend);
                    }
                }
            }
            else
            {
                ClientDistributionData clientDistributionData = new ClientDistributionData()
                {
                    playerIds = restOfIds,
                    channel = channel,
                    data = new PacketByteBuf(packetByteBuf)
                };

                PacketByteBuf finalBuff =
                    PacketHandler.CompressMessage(PacketType.ClientDistributionMessage, clientDistributionData);

                SteamIntegration.clientSocket?.Connection.SendMessage(finalBuff.getBytes());
            }
        }

        public static void SendMessageDirectToServer(NetworkChannel channel, byte[] packetByteBuf)
        {
            if (!SteamIntegration.hasLobby) return;

            SendType p2PSend = SteamIntegration.networkChannels[channel];
            SteamIntegration.clientSocket?.Connection.SendMessage(packetByteBuf, p2PSend);
        }

        public static void SendMessage(SteamId steamId, NetworkChannel channel, byte[] packetByteBuf, bool ignoreSelf = true)
        {
            if (!SteamIntegration.hasLobby) return;
            
            if (steamId == SteamIntegration.currentId) return;

            SendType p2PSend = SteamIntegration.networkChannels[channel];
            
            if (SteamIntegration.isHost)
            {
                if (ignoreSelf)
                {
                    if (steamId == SteamIntegration.currentId) return;
                }

                if (cachedConnections.ContainsKey(steamId))
                {
                    Connection connection = cachedConnections[steamId];
                    connection.SendMessage(packetByteBuf, p2PSend);
                }
            }
            else
            {
                List<ulong> players = new List<ulong>();
                players.Add(steamId);
                
                ClientDistributionData clientDistributionData = new ClientDistributionData()
                {
                    playerIds = players,
                    channel = channel,
                    data = new PacketByteBuf(packetByteBuf)
                };

                PacketByteBuf finalBuff =
                    PacketHandler.CompressMessage(PacketType.ClientDistributionMessage, clientDistributionData);

                SteamIntegration.clientSocket?.Connection.SendMessage(finalBuff.getBytes());
            }
        }
    }
}