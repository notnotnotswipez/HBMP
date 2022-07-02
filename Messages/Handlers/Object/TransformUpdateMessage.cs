using System;
using System.Collections.Generic;
using System.Linq;
using HBMP.DataType;
using HBMP.Nodes;
using HBMP.Object;
using MelonLoader;

namespace HBMP.Messages.Handlers
{
    public class TransformUpdateMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            TransformUpdateData transformUpdateData = (TransformUpdateData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(transformUpdateData.objectId);
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(transformUpdateData.userId));
            packetByteBuf.WriteSimpleTransform(transformUpdateData.sTransform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            ushort objectId = packetByteBuf.ReadUShort();
            SyncedObject syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject == null)
            {
                return;
            }

            long userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            List<byte> transformBytes = new List<byte>();
            for (int i = packetByteBuf.byteIndex; i < packetByteBuf.getBytes().Length; i++) {
                transformBytes.Add(packetByteBuf.getBytes()[i]);
            }
            SimplifiedTransform simpleTransform = SimplifiedTransform.FromBytes(transformBytes.ToArray());

            syncedObject.UpdateObject(simpleTransform);

            if (Server.instance != null)
            {
                byte[] byteArray = WriteTypeToBeginning(NetworkMessageType.TransformUpdateMessage, packetByteBuf);
                Server.instance.BroadcastMessageExcept((byte)NetworkChannel.Unreliable, byteArray, userId);
            }
        }
    }

    public class TransformUpdateData : MessageData
    {
        public long userId;
        public ushort objectId;
        public SimplifiedTransform sTransform;
    }
}