namespace HBMP.Messages
{
    public class DebugAssistance
    {
        public static void SimulatePacket(PacketType netType, MessageData messageData)
        {
            PacketByteBuf packetByteBuf = PacketHandler.CompressMessage(netType, messageData);
            
            byte[] data = packetByteBuf.getBytes();
            byte messageType = data[0];
            byte[] realData = new byte[data.Length - sizeof(byte)];

            for (int b = sizeof(byte); b < data.Length; b++)
                realData[b - sizeof(byte)] = data[b];

            PacketByteBuf secondBuf = new PacketByteBuf(realData);
            
            PacketHandler.ReadMessage((PacketType)messageType, secondBuf, 0, false);
        }
    }
}