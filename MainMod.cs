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
using Node = HBMP.Nodes.Node;

namespace HBMP
{
    public class MainMod : MelonMod
    {
        private PlayerRepresentation debugRepresentation;

        public static Dictionary<byte, GameObject> boneDictionary = new Dictionary<byte, GameObject>();
        private byte currentBoneIndex = 0;
        public override void OnApplicationStart()
        {
            GameSDK.LoadGameSDK();
            MessageHandler.RegisterHandlers();
            DiscordIntegration.Init();
            Client.StartClient();
            DataDirectory.Initialize();
            VrMenuPageBuilder builder = VrMenuPageBuilder.Builder();

            builder.AddButton(new VrMenuButton("Start Server", () =>
            {
                Server.StartServer();
            }, Color.green
                ));

            builder.AddButton(new VrMenuButton("End Server/Disconnect", () => 
            {
                if (DiscordIntegration.hasLobby && DiscordIntegration.isHost)
                {
                    Server.instance.Shutdown();
                }
                else if (DiscordIntegration.isConnected)
                {
                    Client.instance.DisconnectFromServer();
                }
            }, Color.red
                ));

            VrMenuPage page = builder.Build();

            VrMenu.RegisterMainButton(new VrMenuButton("HBMP", () =>
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
            
            if (Server.instance != null)
            {
                PacketByteBuf message = MessageHandler.CompressMessage(NetworkMessageType.SceneTransferMessage, new SceneTransferData()
                {
                    sceneIndex = buildIndex
                });
                
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, message.getBytes());
            }
        }
        
        public override void OnFixedUpdate()
        {
            if (DiscordIntegration.hasLobby)
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
                    userId =  DiscordIntegration.currentUser.Id,
                    simplifiedTransforms = simplifiedTransformsArray
                });
                
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Unreliable, message.getBytes());
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
                    userId =  DiscordIntegration.currentUser.Id,
                    boneIndex = boneId,
                    simplifiedTransform = simplifiedTransform
                });
                
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Unreliable, message.getBytes());
            }
        }

        public override void OnApplicationQuit()
        {
            if (Server.instance != null)
            {
                Server.instance.Shutdown();
            }
            else
            {
                if (DiscordIntegration.hasLobby)
                {
                    if (Client.instance != null)
                    {
                        Client.instance.Shutdown();
                    }
                }
            }
            base.OnApplicationQuit();
        }

        public override void OnLateUpdate() {
            DiscordIntegration.Tick();
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                if (Server.instance == null)
                {
                    Server.StartServer();
                }
            }
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
