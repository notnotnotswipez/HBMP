using System.Collections.Generic;
using HBMP.Extensions;
using HBMP.Messages.Handlers;
using MelonLoader;

namespace HBMP.Messages
{
    public class MessageHandler
    {
        public static Dictionary<NetworkMessageType, MessageReader> MessageReaders =
            new Dictionary<NetworkMessageType, MessageReader>();

        public static void RegisterHandlers()
        {
            MessageReaders.Add(NetworkMessageType.PlayerUpdateMessage, new PlayerSyncReader());
            MessageReaders.Add(NetworkMessageType.ShortIdUpdateMessage, new ShortIdMessage());
            MessageReaders.Add(NetworkMessageType.InitializeSyncMessage, new InitializeSyncMessage());
            MessageReaders.Add(NetworkMessageType.TransformUpdateMessage, new TransformUpdateMessage());
            MessageReaders.Add(NetworkMessageType.OwnerChangeMessage, new OwnerChangeMessage());
            MessageReaders.Add(NetworkMessageType.DisconnectMessage, new DisconnectMessage());
            MessageReaders.Add(NetworkMessageType.GunshotMessage, new GunshotMessage());
            MessageReaders.Add(NetworkMessageType.RequestIdsMessage, new RequestIdsMessage());
            MessageReaders.Add(NetworkMessageType.ExplodeMessage, new ExplodeMessage());
            MessageReaders.Add(NetworkMessageType.JoinCatchupMessage, new JoinCatchupMessage());
            MessageReaders.Add(NetworkMessageType.EnemySpawnMessage, new EnemySpawnMessage());
            MessageReaders.Add(NetworkMessageType.EnemyDestroyMessage, new EnemyDestroyMessage());
            MessageReaders.Add(NetworkMessageType.SceneTransferMessage, new SceneTransferMessage());
            MessageReaders.Add(NetworkMessageType.IkUpdateMessage, new IkSyncMessage());
        }

        public static void ReadMessage(NetworkMessageType messageType, PacketByteBuf packetByteBuf, long sender)
        {
            MessageReaders[messageType].ReadData(packetByteBuf, sender);
        }
        
        public static PacketByteBuf CompressMessage(NetworkMessageType messageType, MessageData messageData)
        {
            PacketByteBuf packetByteBuf = MessageReaders[messageType].CompressData(messageData);
            List<byte> taggedBytes = new List<byte>();
            taggedBytes.Add((byte) messageType);
            foreach (byte b in packetByteBuf.getBytes()) {
                taggedBytes.Add(b);
            }
            byte[] finalArray = taggedBytes.ToArray();
            return new PacketByteBuf(finalArray);
        }
    }
}