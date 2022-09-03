using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using FirearmSystem;
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
    class PatchCoroutines
    {

        public static IEnumerator WaitForGunshot(GameObject gameObject)
        {
            SyncedObject syncedObject = gameObject.GetComponentInParent<SyncedObject>();
            if (syncedObject)
            {
                GunshotMessageData gunshotMessageData = new GunshotMessageData()
                {
                    userId = DiscordIntegration.currentUser.Id,
                    objectId = syncedObject.currentId
                };

                PacketByteBuf packetByteBuf =
                    MessageHandler.CompressMessage(NetworkMessageType.GunshotMessage, gunshotMessageData);

                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Attack, packetByteBuf.getBytes());
            }

            yield break;
        }

        public static IEnumerator WaitForProperUnGrab(GameObject gameObject)
        {
            SyncedObject syncedObject = gameObject.GetComponent<SyncedObject>();
            if (syncedObject != null)
            {
                EnemyRoot enemyRoot = SyncedObject.FindEnemyRoot(syncedObject.gameObject);
                if (enemyRoot)
                {
                    if (SyncedObject.returnedEnemyRoots.ContainsKey(enemyRoot))
                    {
                        long ownerId = SyncedObject.returnedEnemyRoots[enemyRoot];
                        object coroutineTask =
                            MelonCoroutines.Start(PatchCoroutines.WaitForEnemyReSync(syncedObject, ownerId));
                        PatchVariables.deletionCourotine.Add(enemyRoot, coroutineTask);
                    }
                }
            }

            yield break;
        }

        public static IEnumerator WaitForProperGrab(GameObject gameObject)
        {
            SyncedObject syncedObject = gameObject.GetComponent<SyncedObject>();
            if (syncedObject == null)
            {
                SyncedObject.Sync(gameObject);
            }
            else
            {
                syncedObject.BroadcastOwnerChange();
            }

            if (syncedObject)
            {
                EnemyRoot enemyRoot = SyncedObject.FindEnemyRoot(syncedObject.gameObject);
                if (enemyRoot)
                {
                    if (PatchVariables.deletionCourotine.ContainsKey(enemyRoot))
                    {
                        MelonCoroutines.Stop(PatchVariables.deletionCourotine[enemyRoot]);
                        PatchVariables.deletionCourotine.Remove(enemyRoot);
                    }
                }
            }

            yield break;
        }

        public static IEnumerator WaitForEnemyReSync(SyncedObject syncedObject, long userId)
        {
            yield return new WaitForEnemyDead(SyncedObject.FindEnemyRoot(syncedObject.gameObject).HealthContainer);
            yield return new WaitForSecondsRealtime(2f);
            syncedObject.ManualSetOwner(userId, true);
            PatchVariables.deletionCourotine.Remove(SyncedObject.FindEnemyRoot(syncedObject.gameObject));
            yield break;
        }

        public static IEnumerator WaitForEnemySyncInfinity(List<HealthContainer> enemies)
        {
            yield return new WaitForFixedUpdate();
            HealthContainer container = enemies[enemies.Count - 1];
            EnemyRoot enemyRoot = container.GetComponentInParent<EnemyRoot>();
            SyncedObject.SyncNPC(enemyRoot, true);
            yield break;
        }

        public static IEnumerator WaitForEnemySyncSingular(GameObject enemy)
        {
            yield return new WaitForFixedUpdate();
            HealthContainer container = enemy.GetComponentInChildren<HealthContainer>();
            if (!container)
            {
                container = enemy.GetComponentInParent<HealthContainer>();
            }

            EnemyRoot enemyRoot = container.GetComponentInParent<EnemyRoot>();
            SyncedObject.SyncNPC(enemyRoot, true);
            yield break;
        }

        public static IEnumerator WaitForEnemySyncGenerator(List<GameObject> enemies)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            EnemyRoot enemyRoot = enemies[enemies.Count - 1].GetComponent<EnemyRoot>();
            SyncedObject.SyncNPC(enemyRoot, false);
            yield break;
        }

        public static IEnumerator WaitForEnemyDeletion(List<ushort> groupIds)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            for (int i = 0; i < groupIds.Count; i++)
            {
                ushort groupId = groupIds[i];

                EnemyDestroyMessageData enemyDestroyMessageData = new EnemyDestroyMessageData()
                {
                    userId = DiscordIntegration.currentUser.Id,
                    groupId = groupId
                };

                PacketByteBuf packetByteBuf =
                    MessageHandler.CompressMessage(NetworkMessageType.EnemyDestroyMessage, enemyDestroyMessageData);

                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
            }
        }
    }

    class PatchVariables
    {
        public static Dictionary<EnemyRoot, object> deletionCourotine = new Dictionary<EnemyRoot, object>();
        public static bool shouldIgnoreFire = false;
    }

    [HarmonyPatch(typeof(EntitySpawner), "InitializeEntity",
        new Type[] { typeof(GameObject), typeof(EntitySpawnProperties) })]
    class EntitySpawnerPatch
    {
        public static void Postfix(EntitySpawner __instance, GameObject entity, EntitySpawnProperties properties,
            bool __result)
        {
            if (DiscordIntegration.hasLobby)
            {
                if (__result)
                {
                    MelonLogger.Msg("Created entity.");
                    MelonLogger.Msg("Spawned entity from entity spawner.");
                    MelonCoroutines.Start(PatchCoroutines.WaitForEnemySyncSingular(entity));
                }
            }
        }
    }

    [HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.ReloadCurrentScene))]
    class ReloadScenePatch
    {
        public static bool Prefix(SceneLoader __instance)
        {
            if (DiscordIntegration.hasLobby)
            {
                if (!DiscordIntegration.isHost)
                {
                    CameraFader cameraFader =
                        UnityEngine.Resources.FindObjectsOfTypeAll<CameraFader>().FirstOrDefault();
                    cameraFader.FadeOut(1);
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(InfinityWaveSpawner), "TrySpawn")]
    class InfinityWavePatch
    {
        public static void Postfix(InfinityWaveSpawner __instance, ref List<HealthContainer> ____aliveEnemies,
            bool __result)
        {
            if (DiscordIntegration.hasLobby)
            {
                if (__result)
                {
                    MelonCoroutines.Start(PatchCoroutines.WaitForEnemySyncInfinity(____aliveEnemies));
                }
            }
        }
    }

    [HarmonyPatch(typeof(InfinityWaveSpawner), "StopSpawnEnemies")]
    class InfinityWaveClearPatch
    {
        public static void Prefix(InfinityWaveSpawner __instance, ref List<HealthContainer> ____allEnemies)
        {
            if (DiscordIntegration.hasLobby)
            {
                List<ushort> groupIds = new List<ushort>();
                foreach (HealthContainer healthContainer in ____allEnemies)
                {
                    EnemyRoot enemyRoot = healthContainer.gameObject.GetComponentInParent<EnemyRoot>();
                    if (!enemyRoot.gameObject.GetComponent<SyncedObject>())
                    {
                        continue;
                    }

                    SyncedObject syncedObject = enemyRoot.gameObject.GetComponent<SyncedObject>();
                    groupIds.Add(syncedObject.groupId);
                }

                MelonCoroutines.Start(PatchCoroutines.WaitForEnemyDeletion(groupIds));
            }
        }
    }

    [HarmonyPatch(typeof(EnemySpawnerFromGenerator), nameof(EnemySpawnerFromGenerator.Spawn),
        new Type[] { typeof(EnemyData) })]
    class EnemySpawnPatch
    {
        public static void Postfix(EnemySpawnerFromGenerator __instance, EnemyData enemyData,
            ref List<GameObject> ___spawnedObjects)
        {
            if (DiscordIntegration.hasLobby)
            {
                MelonCoroutines.Start(PatchCoroutines.WaitForEnemySyncGenerator(___spawnedObjects));
            }
        }
    }

    [HarmonyPatch(typeof(EnemySpawnerFromGenerator), nameof(EnemySpawnerFromGenerator.ClearAndDestroy))]
    class EnemyClearPatch
    {
        public static void Prefix(EnemySpawnerFromGenerator __instance, ref List<GameObject> ___spawnedObjects)
        {
            if (DiscordIntegration.hasLobby)
            {
                foreach (GameObject enemy in ___spawnedObjects)
                {
                    if (enemy.GetComponent<SyncedObject>())
                    {
                        SyncedObject syncedObject = enemy.GetComponent<SyncedObject>();
                        EnemyDestroyMessageData enemyDestroyMessageData = new EnemyDestroyMessageData()
                        {
                            userId = DiscordIntegration.currentUser.Id,
                            groupId = syncedObject.groupId
                        };

                        PacketByteBuf packetByteBuf =
                            MessageHandler.CompressMessage(NetworkMessageType.EnemyDestroyMessage,
                                enemyDestroyMessageData);

                        Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Grabbable), "AttemptGrab", new Type[]
    {
        typeof(Grabber),
        typeof(TestGrab.GrabPoint), typeof(ConfigurableJoint), typeof(GrabType)
    })]
    class GrabPatch
    {
        public static void Postfix(Grabbable __instance, Grabber grabber, TestGrab.GrabPoint grabPoint,
            ConfigurableJoint configurableJoint, GrabType grabType)
        {
            if (DiscordIntegration.hasLobby)
            {
                MelonCoroutines.Start(PatchCoroutines.WaitForProperGrab(__instance.gameObject));
            }
        }
    }

    [HarmonyPatch(typeof(Grabbable), "AttemptUngrab", new Type[] { typeof(Grabber) })]
    class UnGrabPatch
    {
        public static void Postfix(Grabbable __instance, Grabber grabber)
        {
            if (DiscordIntegration.hasLobby)
            {
                MelonCoroutines.Start(PatchCoroutines.WaitForProperUnGrab(__instance.gameObject));
            }
        }
    }

    [HarmonyPatch(typeof(Chamber), "FireProjectile")]
    class GunPatch
    {
        public static void Postfix(Chamber __instance)
        {
            if (DiscordIntegration.hasLobby)
            {
                if (!PatchVariables.shouldIgnoreFire)
                {
                    MelonCoroutines.Start(PatchCoroutines.WaitForGunshot(__instance.gameObject));
                }
            }
        }
    }

    [HarmonyPatch(typeof(Explodeable), "Explode")]
    class ExplosivesPatch
    {
        public static void Prefix(Explodeable __instance)
        {
            if (DiscordIntegration.hasLobby)
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
}