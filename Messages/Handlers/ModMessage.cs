using System.Collections.Generic;

namespace HBMP.Messages.Handlers
{
    public class ModMessage : MessageReader
    {
        public static Dictionary<ushort, ModExtensionMessage> Extensions = new Dictionary<ushort, ModExtensionMessage>();
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            return new PacketByteBuf();
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            ushort extensionId = packetByteBuf.ReadUShort();
            if (Extensions.ContainsKey(extensionId))
            {
                ModExtensionMessage modExtensionMessage = Extensions[extensionId];
                modExtensionMessage.HandleData(packetByteBuf);
            }
        }

        public PacketByteBuf CompressData(MessageData messageData, ushort extensionId)
        {
            if (Extensions.ContainsKey(extensionId))
            {
                ModExtensionMessage modExtensionMessage = Extensions[extensionId];
                PacketByteBuf packetByteBuf = new PacketByteBuf();
                packetByteBuf.WriteUShort(extensionId);
                packetByteBuf.WriteBytes(modExtensionMessage.CompressData(messageData).getBytes());
                packetByteBuf.create();
                return packetByteBuf;
            }
            return new PacketByteBuf();
        }
    }
}