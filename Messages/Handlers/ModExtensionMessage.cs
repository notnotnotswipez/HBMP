namespace HBMP.Messages.Handlers
{
    public abstract class ModExtensionMessage
    {
        public abstract PacketByteBuf CompressData(MessageData messageData);
        public abstract void HandleData(PacketByteBuf packetByteBuf);
    }
}