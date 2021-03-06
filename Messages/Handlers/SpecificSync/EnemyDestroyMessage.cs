using System.Collections.Generic;
using System.Linq;
using HBMP.Nodes;
using HBMP.Object;
using HBMP.Patches;
using UnityEngine;

namespace HBMP.Messages.Handlers
{
    public class EnemyDestroyMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            EnemyDestroyMessageData enemyDestroyMessageData = (EnemyDestroyMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(enemyDestroyMessageData.userId));
            packetByteBuf.WriteUShort(enemyDestroyMessageData.groupId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            long userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            ushort groupId = packetByteBuf.ReadUShort();
            List<SyncedObject> objects = SyncedObject.relatedSyncedObjects[groupId];
            EnemyRoot enemyRoot = null;
            foreach (SyncedObject synced in objects)
            {
                if (!synced.GetComponentInParent<EnemyRoot>()) continue;
                enemyRoot = synced.GetComponentInParent<EnemyRoot>();
                break;
            }

            if (enemyRoot != null)
            {
                SyncedObject.spawnedEnemies.Remove(enemyRoot);
                GameObject.Destroy(enemyRoot.gameObject);
            }
        }
    }

    public class EnemyDestroyMessageData : MessageData
    {
        public long userId;
        public ushort groupId;

    }
}