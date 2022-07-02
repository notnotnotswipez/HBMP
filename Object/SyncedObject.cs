using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FirearmSystem;
using HBMP.DataType;
using HBMP.Messages;
using HBMP.Messages.Handlers;
using HBMP.Nodes;
using MelonLoader;
using RootMotion.Dynamics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HBMP.Object
{
    public class SyncedObject : MonoBehaviour
    {
        public static Dictionary<ushort, SyncedObject> syncedObjectIds = new Dictionary<ushort, SyncedObject>();
        public static List<GameObject> syncedObjects = new List<GameObject>();
        public static Dictionary<ushort, List<SyncedObject>> relatedSyncedObjects =
            new Dictionary<ushort, List<SyncedObject>>();
        
        public Vector3 lastPos = new Vector3();
        public Vector3 lastPosition;
        public Quaternion lastRotation;
        public static ushort lastId = 0;
        public static ushort lastGroupId = 0;
        public long simulatorId = 0;
        public ushort currentId = 0;
        public ushort groupId = 0;

        public void BroadcastOwnerChange()
        {
            if (!IsClientSimulated())
            {
                MelonLogger.Msg("Transferred ownership of sync Id: "+currentId);
                long currentUserId = DiscordIntegration.currentUser.Id;

                SetOwner(currentUserId);
                OwnerChangeData ownerQueueChangeData = new OwnerChangeData()
                {
                    userId = currentUserId,
                    objectId = currentId
                };
                PacketByteBuf packetByteBuf = MessageHandler.CompressMessage(NetworkMessageType.OwnerChangeMessage,
                    ownerQueueChangeData);
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, packetByteBuf.getBytes());

                MelonLogger.Msg("Transferring ownership of whole group ID: "+groupId);
                if (!relatedSyncedObjects.ContainsKey(groupId))
                {
                    return;
                }
                MelonLogger.Msg("Related sync objects count of transferred ownership object is: "+relatedSyncedObjects[groupId].Count);
                foreach (SyncedObject relatedSync in relatedSyncedObjects[groupId])
                {
                    MelonLogger.Msg("Transferred ownership of related sync part: "+relatedSync.gameObject.name);
                    relatedSync.SetOwner(currentUserId);
                }
            }
        }

        public static void CleanData()
        {
            lastId = 0;
            lastGroupId = 0;
            syncedObjectIds.Clear();
            foreach (GameObject gameObject in syncedObjects)
            {
                SyncedObject syncedObject = gameObject.GetComponent<SyncedObject>();
                Destroy(syncedObject);
            }
            syncedObjects.Clear();
            relatedSyncedObjects.Clear();
        }

        public static void MakeSyncedObject(GameObject gameObject, ushort objectId, long ownerId, ushort groupId)
        {
            SyncedObject syncedObject = gameObject.AddComponent<SyncedObject>();
            // If the group ID coming in is greater than or equal to the one stored clientside, then we should set it.
            if (lastGroupId <= groupId)
            {
                lastGroupId = groupId;
                lastGroupId++;
            }
            
            if (relatedSyncedObjects.ContainsKey(groupId))
            {
                List<SyncedObject> otherSynced = relatedSyncedObjects[groupId];
                if (!otherSynced.Contains(syncedObject))
                {
                    otherSynced.Add(syncedObject);
                    MelonLogger.Msg("Added related sync in group ID: "+groupId);
                }
                relatedSyncedObjects[groupId] = otherSynced;
            }
            else
            {
                List<SyncedObject> otherSynced = new List<SyncedObject>();
                otherSynced.Add(syncedObject);
                relatedSyncedObjects.Add(groupId, otherSynced);
                MelonLogger.Msg("Added related sync in group ID: "+groupId);
            }

            syncedObjects.Add(gameObject);
            syncedObject.SetOwner(ownerId);
            syncedObject.currentId = objectId;
            MelonLogger.Msg("Made sync object for: "+gameObject.name+", with an ID of: "+objectId+", and group ID of: "+groupId);
            MelonLogger.Msg("Owner: "+ownerId);
            syncedObjectIds.Add(objectId, syncedObject);
        }

        public static void Sync(GameObject desiredSync)
        {
            if (!DiscordIntegration.hasLobby)
            {
                return;
            }
            
            if (desiredSync.GetComponent<SyncedObject>() != null)
            {
                return;
            }

            if (!desiredSync.GetComponent<Rigidbody>() || desiredSync.GetComponentsInChildren<Rigidbody>().Length == 0)
            {
                return;
            }

            ushort groupId = GetGroupId();
            
            if (isNPC(desiredSync))
            {
                foreach (Rigidbody rigidbody in GetProperRigidBodies(desiredSync.transform, true)) {
                    GameObject npcObj = rigidbody.gameObject;
                    BroadcastSyncData(npcObj, groupId);
                }
                return;
            }
            
            
            BroadcastSyncData(desiredSync, groupId);
        }

        private static void BroadcastSyncData(GameObject gameObject, ushort groupId)
        {
            String syncObject = GetGameObjectPath(gameObject);
            // We add this after, just incase someone else syncs and object that matches, this should never ever be looked for. Since its already synced.
            gameObject.name += "(Client synced First)";
            MelonLogger.Msg("Attempting to sync object, base path is: "+syncObject);
            ushort syncedId = GetObjectId();
            MelonLogger.Msg("Sync ID: "+syncedId);
            SyncedObject syncedObject = gameObject.AddComponent<SyncedObject>();
            if (relatedSyncedObjects.ContainsKey(groupId))
            {
                List<SyncedObject> otherSynced = relatedSyncedObjects[groupId];
                if (!otherSynced.Contains(syncedObject))
                {
                    otherSynced.Add(syncedObject);
                    MelonLogger.Msg("Added related sync in group ID: "+groupId);
                }
            }
            else
            {
                List<SyncedObject> otherSynced = new List<SyncedObject>();
                otherSynced.Add(syncedObject);
                relatedSyncedObjects.Add(groupId, otherSynced);
                MelonLogger.Msg("Added related sync in group ID: "+groupId);
            }
            syncedObject.currentId = syncedId;
            syncedObject.SetOwner(DiscordIntegration.currentUser.Id);
            syncedObjects.Add(gameObject);
            if (!syncedObjectIds.ContainsKey(syncedId))
            {
                syncedObjectIds.Add(syncedId, syncedObject);
            }
            else
            {
                return;
            }

            InitializeSyncData initializeSyncData = new InitializeSyncData()
            {
                userId = DiscordIntegration.currentUser.Id,
                objectId = syncedId,
                objectName = syncObject,
                groupId = groupId
            };
            
            PacketByteBuf packetByteBuf =
                MessageHandler.CompressMessage(NetworkMessageType.InitializeSyncMessage, initializeSyncData);
            
            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, packetByteBuf.getBytes());
        }

        private void OnOwnershipChange(bool owning)
        {
            if (owning)
            {
                Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
                if (rigidbody)
                {
                    if (!rigidbody.isKinematic)
                    {
                        rigidbody.isKinematic = false;
                    }
                }

                AnimatorBonesPinner animatorBonesPinner = gameObject.GetComponentInParent<AnimatorBonesPinner>();
                PuppetMaster puppetMaster = gameObject.GetComponentInParent<PuppetMaster>();
                if (animatorBonesPinner)
                {
                    Type bonesPinner = typeof(AnimatorBonesPinner);
                    FieldInfo isActive = bonesPinner.GetField("_active", BindingFlags.NonPublic | BindingFlags.Instance);
                    if ((bool)isActive.GetValue(animatorBonesPinner))
                    {
                        MelonLogger.Msg("Deactivated bones pinner on NPC");
                        isActive.SetValue(animatorBonesPinner,false);
                    }
                }

                if (puppetMaster)
                {
                    if (!puppetMaster.enabled)
                    {
                        puppetMaster.enabled = true;
                    }
                }
            }
            else
            {
                AnimatorBonesPinner animatorBonesPinner = gameObject.GetComponentInParent<AnimatorBonesPinner>();
                PuppetMaster puppetMaster = gameObject.GetComponentInParent<PuppetMaster>();
                if (animatorBonesPinner)
                {
                    Type bonesPinner = typeof(AnimatorBonesPinner);
                    FieldInfo isActive = bonesPinner.GetField("_active", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (!(bool)isActive.GetValue(animatorBonesPinner))
                    {
                        MelonLogger.Msg("Activated bones pinner on NPC");
                        isActive.SetValue(animatorBonesPinner,true);
                    }
                }
                if (puppetMaster)
                {
                    if (puppetMaster.enabled)
                    {
                        puppetMaster.enabled = false;
                    }
                }
            }
        }

        public static GameObject GetInteractiveEntityInScene(String name)
        {
            String[] split = name.Split('/');
            String properName = split[split.Length-1].Replace("(Clone)", "").Replace("(Made copy)", "");

            // Probably gun
            if (!properName.Contains("Enemy"))
            {
                foreach (var interactiveObject in GameObject.FindObjectsOfType<InteractiveEntity>())
                {
                    GameObject gameObject = interactiveObject.gameObject;
                    // Instantiated copy (We can clone this), we dont want to clone already synced objects as those come with their own ids
                    if (gameObject.name.Replace("(Clone)", "").Replace("(Made copy)", "").Equals(properName) && !gameObject.GetComponent<SyncedObject>())
                    {
                        return gameObject;
                    }
                }
            }
            else
            {
                foreach (var interactiveObject in GameObject.FindObjectsOfType<EnemyRoot>())
                {
                    GameObject gameObject = interactiveObject.gameObject;
                    // Instantiated copy (We can clone this), non synced NPCs will be hard to come by but still.
                    if (gameObject.name.Replace("(Clone)", "").Replace("(Made copy)", "").Equals(properName) && !gameObject.GetComponent<SyncedObject>())
                    {
                        return gameObject;
                    }
                }
            }

            return null;
        }

        public static Rigidbody[] GetProperRigidBodies(Transform transform, bool checkForNpc)
        {
            MelonLogger.Msg("Getting all rigidbodies for: "+transform.gameObject.name);
            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            

            Transform ultimateParent = transform;
            if (checkForNpc)
            {
                EnemyRoot foundBody = transform.GetComponentInParent<EnemyRoot>();
                if (!foundBody)
                {
                    foundBody = transform.GetComponent<EnemyRoot>();
                    if (!foundBody)
                    {
                        transform.GetComponentInChildren<EnemyRoot>();
                    }
                }
                
                if (foundBody)
                {
                    ultimateParent = foundBody.gameObject.transform;
                }
            }
            else
            {
                MelonLogger.Msg("Is not an npc");
                Transform parentWithInteractive = transform;
                while (isPartOfBiggerObject(parentWithInteractive))
                {
                    MelonLogger.Msg(parentWithInteractive.gameObject.name+" Is part of a bigger object.");
                    parentWithInteractive = parentWithInteractive.parent;
                }

                MelonLogger.Msg("Going with: "+parentWithInteractive.gameObject.name);
                ultimateParent = parentWithInteractive;
            }

            Rigidbody baseRigidBody = ultimateParent.transform.gameObject.GetComponent<Rigidbody>();
            
            if (baseRigidBody)
            {
                rigidbodies.Add(baseRigidBody);
            }

            foreach (Rigidbody rigidbody in ultimateParent.transform.gameObject.GetComponentsInChildren<Rigidbody>()) {
                if (!rigidbodies.Contains(rigidbody))
                {
                    rigidbodies.Add(rigidbody);
                }
            }

            return rigidbodies.ToArray();
        }

        private static bool isPartOfBiggerObject(Transform transform)
        {
            if (transform.parent.gameObject.GetComponent<ColliderHighlighter>() || transform.parent.gameObject.GetComponent<Rigidbody>())
            {
                return true;
            }

            return false;
        }

        public static bool isNPC(GameObject gameObject)
        {
            return GetGameObjectPath(gameObject).Contains("[BODY]");
        }

        protected void UpdateStoredPositions() {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }
        
        void OnCollisionEnter(Collision collision)
        {
            if (!IsClientSimulated())
            {
                return;
            }

            if (collision.gameObject.GetComponent<InteractiveEntity>())
            {
                Sync(collision.gameObject);
            }
        }
        
        public bool HasChangedPositions() => (transform.position - lastPosition).sqrMagnitude > 0.0005f || Quaternion.Angle(transform.rotation, lastRotation) > 0.05f;

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }

            return path;
        }

        public void SetOwner(long userId)
        {
            simulatorId = userId;
            if (IsClientSimulated())
            {
                OnOwnershipChange(true); 
            }
        }

        public bool IsClientSimulated()
        {
            return simulatorId == DiscordIntegration.currentUser.Id;
        }

        public static SyncedObject GetSyncedObject(ushort objectId)
        {
            if (syncedObjectIds.ContainsKey(objectId))
            {
                return syncedObjectIds[objectId];
            }

            return null;
        }

        public void UpdateObject(SimplifiedTransform simplifiedTransform)
        {
            if (!IsClientSimulated())
            {
                OnOwnershipChange(false);

                Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
                if (rigidbody)
                {
                    rigidbody.velocity = new Vector3(0, 0, 0 );
                    rigidbody.isKinematic = true;
                }

                gameObject.transform.position = simplifiedTransform.position;
                gameObject.transform.eulerAngles = simplifiedTransform.rotation.ExpandQuat().eulerAngles;
            }
            else
            {
                OnOwnershipChange(true);
            }
        }

        public void FixedUpdate()
        {
            if (!DiscordIntegration.hasLobby)
            {
                return;
            }

            bool shouldSendUpdate = HasChangedPositions() || isNPC(gameObject);
            if (IsClientSimulated() && HasChangedPositions())
            {
                SimplifiedTransform simplifiedTransform =
                    new SimplifiedTransform(gameObject.transform.position, Quaternion.Euler(gameObject.transform.eulerAngles));
                
                TransformUpdateData transformUpdateData = new TransformUpdateData()
                {
                    objectId = currentId,
                    userId = DiscordIntegration.currentUser.Id,
                    sTransform = simplifiedTransform
                };
                
                PacketByteBuf packetByteBuf =
                    MessageHandler.CompressMessage(NetworkMessageType.TransformUpdateMessage, transformUpdateData);
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Unreliable, packetByteBuf.getBytes());
            }

            UpdateStoredPositions();
        }

        public static ushort GetObjectId()
        {
            return lastId++;
        }
        
        public static ushort GetGroupId()
        {
            return lastGroupId++;
        }
    }
}