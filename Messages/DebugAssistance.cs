namespace HBMP.Messages
{
    public class DebugAssistance
    {
        public static void SimulatePacket(NetworkMessageType netType, MessageData messageData)
        {
            PacketByteBuf packetByteBuf = MessageHandler.CompressMessage(netType, messageData);
            
            byte[] data = packetByteBuf.getBytes();
            byte messageType = data[0];
            byte[] realData = new byte[data.Length - sizeof(byte)];

            for (int b = sizeof(byte); b < data.Length; b++)
                realData[b - sizeof(byte)] = data[b];

            PacketByteBuf secondBuf = new PacketByteBuf(realData);
            
            MessageHandler.ReadMessage((NetworkMessageType)messageType, secondBuf, DiscordIntegration.currentUser.Id);
        }
    }
}