using HBMP.Nodes;
using Steamworks;

namespace HBMP.Messages.Handlers
{
    public class DisconnectMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            DisconnectMessageData disconnectMessageData = (DisconnectMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamManager.GetByteId(disconnectMessageData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (SteamManager.Instance.isConnectedToLobby)
            {
                SteamManager.Disconnect(false);
            }
        }
    }
    
    public class DisconnectMessageData : MessageData
    {
        public SteamId userId;
    }
}