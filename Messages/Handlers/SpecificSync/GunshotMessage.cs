using System;
using System.Collections.Generic;
using System.Reflection;
using FirearmSystem;
using GameCore.Effects;
using HBMP.Nodes;
using HBMP.Object;
using HBMP.Patches;
using InteractionSystem;
using UnityEngine;

namespace HBMP.Messages.Handlers
{
    public class GunshotMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            GunshotMessageData gunshotMessageData = (GunshotMessageData)messageData;
            
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(gunshotMessageData.userId));
            packetByteBuf.WriteUShort(gunshotMessageData.objectId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            long userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            ushort objectId = packetByteBuf.ReadUShort();
            
            SyncedObject syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject)
            {
                PatchVariables.shouldIgnoreFire = true;
                DefaultSupply defaultSupply = syncedObject.GetComponentInChildren<DefaultSupply>();
                ProjectileContainer container = defaultSupply.DefaultProjectileContainer;
                Type projectileContainerType = typeof(ProjectileContainer);
                FieldInfo objectList = projectileContainerType.GetField("_projectilePrefab", BindingFlags.NonPublic | BindingFlags.Instance);
                GameObject projectile = (GameObject)objectList.GetValue(container);
                
                foreach (Chamber chamber in syncedObject.GetComponentsInChildren<Chamber>()) {
                    chamber.InjectBulletInChamber(projectile);
                    chamber.TryShot();
                }
                PatchVariables.shouldIgnoreFire = false;
            }
            else
            {
                string gunReplacement = "Mp5A3";
                GameObject potentialPrefab = SyncedObject.GetInteractiveEntityInScene(gunReplacement);
                potentialPrefab.SetActive(true);
                
                Socket socket = potentialPrefab.GetComponentInParent<Socket>();
                if (socket)
                {
                    socket.ForceUnsnapObjectFromSocket();
                }

                GameObject copied = GameObject.Instantiate(potentialPrefab);
                GameObject.Destroy(potentialPrefab);
                copied.name += "(Made copy)";
                // Temp group ID, will break, but things are broken already and this makes it better than air.
                SyncedObject.MakeSyncedObject(copied, objectId, userId, 999, false);
                
                syncedObject = SyncedObject.GetSyncedObject(objectId);
                if (syncedObject != null)
                {
                    PatchVariables.shouldIgnoreFire = true;
                    DefaultSupply defaultSupply = syncedObject.GetComponentInChildren<DefaultSupply>();
                    ProjectileContainer container = defaultSupply.DefaultProjectileContainer;
                    Type projectileContainerType = typeof(ProjectileContainer);
                    FieldInfo objectList = projectileContainerType.GetField("_projectilePrefab", BindingFlags.NonPublic | BindingFlags.Instance);
                    GameObject projectile = (GameObject)objectList.GetValue(container);
                
                    foreach (Chamber chamber in syncedObject.GetComponentsInChildren<Chamber>()) {
                        chamber.InjectBulletInChamber(projectile);
                        chamber.TryShot();
                    }
                    PatchVariables.shouldIgnoreFire = false;
                }
            }
        }
    }

    public class GunshotMessageData : MessageData
    {
        public long userId;
        public ushort objectId;
    }
}