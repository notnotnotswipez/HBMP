using System.Collections.Generic;
using HBMP.Nodes;
using HBMP.Object;
using InteractionSystem;
using MelonLoader;
using Steamworks;
using UnityEngine;

namespace HBMP.Messages.Handlers
{
    public class InitializeSyncMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            InitializeSyncData initializeSyncData = (InitializeSyncData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(initializeSyncData.userId));
            packetByteBuf.WriteUShort(initializeSyncData.objectId);
            packetByteBuf.WriteUShort(initializeSyncData.groupId);
            packetByteBuf.WriteString(initializeSyncData.objectName);
            packetByteBuf.create();
         
            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            ushort objectId = packetByteBuf.ReadUShort();
            ushort groupId = packetByteBuf.ReadUShort();
            string objectName = packetByteBuf.ReadString();
            
            MelonLogger.Msg("Received sync request for: "+objectName);
            
            if (!SyncedObject.syncedObjectIds.ContainsKey(objectId))
            {
                SyncedObject.lastId = objectId;
                SyncedObject.lastId++;
                GameObject foundCopy = GameObject.Find(objectName);
                if (!foundCopy)
                {
                    MelonLogger.Error("Cound not find object: "+objectName);
                    MelonLogger.Msg("Searching scene for a: "+objectName);
                    GameObject potentialPrefab = SyncedObject.GetInteractiveEntityInScene(objectName);
                    if (!potentialPrefab)
                    {
                        MelonLogger.Error("Couldnt find "+objectName+" or any instanciable objects in the scene. Sorry! Probably a magazine");
                        return;
                    }
                    
                    potentialPrefab.SetActive(true);

                    // Check for socket
                    Socket socket = potentialPrefab.GetComponentInParent<Socket>();
                    if (socket)
                    {
                        // Fixes item
                        socket.ForceUnsnapObjectFromSocket();
                    }

                    GameObject copied = GameObject.Instantiate(potentialPrefab);
                    GameObject.Destroy(potentialPrefab);
                    copied.name += "(Made copy)";
                    SyncedObject.MakeSyncedObject(copied, objectId, userId, groupId);
                    return;
                }
                SyncedObject potentialAlreadySyncedObject = foundCopy.GetComponent<SyncedObject>();
                if (potentialAlreadySyncedObject)
                {
                    MelonLogger.Msg("Object with id of: "+objectId+" does not exist, however, the related object:"+foundCopy.name
                    +", does exist and is synced already, must make a clone to correct the mismatch.");
                    GameObject potentialPrefab = SyncedObject.GetInteractiveEntityInScene(objectName);
                    if (!potentialPrefab)
                    {
                        MelonLogger.Error("Couldnt find "+objectName+" or any instanciable objects in the scene. Sorry! Probably a magazine");
                        return;
                    }
                    
                    potentialPrefab.SetActive(true);
                    
                    // Check for socket
                    Socket socket = potentialPrefab.GetComponentInParent<Socket>();
                    if (socket)
                    {
                        // Fixes item
                        socket.ForceUnsnapObjectFromSocket();
                    }

                    GameObject copied = GameObject.Instantiate(potentialPrefab);
                    GameObject.Destroy(potentialPrefab);
                    copied.name += "(Made copy)";
                    SyncedObject.MakeSyncedObject(copied, objectId, userId, groupId);
                    return;
                }
                
                SyncedObject.MakeSyncedObject(foundCopy, objectId, userId, groupId);
                
            }
            else
            {
                MelonLogger.Error("Received request to sync object with id: "+objectId+", but that slot is already taken up!");
            }
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new System.NotImplementedException();
        }
    }

    public class InitializeSyncData : MessageData
    {
        public string objectName;
        public ushort objectId;
        public SteamId userId;
        public ushort groupId;
    }
}