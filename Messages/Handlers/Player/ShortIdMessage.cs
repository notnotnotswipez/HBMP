using System;
using Steamworks;

namespace HBMP.Messages.Handlers
{
    public class ShortIdMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            ShortIdMessageData shortIdMessageData = (ShortIdMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteULong(shortIdMessageData.userId.Value);
            packetByteBuf.WriteByte(shortIdMessageData.byteId);
            packetByteBuf.create();
            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (packetByteBuf.getBytes().Length <= 0)
                throw new IndexOutOfRangeException();

            ulong userId = packetByteBuf.ReadULong();
            byte byteId = packetByteBuf.ReadByte();

            if (userId == SteamManager.currentId)
                SteamManager.localByteId = byteId;
            SteamManager.RegisterUser(byteId, userId);
        }
    }

    public class ShortIdMessageData : MessageData
    {
        public SteamId userId;
        public byte byteId;
    }
}