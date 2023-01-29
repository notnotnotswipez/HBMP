using System.Collections.Generic;
using HBMP.Nodes;
using Steamworks;

namespace HBMP.Messages.Handlers
{
    public class RequestIdsMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            RequestIdsMessageData requestIdsMessageData = (RequestIdsMessageData)messageData;

            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(requestIdsMessageData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            SteamId userId = SteamIntegration.GetByteId(packetByteBuf.ReadByte());
            if (SteamIntegration.isHost)
            {
                foreach (KeyValuePair<byte, ulong> valuePair in SteamIntegration.byteIds) {
                    ShortIdMessageData addMessageData = new ShortIdMessageData()
                    {
                        userId = valuePair.Value,
                        byteId = valuePair.Key,
                    };
                    PacketByteBuf shortBuf = PacketHandler.CompressMessage(PacketType.ShortIdUpdateMessage, addMessageData);
                    SteamPacketNode.SendMessage(userId, NetworkChannel.Reliable, shortBuf.getBytes());
                }
            }
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new System.NotImplementedException();
        }
    }

    public class RequestIdsMessageData : MessageData
    {
        public SteamId userId;
    }
}