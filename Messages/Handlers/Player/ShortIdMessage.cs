using System;
using HBMP.Nodes;
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

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            if (packetByteBuf.getBytes().Length <= 0)
                throw new IndexOutOfRangeException();

            ulong userId = packetByteBuf.ReadULong();
            byte byteId = packetByteBuf.ReadByte();

            if (userId == SteamIntegration.currentId)
                SteamIntegration.localByteId = byteId;
            
            SteamIntegration.RegisterUser(byteId, userId);
            
            if (userId != SteamIntegration.currentId)
            {
                PlayerConfirmData playerConfirmData = new PlayerConfirmData()
                {
                    userId = SteamIntegration.currentId.Value
                };
                PacketByteBuf confirmBuff =
                    PacketHandler.CompressMessage(PacketType.PlayerConfirmationMessage, playerConfirmData);

                SteamPacketNode.SendMessage(userId, NetworkChannel.Reliable, confirmBuff.getBytes());
            }
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new NotImplementedException();
        }
    }

    public class ShortIdMessageData : MessageData
    {
        public SteamId userId;
        public byte byteId;
    }
}