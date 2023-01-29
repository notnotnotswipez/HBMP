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
using HBMP.Utils;
using InteractionSystem;
using MelonLoader;
using Steamworks;
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
                    userId = SteamIntegration.currentId,
                    objectId = syncedObject.currentId
                };

                PacketByteBuf packetByteBuf =
                    PacketHandler.CompressMessage(PacketType.GunshotMessage, gunshotMessageData);

                SteamPacketNode.BroadcastMessage(NetworkChannel.Attack, packetByteBuf.getBytes());
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
                        SteamId ownerId = SyncedObject.returnedEnemyRoots[enemyRoot];
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

        public static IEnumerator WaitForEnemyReSync(SyncedObject syncedObject, SteamId userId)
        {
            yield return new WaitForNotMoving(syncedObject);
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
            enemyRoot.Blackboard.SetVariableValue("Target", PlayerUtils.GetRandomPlayerHead());
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
                    userId = SteamIntegration.currentId,
                    groupId = groupId
                };

                PacketByteBuf packetByteBuf =
                    PacketHandler.CompressMessage(PacketType.EnemyDestroyMessage, enemyDestroyMessageData);

                SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
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
        public static void Postfix(EntitySpawner __instance, GameObject entity, EntitySpawnProperties properties)
        {
            if (SteamIntegration.hasLobby)
            {
                MelonLogger.Msg("Created entity.");
                MelonLogger.Msg("Spawned entity from entity spawner.");
                MelonCoroutines.Start(PatchCoroutines.WaitForEnemySyncSingular(entity));
            }
        }
    }

    [HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.ReloadCurrentScene))]
    class ReloadScenePatch
    {
        public static bool Prefix(SceneLoader __instance)
        {
            if (SteamIntegration.hasLobby)
            {
                if (!SteamIntegration.isHost)
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
            if (SteamIntegration.hasLobby)
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
            if (SteamIntegration.hasLobby)
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
            if (SteamIntegration.hasLobby)
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
            if (SteamIntegration.hasLobby)
            {
                foreach (GameObject enemy in ___spawnedObjects)
                {
                    if (enemy.GetComponent<SyncedObject>())
                    {
                        SyncedObject syncedObject = enemy.GetComponent<SyncedObject>();
                        EnemyDestroyMessageData enemyDestroyMessageData = new EnemyDestroyMessageData()
                        {
                            userId = SteamIntegration.currentId,
                            groupId = syncedObject.groupId
                        };

                        PacketByteBuf packetByteBuf =
                            PacketHandler.CompressMessage(PacketType.EnemyDestroyMessage,
                                enemyDestroyMessageData);

                        SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Grabbable), "AttemptGrab")]
    class GrabPatch
    {
        public static void Postfix(Grabbable __instance)
        {
            if (SteamIntegration.hasLobby)
            {
                MelonCoroutines.Start(PatchCoroutines.WaitForProperGrab(__instance.gameObject));
            }
        }
    }

    [HarmonyPatch(typeof(Grabbable), "AttemptUngrab")]
    class UnGrabPatch
    {
        public static void Postfix(Grabbable __instance)
        {
            if (SteamIntegration.hasLobby)
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
            if (SteamIntegration.hasLobby)
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
            if (SteamIntegration.hasLobby)
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
                        userId = SteamIntegration.currentId,
                        objectId = syncedObject.currentId
                    };

                    PacketByteBuf packetByteBuf =
                        PacketHandler.CompressMessage(PacketType.ExplodeMessage, explodeMessageData);

                    SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
                }
            }
        }
    }
}