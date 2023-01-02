using System;
using System.Collections.Generic;
using System.IO;
using BulletMenuVR;
using FirearmSystem;
using HarmonyLib;
using HBMP.DataType;
using HBMP.Messages;
using HBMP.Messages.Handlers;
using HBMP.Nodes;
using HBMP.Object;
using HBMP.Representations;
using HBMP.Utils;
using InteractionSystem;
using MelonLoader;
using NodeCanvas.Framework;
using UnityEngine;
using UnityEngine.Networking;

namespace HBMP
{
    public class MainMod : MelonMod
    {
        private PlayerRepresentation debugRepresentation;

        public static Dictionary<byte, GameObject> boneDictionary = new Dictionary<byte, GameObject>();
        private byte currentBoneIndex = 0;

        public override void OnApplicationStart()
        {
            MessageHandler.RegisterHandlers();
            SteamManager.Init();
            DataDirectory.Initialize();
            VrMenuPageBuilder builder = VrMenuPageBuilder.Builder();

            builder.AddButton(new VrMenuButton("Start Server", () =>
            {
                CreateFriendLobby();
            }, Color.green
                ));

            builder.AddButton(new VrMenuButton("End Server/Disconnect", () => 
            {
                if (SteamManager.Instance.isConnectedToLobby && SteamManager.Instance.isHost)
                {
                    SteamManager.Disconnect(false);
                }
                else if (SteamManager.Instance.isConnectedToLobby && !SteamManager.Instance.isHost)
                {
                    SteamManager.Disconnect(false);
                }
            }, Color.red
                ));

            VrMenuPage page = builder.Build();

            VrMenu.RegisterMainButton(new VrMenuButton("HBMPSteamNetwork", () =>
                {
                    page.Open();
                }, Color.blue
            ));
        }
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            currentBoneIndex = 0;
            boneDictionary.Clear();
            boneDictionary.Add(currentBoneIndex++, GameObject.Find("[HARD BULLET PLAYER]/HexaBody/PlayerModel/PlayerModel/root"));
            populateBoneDictionary(GameObject.Find("[HARD BULLET PLAYER]/HexaBody/PlayerModel/PlayerModel/root").transform);
            Mod.Start();
            SyncedObject.CleanData();
            SyncedObject.CloneAllWeapons();
            
            if (SteamManager.Instance.isHost)
            {
                PacketByteBuf message = MessageHandler.CompressMessage(NetworkMessageType.SceneTransferMessage, new SceneTransferData()
                {
                    sceneIndex = buildIndex
                });
                
                SteamPacketNode.BroadcastMessage(NetworkChannel.Reliable, message);
            }
        }
        
        public override void OnFixedUpdate()
        {
            if (SteamManager.Instance.isConnectedToLobby)
            {
                foreach (EnemyRoot spawnedRoot in SyncedObject.spawnedEnemies)
                {
                    spawnedRoot.gameObject.GetComponentInChildren<Blackboard>().SetVariableValue("Target", HBMF.r.playerloc);
                }

                Transform rightHand = GameObject
                    .Find("[HARD BULLET PLAYER]/HexaBody/RightArm/Hand/HVR_CustomHandRight Variant").transform;
                Transform leftHand = GameObject
                    .Find("[HARD BULLET PLAYER]/HexaBody/LeftArm/Hand/HVR_CustomHandLeft Variant").transform;
                Transform head = GameObject.Find("[HARD BULLET PLAYER]/HexaBody/Pelvis/CameraRig/FloorOffset/Scaler/Camera")
                    .transform;

                SimplifiedTransform[] simplifiedTransformsArray = new SimplifiedTransform[3];
                var headTransform = head;
                simplifiedTransformsArray[0] = (new SimplifiedTransform(headTransform.position, Quaternion.Euler(headTransform.eulerAngles)));
                var rightHandTransform = rightHand;
                simplifiedTransformsArray[1] = (new SimplifiedTransform(rightHandTransform.position, Quaternion.Euler(rightHandTransform.eulerAngles)));
                var leftHandTransform = leftHand;
                simplifiedTransformsArray[2] = (new SimplifiedTransform(leftHandTransform.position, Quaternion.Euler(leftHandTransform.eulerAngles)));

                if (debugRepresentation != null)
                {
                    debugRepresentation.UpdateTransforms(simplifiedTransformsArray);
                }
                
                sendBones();

                PacketByteBuf message = MessageHandler.CompressMessage(NetworkMessageType.PlayerUpdateMessage, new PlayerSyncMessageData()
                {
                    userId =  SteamManager.currentId,
                    simplifiedTransforms = simplifiedTransformsArray
                });
                
                SteamPacketNode.BroadcastMessage(NetworkChannel.Unreliable, message);
            }
        }
        
        private void populateBoneDictionary(Transform parent)
        {
            int childCount = parent.childCount;

            for (int i = 0; i < childCount; i++)
            {
                GameObject child = parent.GetChild(i).gameObject;
                boneDictionary.Add(currentBoneIndex++, child);

                if (child.transform.childCount > 0 && !Blacklist.isBlockedBone(child))
                {
                    populateBoneDictionary(child.transform);
                }
            }
        }

        private void sendBones()
        {
            foreach (byte boneId in boneDictionary.Keys)
            {
                GameObject bone = boneDictionary[boneId];
                SimplifiedTransform simplifiedTransform = new SimplifiedTransform(bone.transform.position,
                    Quaternion.Euler(bone.transform.eulerAngles));
                
                PacketByteBuf message = MessageHandler.CompressMessage(NetworkMessageType.IkUpdateMessage, new IkSyncMessageData()
                {
                    userId =  SteamManager.currentId,
                    boneIndex = boneId,
                    simplifiedTransform = simplifiedTransform
                });
                
                SteamPacketNode.BroadcastMessage(NetworkChannel.Unreliable, message);
            }
        }

        public override void OnApplicationQuit()
        {
            SteamManager.Disconnect(true);
            base.OnApplicationQuit();
        }

        public override void OnLateUpdate() {
            SteamPacketNode.Flush();
            SteamPacketNode.Callbacks();
        }

        public override void OnUpdate()
        {
            if (SteamManager.Instance != null)
            {
                SteamManager.Instance.Update();
                if (Input.GetKeyDown(KeyCode.H))
                {
                    if (SteamManager.Instance.isConnectedToLobby == false)
                    {
                        CreateFriendLobby();
                    }
                }
            }
        }

        private async void CreateFriendLobby()
        {
            await SteamManager.Instance.CreateFriendLobby();
        }
    }

    public class Mod : MonoBehaviour
    {
        private static string id;
        public static GameObject player;

        public static String[] versionString = new[] { "1", "4", "0" };

        public static void Start()
        {
            AssetBundle localAssetBundle = AssetBundle.LoadFromMemory(EmbeddedAssetBundle.LoadFromAssembly(System.Reflection.Assembly.GetExecutingAssembly(), "HBMP.Resources.player.hbmp"));
            player = localAssetBundle.LoadAsset<GameObject>("1_Player.prefab");
            localAssetBundle.Unload(false);
        }

        public static string GetVersionString()
        {
            return versionString[0]+"."+versionString[1]+"."+versionString[2];
        }
    }
}
