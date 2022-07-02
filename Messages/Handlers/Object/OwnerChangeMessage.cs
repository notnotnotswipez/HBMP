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

            // Related syncables must also be ownership transferred, better to not send multiple packets at once to reduce load on network.
            if (SyncedObject.relatedSyncedObjects.ContainsKey(syncedObject.groupId))
            {
                MelonLogger.Msg("Transferring related synced objects for object, count: "+SyncedObject.relatedSyncedObjects[syncedObject.groupId].Count);
                foreach (SyncedObject relatedSync in SyncedObject.relatedSyncedObjects[syncedObject.groupId]) {
                    relatedSync.SetOwner(userId);
                }
            }

            if (Server.instance != null)
            {
                byte[] finalBytes = WriteTypeToBeginning(NetworkMessageType.OwnerChangeMessage, packetByteBuf);
                Server.instance.BroadcastMessageExcept((byte)NetworkChannel.Transaction, finalBytes, userId);
            }
        }
    }

    public class OwnerChangeData : MessageData
    {
        public long userId;
        public ushort objectId;
    }
}