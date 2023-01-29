using System.Collections.Generic;
using HBMP.Extensions;
using HBMP.Messages.Handlers;
using HBMP.Messages.Handlers.Network;
using MelonLoader;

namespace HBMP.Messages
{
    public class PacketHandler
    {
        public static Dictionary<PacketType, MessageReader> MessageReaders =
            new Dictionary<PacketType, MessageReader>();

        public static void RegisterHandlers()
        {
            MessageReaders.Add(PacketType.PlayerUpdateMessage, new PlayerSyncReader());
            MessageReaders.Add(PacketType.ShortIdUpdateMessage, new ShortIdMessage());
            MessageReaders.Add(PacketType.InitializeSyncMessage, new InitializeSyncMessage());
            MessageReaders.Add(PacketType.TransformUpdateMessage, new TransformUpdateMessage());
            MessageReaders.Add(PacketType.OwnerChangeMessage, new OwnerChangeMessage());
            MessageReaders.Add(PacketType.DisconnectMessage, new DisconnectMessage());
            MessageReaders.Add(PacketType.GunshotMessage, new GunshotMessage());
            MessageReaders.Add(PacketType.RequestIdsMessage, new RequestIdsMessage());
            MessageReaders.Add(PacketType.ExplodeMessage, new ExplodeMessage());
            MessageReaders.Add(PacketType.JoinCatchupMessage, new JoinCatchupMessage());
            MessageReaders.Add(PacketType.EnemySpawnMessage, new EnemySpawnMessage());
            MessageReaders.Add(PacketType.EnemyDestroyMessage, new EnemyDestroyMessage());
            MessageReaders.Add(PacketType.SceneTransferMessage, new SceneTransferMessage());
            MessageReaders.Add(PacketType.IkUpdateMessage, new IkSyncMessage());
            MessageReaders.Add(PacketType.ModMessage, new ModMessage());
            MessageReaders.Add(PacketType.ClientDistributionMessage, new ClientDistributionMessage());
            MessageReaders.Add(PacketType.PlayerGreetingMessage, new PlayerGreetingMessage());
            MessageReaders.Add(PacketType.PlayerConfirmationMessage, new PlayerConfirmPacket());
        }

        public static void ReadMessage(PacketType messageType, PacketByteBuf packetByteBuf, ulong sender, bool server)
        {
            var reader = MessageReaders[messageType];
            if (server)
            {
                // Server should have the ENTIRE buffer including packet index.
                PacketByteBuf fullBuff = new PacketByteBuf();
                fullBuff.WriteByte((byte)messageType);
                fullBuff.WriteBytes(packetByteBuf.getBytes());
                fullBuff.create();
                
                reader.ReadDataServer(fullBuff, sender);
            }
            else
            {
                reader.ReadData(packetByteBuf, sender);
            }
        }
        
        public static PacketByteBuf CompressMessage(PacketType messageType, MessageData messageData)
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
        
        public static PacketByteBuf CompressMessage(ushort extensionId, MessageData messageData)
        {
            ModMessage modMessage = (ModMessage)MessageReaders[PacketType.ModMessage];
            PacketByteBuf packetByteBuf = modMessage.CompressData(messageData, extensionId);
            List<byte> taggedBytes = new List<byte>();
            taggedBytes.Add((byte) PacketType.ModMessage);
            foreach (byte b in packetByteBuf.getBytes()) {
                taggedBytes.Add(b);
            }
            byte[] finalArray = taggedBytes.ToArray();
            return new PacketByteBuf(finalArray);
        }
    }
}