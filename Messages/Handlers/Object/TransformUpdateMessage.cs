using System;
using System.Collections.Generic;
using System.Linq;
using HBMP.DataType;
using HBMP.Nodes;
using HBMP.Object;
using MelonLoader;
using Steamworks;

namespace HBMP.Messages.Handlers
{
    public class TransformUpdateMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            TransformUpdateData transformUpdateData = (TransformUpdateData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(transformUpdateData.objectId);
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(transformUpdateData.userId));
            packetByteBuf.WriteSimpleTransform(transformUpdateData.sTransform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            ushort objectId = packetByteBuf.ReadUShort();
            SyncedObject syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject == null)
            {
                return;
            }

            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            List<byte> transformBytes = new List<byte>();
            for (int i = packetByteBuf.byteIndex; i < packetByteBuf.getBytes().Length; i++) {
                transformBytes.Add(packetByteBuf.getBytes()[i]);
            }
            SimplifiedTransform simpleTransform = SimplifiedTransform.FromBytes(transformBytes.ToArray());

            syncedObject.UpdateObject(simpleTransform);
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new NotImplementedException();
        }
    }

    public class TransformUpdateData : MessageData
    {
        public SteamId userId;
        public ushort objectId;
        public SimplifiedTransform sTransform;
    }
}