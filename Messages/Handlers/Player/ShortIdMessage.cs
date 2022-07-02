using System;

namespace HBMP.Messages.Handlers
{
    public class ShortIdMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            ShortIdMessageData shortIdMessageData = (ShortIdMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteLong(shortIdMessageData.userId);
            packetByteBuf.WriteByte(shortIdMessageData.byteId);
            packetByteBuf.create();
            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (packetByteBuf.getBytes().Length <= 0)
                throw new IndexOutOfRangeException();

            int index = 0;
            long userId = BitConverter.ToInt64(packetByteBuf.getBytes(), index);
            index += sizeof(long);

            byte byteId = packetByteBuf.getBytes()[index++];

            if (userId == DiscordIntegration.currentUser.Id)
                DiscordIntegration.localByteId = byteId;
            DiscordIntegration.RegisterUser(userId, byteId);
        }
    }

    public class ShortIdMessageData : MessageData
    {
        public long userId;
        public byte byteId;
    }
}