using System.Collections.Generic;

namespace HBMP.Messages
{
    public abstract class MessageReader
    {
        public abstract PacketByteBuf CompressData(MessageData messageData);
        public abstract void ReadData(PacketByteBuf packetByteBuf, ulong sender);
        
        public abstract void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender);

        public byte[] WriteTypeToBeginning(PacketType type, PacketByteBuf packetByteBuf)
        {
            List<byte> allBytes = new List<byte>();
            allBytes.Add((byte)type);
            foreach (byte b in packetByteBuf.getBytes()) {
                allBytes.Add(b);
            }
            byte[] byteArray = allBytes.ToArray();

            return byteArray;
        }
    }

    public abstract class MessageData
    {
        
    }
}