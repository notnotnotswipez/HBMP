using System;
using System.Collections.Generic;
using HBMP.Messages;
using Steamworks;

namespace HBMP.Nodes
{
    public class SteamPacketNode
    {
        private static List<QueuedPacket> queuedBufs = new List<QueuedPacket>();

        public static void BroadcastMessage(NetworkChannel channel, PacketByteBuf packetByteBuf)
        {
            foreach (var connectedUser in SteamManager.connectedIds)
            {
                queuedBufs.Add(new QueuedPacket()
                {
                    _packetByteBuf = packetByteBuf,
                    _steamId = connectedUser,
                    channel = channel
                });
            }
        }

        public static void SendMessage(SteamId steamId, NetworkChannel channel, PacketByteBuf packetByteBuf)
        {
            queuedBufs.Add(new QueuedPacket()
            {
                _packetByteBuf = packetByteBuf,
                _steamId = steamId,
                channel = channel
            });
        }

        public static void Flush()
        {
            foreach (var packets in queuedBufs)
            {
                P2PSend sendType = SteamManager.networkChannels[packets.channel];
                bool success = SteamNetworking.SendP2PPacket(packets._steamId, packets._packetByteBuf.getBytes(), -1, (int)packets.channel, sendType);
            }
            queuedBufs.Clear();
        }

        public static void Callbacks()
        {
            foreach (NetworkChannel channel in SteamManager.networkChannels.Keys)
            {
                while (SteamNetworking.IsP2PPacketAvailable((int)channel))
                {
                    var packet = SteamNetworking.ReadP2PPacket((int)channel);
                    if (packet.HasValue)
                    {
                        byte[] data = packet.Value.Data;
                        if (data.Length <= 0) // Idk
                            throw new Exception("Data was invalid!");
                 
                        byte messageType = data[0];
                        byte[] realData = new byte[data.Length - sizeof(byte)];

                        for (int b = sizeof(byte); b < data.Length; b++)
                            realData[b - sizeof(byte)] = data[b];

                        PacketByteBuf packetByteBuf = new PacketByteBuf(realData);
             
                        MessageHandler.ReadMessage((NetworkMessageType)messageType, packetByteBuf, 0);
                    }
                }
            }
        }

        class QueuedPacket
        {
            public PacketByteBuf _packetByteBuf;
            public SteamId _steamId;
            public NetworkChannel channel;
        }
    }
}