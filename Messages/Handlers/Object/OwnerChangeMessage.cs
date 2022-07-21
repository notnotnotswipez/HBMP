using HBMP.Nodes;
using HBMP.Object;
using MelonLoader;

namespace HBMP.Messages.Handlers
{
    public class OwnerChangeMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            OwnerChangeData ownerQueueChangeData = (OwnerChangeData)messageData;
            
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(ownerQueueChangeData.userId));
            packetByteBuf.WriteUShort(ownerQueueChangeData.objectId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            long userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            ushort objectId = packetByteBuf.ReadUShort();

            SyncedObject syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject == null)
            {
                return;
            }
            
            syncedObject.SetOwner(userId);

            if (SyncedObject.relatedSyncedObjects.ContainsKey(syncedObject.groupId))
            {
                foreach (SyncedObject relatedSync in SyncedObject.relatedSyncedObjects[syncedObject.groupId]) {
                    relatedSync.SetOwner(userId);
                }
            }
        }
    }

    public class OwnerChangeData : MessageData
    {
        public long userId;
        public ushort objectId;
    }
}