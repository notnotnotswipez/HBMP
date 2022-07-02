using HBMP.Nodes;

namespace HBMP.Messages.Handlers
{
    public class DisconnectMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            DisconnectMessageData disconnectMessageData = (DisconnectMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(disconnectMessageData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (DiscordIntegration.hasLobby)
            {
                if (Client.instance != null)
                {
                    Client.instance.Shutdown();
                }
            }
        }
    }
    
    public class DisconnectMessageData : MessageData
    {
        public long userId;
    }
}