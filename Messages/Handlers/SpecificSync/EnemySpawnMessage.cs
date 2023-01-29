using System;
using System.Linq;
using System.Reflection;
using HBMP.Nodes;
using HBMP.Object;
using HBMP.Patches;
using HBMP.Utils;
using MelonLoader;
using NodeCanvas.Framework;
using Steamworks;
using UnityEngine;

namespace HBMP.Messages.Handlers
{
    public class EnemySpawnMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            EnemySpawnMessageData enemySpawnMessageData = (EnemySpawnMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(enemySpawnMessageData.userId));
            packetByteBuf.WriteUShort(enemySpawnMessageData.groupId);
            packetByteBuf.WriteUShort(enemySpawnMessageData.startingObjectId);
            packetByteBuf.WriteBool(enemySpawnMessageData.shouldRevertToOwner);
            packetByteBuf.create();
            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
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
            
            EnemyRoot enemyPrefab = null;

            foreach (InfinityWaveSpawner checkedSpawner in Resources.FindObjectsOfTypeAll<InfinityWaveSpawner>())
            {
                if (!checkedSpawner.gameObject.activeInHierarchy)
                {
                    continue;
                }

                enemyPrefab = ReflectionHelper.GetPrivateField<EnemyRoot>(checkedSpawner, "_enemyPrefab");
                if (enemyPrefab != null)
                {
                    break;
                }
            }
            
            if (enemyPrefab != null)
            {
                GameObject spawnedNPC = GameObject.Instantiate(enemyPrefab.gameObject);
                spawnedNPC.GetComponent<EnemyRoot>().HealthContainer.IgnoreDamage = false;
                spawnedNPC.GetComponentInChildren<Blackboard>().SetVariableValue("Target", PlayerUtils.GetRandomPlayerHead());

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

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new NotImplementedException();
        }
    }

    public class EnemySpawnMessageData : MessageData
    {
        public SteamId userId;
        public ushort groupId;
        public ushort startingObjectId;
        public bool shouldRevertToOwner;
    }
}