using System;
using System.Collections.Generic;
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
        public static GameObject npcPrefab;
        public static Vector3 tpPos;

        private PlayerRepresentation debugRepresentation;
        public override void OnApplicationStart()
        {
            MessageHandler.RegisterHandlers();
            DiscordIntegration.Init();
            Client.StartClient();
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
            Mod.Start();
            SyncedObject.CleanData();
            
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

                PacketByteBuf message = MessageHandler.CompressMessage(NetworkMessageType.PlayerUpdateMessage, new PlayerSyncMessageData()
                {
                    userId =  DiscordIntegration.currentUser.Id,
                    simplifiedTransforms = simplifiedTransformsArray
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
            // This will update and flush discords callbacks
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

            if (Input.GetKeyDown(KeyCode.P))
            {
                tpPos = GameObject.Find("[HARD BULLET PLAYER]/HexaBody/Pelvis/CameraRig/FloorOffset/Scaler/Camera").transform.position;
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                PlayerUtils.TeleportPlayer(tpPos);
            }
        }
    }

    public class Mod : MonoBehaviour
    {
        private static string id;
        public static GameObject player;

        public static String[] versionString = new[] { "1", "1", "5" };

        public static void Start()
        {
            AssetBundle localAssetBundle = AssetBundle.LoadFromFile(MelonUtils.UserDataDirectory+"/HBMP/player.hbmp");
            player = localAssetBundle.LoadAsset<GameObject>("1_Player.prefab");
            localAssetBundle.Unload(false);
        }

        public static string GetVersionString()
        {
            return versionString[0]+"."+versionString[1]+"."+versionString[2];
        }
    }
}
