using System;
using System.Collections.Generic;
using System.Reflection;
using FirearmSystem;
using GameCore.Effects;
using HBMP.Nodes;
using HBMP.Object;
using HBMP.Patches;
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
        }
    }

    public class GunshotMessageData : MessageData
    {
        public long userId;
        public ushort objectId;
    }
}