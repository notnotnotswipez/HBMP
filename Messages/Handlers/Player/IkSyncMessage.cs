using System.Collections.Generic;
using HBMP.DataType;
using HBMP.Representations;
using Steamworks;

namespace HBMP.Messages.Handlers
{
    public class IkSyncMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            IkSyncMessageData ikSyncMessageData = (IkSyncMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(ikSyncMessageData.userId));
            packetByteBuf.WriteByte(ikSyncMessageData.boneIndex);
            packetByteBuf.WriteSimpleTransform(ikSyncMessageData.simplifiedTransform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
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

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new System.NotImplementedException();
        }
    }

    public class IkSyncMessageData : MessageData
    {
        public SteamId userId;
        public byte boneIndex;
        public SimplifiedTransform simplifiedTransform;
    }
}