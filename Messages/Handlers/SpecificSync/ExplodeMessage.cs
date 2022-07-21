using System.Collections.Generic;
using HBMP.Nodes;
using HBMP.Object;

namespace HBMP.Messages.Handlers
{
    public class ExplodeMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            ExplodeMessageData explodeMessageData = (ExplodeMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(explodeMessageData.userId));
            packetByteBuf.WriteUShort(explodeMessageData.objectId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            long userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            ushort objectId = packetByteBuf.ReadUShort();
            SyncedObject syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject)
            {
                Explodeable explodeable = syncedObject.gameObject.GetComponentInChildren<Explodeable>();
                if (!explodeable)
                {
                    explodeable = syncedObject.gameObject.GetComponentInParent<Explodeable>();
                }
                explodeable.Explode();
            }
        }
    }

    public class ExplodeMessageData : MessageData
    {
        public long userId;
        public ushort objectId;
    }
}