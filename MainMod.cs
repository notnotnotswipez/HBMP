using System;
using System.Collections.Generic;
using FirearmSystem;
using HarmonyLib;
using HBMP.DataType;
using HBMP.Messages;
using HBMP.Messages.Handlers;
using HBMP.Nodes;
using HBMP.Object;
using HBMP.Representations;
using InteractionSystem;
using MelonLoader;
using UnityEngine;
using UnityEngine.Networking;

namespace HBMP
{
    public class MainMod : MelonMod
    {
        private PlayerRepresentation debugRepresentation;
        public override void OnApplicationStart()
        {
            MessageHandler.RegisterHandlers();
            DiscordIntegration.Init();
            Client.StartClient();
        }
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            Mod.Start();
            SyncedObject.CleanData();
        }
        
        public override void OnFixedUpdate()
        {
            if (DiscordIntegration.hasLobby)
            {
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
            DiscordIntegration.Flush();
        }
        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                Server.StartServer();
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                debugRepresentation = new PlayerRepresentation(DiscordIntegration.currentUser);
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                if (DiscordIntegration.hasLobby && DiscordIntegration.isHost)
                {
                    Server.instance.Shutdown();
                }
                else if (DiscordIntegration.isConnected)
                {
                    Client.instance.DisconnectFromServer();
                }
            }
            DiscordIntegration.Update();
        }
    }

    public class Mod : MonoBehaviour
    {
        private static List<GameObject> players;
        private static string id;
        private static bool connected = false;
        public static GameObject player;

        public static String[] versionString = new[] { "0", "7", "0" };

        public static void Start()
        {
            players = new List<GameObject>();
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
