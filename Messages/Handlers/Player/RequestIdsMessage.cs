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
            packetByteBuf.WriteByte(SteamManager.GetByteId(requestIdsMessageData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SteamId userId = SteamManager.GetByteId(packetByteBuf.ReadByte());
            if (SteamManager.Instance.isHost)
            {
                foreach (KeyValuePair<byte, ulong> valuePair in SteamManager.byteIds) {
                    ShortIdMessageData addMessageData = new ShortIdMessageData()
                    {
                        userId = valuePair.Value,
                        byteId = valuePair.Key,
                    };
                    PacketByteBuf shortBuf = MessageHandler.CompressMessage(NetworkMessageType.ShortIdUpdateMessage, addMessageData);
                    SteamPacketNode.SendMessage(userId, NetworkChannel.Reliable, shortBuf);
                }
            }
        }
    }

    public class RequestIdsMessageData : MessageData
    {
        public SteamId userId;
    }
}