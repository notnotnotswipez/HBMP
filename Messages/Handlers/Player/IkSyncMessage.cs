using System.Collections.Generic;
using HBMP.DataType;
using HBMP.Representations;

namespace HBMP.Messages.Handlers
{
    public class IkSyncMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            IkSyncMessageData ikSyncMessageData = (IkSyncMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(ikSyncMessageData.userId));
            packetByteBuf.WriteByte(ikSyncMessageData.boneIndex);
            packetByteBuf.WriteSimpleTransform(ikSyncMessageData.simplifiedTransform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            long userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            byte path = packetByteBuf.ReadByte();
            List<byte> transformBytes = new List<byte>();
            for (int i = packetByteBuf.byteIndex; i < packetByteBuf.getBytes().Length; i++) {
                transformBytes.Add(packetByteBuf.getBytes()[i]);
            }
            SimplifiedTransform simpleTransform = SimplifiedTransform.FromBytes(transformBytes.ToArray());
            
            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                PlayerRepresentation playerRepresentation = PlayerRepresentation.representations[userId];
                playerRepresentation.updateIkTransform(path, simpleTransform);
            }
        }
    }

    public class IkSyncMessageData : MessageData
    {
        public long userId;
        public byte boneIndex;
        public SimplifiedTransform simplifiedTransform;
    }
}