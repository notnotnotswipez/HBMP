using HBMP.Object;
using MelonLoader;

namespace HBMP.Messages.Handlers
{
    public class JoinCatchupMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            JoinCatchupData joinCatchupData = (JoinCatchupData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(joinCatchupData.lastId);
            packetByteBuf.WriteUShort(joinCatchupData.lastGroupId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            MelonLogger.Msg("Caught up with server host!");
            ushort lastId = packetByteBuf.ReadUShort();
            ushort lastGroupId = packetByteBuf.ReadUShort();
            SyncedObject.lastId = lastId;
            SyncedObject.lastGroupId = lastGroupId;
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new System.NotImplementedException();
        }
    }

    public class JoinCatchupData : MessageData
    {
        public ushort lastId;
        public ushort lastGroupId;
    }
}