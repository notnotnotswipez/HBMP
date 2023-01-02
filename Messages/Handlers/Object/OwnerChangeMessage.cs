using HBMP.Nodes;
using HBMP.Object;
using InteractionSystem;
using MelonLoader;
using Steamworks;
using UnityEngine;

namespace HBMP.Messages.Handlers
{
    public class OwnerChangeMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            OwnerChangeData ownerQueueChangeData = (OwnerChangeData)messageData;
            
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamManager.GetByteId(ownerQueueChangeData.userId));
            packetByteBuf.WriteUShort(ownerQueueChangeData.objectId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SteamId userId = SteamManager.GetLongId(packetByteBuf.ReadByte());
            ushort objectId = packetByteBuf.ReadUShort();

            SyncedObject syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject == null)
            {
                return;
            }

            foreach (Grabber hand in GameObject.Find("[HARD BULLET PLAYER]").GetComponentsInChildren<Grabber>())
            {
                if (hand.HasGrabbedObject)
                {
                    if (hand.CurentGrab.Grabbable.gameObject.Equals(syncedObject.gameObject))
                    {
                        hand.Ungrab();
                    }
                }
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
        public SteamId userId;
        public ushort objectId;
    }
}