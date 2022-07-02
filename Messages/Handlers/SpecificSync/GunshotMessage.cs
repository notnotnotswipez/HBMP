using GameCore.Effects;
using HBMP.Nodes;
using HBMP.Object;

namespace HBMP.Messages.Handlers
{
    public class GunshotMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            GunshotMessageData gunshotMessageData = (GunshotMessageData)messageData;
            
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(gunshotMessageData.userId));
            packetByteBuf.WriteUShort(gunshotMessageData.objectId);
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
                foreach (PlayMuzzleFlashFromPool muzzleFlash in syncedObject.GetComponentsInChildren<PlayMuzzleFlashFromPool>()) {
                    muzzleFlash.PlayEffect();
                }
            }
            
            if (Server.instance != null)
            {
                byte[] byteArray = WriteTypeToBeginning(NetworkMessageType.GunshotMessage, packetByteBuf);
                Server.instance.BroadcastMessageExcept((byte)NetworkChannel.Object, byteArray, userId);
            }
        }
    }

    public class GunshotMessageData : MessageData
    {
        public long userId;
        public ushort objectId;
    }
}