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
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(explodeMessageData.userId));
            packetByteBuf.WriteUShort(explodeMessageData.objectId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
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

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ExplodeMessageData : MessageData
    {
        public SteamId userId;
        public ushort objectId;
    }
}