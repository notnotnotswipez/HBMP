using System;
using System.Collections.Generic;
using System.Linq;
using HBMP.DataType;
using HBMP.Extensions;
using HBMP.Nodes;
using HBMP.Representations;
using MelonLoader;
using Steamworks;
using TMPro;
using UnityEngine;

namespace HBMP.Messages.Handlers
{
    public class PlayerSyncReader : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            PlayerSyncMessageData playerSyncMessageData = (PlayerSyncMessageData)messageData;
            List<byte> rawBytes = new List<byte>();

            rawBytes.Add(SteamIntegration.GetByteId(playerSyncMessageData.userId));

            for (int r = 0; r < playerSyncMessageData.simplifiedTransforms.Length; r++)
                rawBytes.AddRange(playerSyncMessageData.simplifiedTransforms[r].GetBytes());

            byte[] finalBytes = rawBytes.ToArray();

            PacketByteBuf packetByteBuf = new PacketByteBuf(finalBytes);

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            if (packetByteBuf.getBytes().Length <= 0)
                throw new IndexOutOfRangeException();

            int index = 0;

            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.getBytes()[index++]);
            if (userId == 0)
            {
                RequestIdsMessageData requestIdsMessageData = new RequestIdsMessageData()
                {
                    userId = SteamIntegration.currentId
                };
                PacketByteBuf shortBuf =
                    PacketHandler.CompressMessage(PacketType.RequestIdsMessage, requestIdsMessageData);
                SteamPacketNode.BroadcastMessage(NetworkChannel.Reliable, shortBuf.getBytes());
                return;
            }

            List<byte> data = packetByteBuf.getBytes().ToList();

            List<SimplifiedTransform> simplifiedTransforms = new List<SimplifiedTransform>();
            for (int i = 0; i < 3; i++) {
                SimplifiedTransform simpleTransform = SimplifiedTransform.FromBytes(data.GetRange(index, SimplifiedTransform.size).ToArray());
                index += SimplifiedTransform.size;
                simplifiedTransforms.Add(simpleTransform);
            }

            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                PlayerRepresentation playerRepresentation = PlayerRepresentation.representations[userId];
                
                playerRepresentation.UpdateTransforms(simplifiedTransforms.ToArray());
            }
            else
            {
                MelonLogger.Error(
                    "Something is wrong, player representation sent update but doesnt exist, requesting updates from host.");
                RequestIdsMessageData requestIdsMessageData = new RequestIdsMessageData()
                {
                    userId = SteamIntegration.currentId
                };
                PacketByteBuf shortBuf =
                    PacketHandler.CompressMessage(PacketType.RequestIdsMessage, requestIdsMessageData);
                SteamPacketNode.BroadcastMessage(NetworkChannel.Reliable, shortBuf.getBytes());
            }
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new NotImplementedException();
        }
    }

    public class PlayerSyncMessageData : MessageData
    {
        public SteamId userId;
        public SimplifiedTransform[] simplifiedTransforms = new SimplifiedTransform[3];
    }
}