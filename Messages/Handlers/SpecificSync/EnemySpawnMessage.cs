using System;
using System.Linq;
using System.Reflection;
using HBMP.Nodes;
using HBMP.Object;
using HBMP.Patches;
using MelonLoader;
using NodeCanvas.Framework;
using UnityEngine;

namespace HBMP.Messages.Handlers
{
    public class EnemySpawnMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            EnemySpawnMessageData enemySpawnMessageData = (EnemySpawnMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(enemySpawnMessageData.userId));
            packetByteBuf.WriteUShort(enemySpawnMessageData.groupId);
            packetByteBuf.WriteUShort(enemySpawnMessageData.startingObjectId);
            packetByteBuf.WriteBool(enemySpawnMessageData.shouldRevertToOwner);
            packetByteBuf.create();
            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            long userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            ushort groupId = packetByteBuf.ReadUShort();
            ushort lastObjectId = packetByteBuf.ReadUShort();
            bool shouldRevertToOwner = packetByteBuf.ReadBoolean();
            if (SyncedObject.lastId <= lastObjectId)
            {
                SyncedObject.lastId = lastObjectId;
            }
            else
            {
                MelonLogger.Error("Theres a mismatch that cannot be fixed at this time, your clients local sync id: " +
                                  SyncedObject.lastId + ", is greater than" +
                                  " requested: " + lastObjectId + ", try reloading the scene?");
                return;
            }

            Type spawnerType = typeof(InfinityWaveSpawner);
            InfinityWaveSpawner infinityWaveSpawner =
                Resources.FindObjectsOfTypeAll<InfinityWaveSpawner>().FirstOrDefault();

            GameObject enemyPrefab = null;

            if (infinityWaveSpawner != null)
            {
                FieldInfo fieldInfo =
                    spawnerType.GetField("_enemyPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
                enemyPrefab = ((EnemyRoot)fieldInfo.GetValue(infinityWaveSpawner)).gameObject;
            }
            else
            {
                MelonLogger.Error("No infinity wave spawner on the map, no prefab to take.");
                return;
            }
            
            if (enemyPrefab != null)
            {
                GameObject spawnedNPC = GameObject.Instantiate(enemyPrefab);
                spawnedNPC.GetComponent<EnemyRoot>().HealthContainer.IgnoreDamage = false;
                spawnedNPC.GetComponentInChildren<Blackboard>().SetVariableValue("Target", HBMF.r.player.transform);

                foreach (InventoryPocket enemyPocket in spawnedNPC.GetComponentsInChildren<InventoryPocket>())
                {
                    enemyPocket.TakeOutSocketableFromPocket();
                }
                SyncedObject.spawnedEnemies.Add(spawnedNPC.GetComponent<EnemyRoot>());

                EnemyRoot enemyRoot = null;
                foreach (Rigidbody rigidbody in SyncedObject.GetProperRigidBodies(spawnedNPC.transform, true))
                {
                    GameObject npcObj = rigidbody.gameObject;
                    SyncedObject.FutureProofSync(npcObj, groupId, userId);
                    if (enemyRoot == null)
                    {
                        enemyRoot = npcObj.GetComponentInParent<EnemyRoot>();
                    }
                }

                if (enemyRoot != null)
                {
                    if (shouldRevertToOwner)
                    {
                        SyncedObject.returnedEnemyRoots.Add(enemyRoot, userId);
                    }
                    SyncedObject.FutureProofSync(enemyRoot.gameObject, groupId, userId);
                }
                SyncedObject.lastId++;
            }
            else
            {
                MelonLogger.Error("Enemy prefab was null!");
                return;
            }
        }
    }

    public class EnemySpawnMessageData : MessageData
    {
        public long userId;
        public ushort groupId;
        public ushort startingObjectId;
        public bool shouldRevertToOwner;
    }
}