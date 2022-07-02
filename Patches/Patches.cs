using System;
using HarmonyLib;
using HBMP.Messages;
using HBMP.Messages.Handlers;
using HBMP.Nodes;
using HBMP.Object;
using InteractionSystem;
using MelonLoader;
using UnityEngine;

namespace HBMP.Patches
{
    [HarmonyPatch(typeof(Grabbable), "AttemptGrab", new Type[] { typeof(Grabber), 
            typeof(TestGrab.GrabPoint), typeof(ConfigurableJoint), typeof(GrabType)})]
        class GrabPatch
        {
            public static void Postfix(Grabbable __instance, Grabber grabber, TestGrab.GrabPoint grabPoint, ConfigurableJoint configurableJoint, GrabType grabType)
            {
                SyncedObject syncedObject = __instance.GetComponent<SyncedObject>();
                if (syncedObject == null)
                {
                    SyncedObject.Sync(__instance.gameObject);
                }
                else
                {
                    syncedObject.BroadcastOwnerChange();
                }
            }
        }
        
        [HarmonyPatch(typeof(Chamber), "FireProjectile")]
        class GunPatch
        {
            public static void Postfix(Chamber __instance)
            {
                SyncedObject syncedObject = __instance.gameObject.GetComponentInParent<SyncedObject>();
                if (syncedObject)
                {
                    GunshotMessageData gunshotMessageData = new GunshotMessageData()
                    {
                        userId = DiscordIntegration.currentUser.Id,
                        objectId = syncedObject.currentId
                    };

                    PacketByteBuf packetByteBuf =
                        MessageHandler.CompressMessage(NetworkMessageType.GunshotMessage, gunshotMessageData);
                    
                    Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
                }
            }
        }
        
        [HarmonyPatch(typeof(Explodeable), "Explode")]
        class ExplosivesPatch
        {
            public static void Prefix(Explodeable __instance)
            {
                SyncedObject syncedObject = __instance.gameObject.GetComponentInParent<SyncedObject>();
                if (syncedObject)
                {
                    if (!syncedObject.IsClientSimulated())
                    {
                        return;
                    }

                    ExplodeMessageData explodeMessageData = new ExplodeMessageData()
                    {
                        userId = DiscordIntegration.currentUser.Id,
                        objectId = syncedObject.currentId
                    };

                    PacketByteBuf packetByteBuf =
                        MessageHandler.CompressMessage(NetworkMessageType.ExplodeMessage, explodeMessageData);
                    
                    Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
                }
            }
        }
}