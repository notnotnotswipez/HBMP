using HBMP.Messages;

namespace HBMP.Utils
{
    public class Utils
    {

        public class EmptyMessageData : MessageData
        {
            public PacketByteBuf additional;
            
            public EmptyMessageData(PacketByteBuf packetByteBuf)
            {
                additional = packetByteBuf;
            }
        }
    }
}