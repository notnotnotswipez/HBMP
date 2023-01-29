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
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(disconnectMessageData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            if (SteamIntegration.hasLobby)
            {
                SteamIntegration.Disconnect(false);
            }
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new System.NotImplementedException();
        }
    }
    
    public class DisconnectMessageData : MessageData
    {
        public SteamId userId;
    }
}