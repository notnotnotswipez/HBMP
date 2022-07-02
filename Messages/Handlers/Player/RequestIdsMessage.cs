using System.Collections.Generic;
using HBMP.Nodes;

namespace HBMP.Messages.Handlers
{
    public class RequestIdsMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            RequestIdsMessageData requestIdsMessageData = (RequestIdsMessageData)messageData;

            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(requestIdsMessageData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            long userId = DiscordIntegration.GetByteId(packetByteBuf.ReadByte());
            if (Server.instance != null)
            {
                foreach (KeyValuePair<byte, long> valuePair in DiscordIntegration.byteIds) {
                    ShortIdMessageData addMessageData = new ShortIdMessageData()
                    {
                        userId = valuePair.Value,
                        byteId = valuePair.Key,
                    };
                    PacketByteBuf shortBuf = MessageHandler.CompressMessage(NetworkMessageType.ShortIdUpdateMessage, addMessageData);
                    Server.instance.SendMessage(userId, (byte)NetworkChannel.Reliable, shortBuf.getBytes());
                }
            }
        }
    }

    public class RequestIdsMessageData : MessageData
    {
        public long userId;
    }
}