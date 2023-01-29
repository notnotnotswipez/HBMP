using HBMP.Nodes;
using HBMP.Object;
using MelonLoader;
using Steamworks;
using UnityEngine.SceneManagement;

namespace HBMP.Messages.Handlers.Network
{
    public class PlayerGreetingMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            // Doesnt matter
            return new PacketByteBuf();
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            // Not gonna be on the client
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            MelonLogger.Msg("Got greeting packet on the server! Sending byte indexes");
            foreach (var valuePair in SteamIntegration.byteIds)
            {
                if (valuePair.Value == sender) continue;

                var addMessageData = new ShortIdMessageData()
                {
                    userId = valuePair.Value,
                    byteId = valuePair.Key
                };
                var byteMessageData =
                    PacketHandler.CompressMessage(PacketType.ShortIdUpdateMessage, addMessageData);
                SteamPacketNode.BroadcastMessage((byte)NetworkChannel.Reliable, byteMessageData.getBytes(), false);
            }

            var idMessageData = new ShortIdMessageData
            {
                userId = sender,
                byteId = SteamIntegration.RegisterUser(sender)
            };
            var secondBuff = PacketHandler.CompressMessage(PacketType.ShortIdUpdateMessage, idMessageData);
            SteamPacketNode.BroadcastMessage((byte)NetworkChannel.Reliable, secondBuff.getBytes(), false);

            var joinCatchupData = new JoinCatchupData
            {
                lastId = SyncedObject.lastId,
                lastGroupId = SyncedObject.lastGroupId
            };
            var catchupBuff = PacketHandler.CompressMessage(PacketType.JoinCatchupMessage, joinCatchupData);
            SteamPacketNode.SendMessage(sender, (byte)NetworkChannel.Reliable, catchupBuff.getBytes(), false);
            
            if (sender != SteamClient.SteamId)
            {
                SceneTransferData sceneChangePacket = new SceneTransferData()
                {
                    sceneIndex = SceneManager.GetActiveScene().buildIndex
                };
                var sceneBuff = PacketHandler.CompressMessage(PacketType.SceneTransferMessage, sceneChangePacket);
                SteamPacketNode.SendMessage(sender, NetworkChannel.Reliable, sceneBuff.getBytes());
            }
        }
    }
}