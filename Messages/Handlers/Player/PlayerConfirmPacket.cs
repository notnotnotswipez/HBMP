using MelonLoader;

namespace HBMP.Messages.Handlers
{
    public class PlayerConfirmPacket : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            PlayerConfirmData playerConfirmData = (PlayerConfirmData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(playerConfirmData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            ulong userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            if (MainMod.idsReadyForPlayerInfo.Contains(userId))
            {
                return;
            }

            MelonLogger.Msg("Player is ready for our data!");
            MainMod.idsReadyForPlayerInfo.Add(userId);
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new System.NotImplementedException();
        }
    }

    public class PlayerConfirmData : MessageData
    {
        public ulong userId;
    }
}