using System.Collections.Generic;
using HBMP.Nodes;
using HBMP.Object;
using Steamworks;

namespace HBMP.Messages.Handlers
{
    public class ExplodeMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            ExplodeMessageData explodeMessageData = (ExplodeMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamManager.GetByteId(explodeMessageData.userId));
            packetByteBuf.WriteUShort(explodeMessageData.objectId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SteamId userId = SteamManager.GetLongId(packetByteBuf.ReadByte());
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
        public SteamId userId;
        public ushort objectId;
    }
}