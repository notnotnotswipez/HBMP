using System.Collections.Generic;
using HBMP.Nodes;

namespace HBMP.Messages.Handlers.Network
{
    public class ClientDistributionMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            ClientDistributionData clientDistributionData = (ClientDistributionData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte((byte)clientDistributionData.channel);
            packetByteBuf.WriteByte((byte)clientDistributionData.playerIds.Count);
            foreach (var playerId in clientDistributionData.playerIds)
            {
                packetByteBuf.WriteByte(SteamIntegration.GetByteId(playerId));
            }
            packetByteBuf.WriteBytes(clientDistributionData.data.getBytes());
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            // Shouldnt be here.
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            // Get rid of packet index
            packetByteBuf.ReadByte();
            
            byte channel = packetByteBuf.ReadByte();
            byte size = packetByteBuf.ReadByte();
            List<ulong> ulongs = new List<ulong>();
            for (byte i = 0; i < size; i++)
            {
                ulongs.Add(SteamIntegration.GetLongId(packetByteBuf.ReadByte()));
            }

            byte[] buffers = packetByteBuf.GetRemainingBytes();
            SteamPacketNode.BroadcastMessageToSetGroup((NetworkChannel)channel, buffers, ulongs, false);
        }
    }
    
    public class ClientDistributionData : MessageData
    {
        public NetworkChannel channel;
        public List<ulong> playerIds = new List<ulong>();
        public PacketByteBuf data;
    }
}