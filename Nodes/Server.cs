using System;
using System.Collections.Generic;
using Discord;
using HBMF;
using HBMP.Messages;
using HBMP.Messages.Handlers;
using HBMP.Object;
using HBMP.Representations;
using MelonLoader;
using UnityEngine.SceneManagement;

namespace HBMP.Nodes
{
    public class Server : Node
    {
        public static Server instance;
        
        public static void StartServer()
        {
            if (instance != null)
                throw new Exception("Can't create another client instance!");
            activeNode = instance = new Server();
        }


        public Server()
        {
            MakeLobby();
        }
        
        public override void UserConnectedEvent(long lobbyId, long userId) {
            DiscordIntegration.activity.Party.Size.CurrentSize = 1 + connectedUsers.Count;
            DiscordIntegration.activityManager.UpdateActivity(DiscordIntegration.activity, (res) => { });

            foreach (KeyValuePair<byte, long> valuePair in DiscordIntegration.byteIds) {
                if (valuePair.Value == userId) continue;

                ShortIdMessageData addMessageData = new ShortIdMessageData()
                {
                    userId = valuePair.Value,
                    byteId = valuePair.Key,
                };
                PacketByteBuf packetByteBuf = MessageHandler.CompressMessage(NetworkMessageType.ShortIdUpdateMessage, addMessageData);
                BroadcastMessage((byte)NetworkChannel.Reliable, packetByteBuf.getBytes());
            }

            ShortIdMessageData idMessageData = new ShortIdMessageData() {
                userId = userId,
                byteId = DiscordIntegration.RegisterUser(userId)
            };
            PacketByteBuf secondBuff = MessageHandler.CompressMessage(NetworkMessageType.ShortIdUpdateMessage, idMessageData);
            BroadcastMessage((byte)NetworkChannel.Reliable, secondBuff.getBytes());
            
            JoinCatchupData joinCatchupData = new JoinCatchupData() {
                lastId = SyncedObject.lastId,
                lastGroupId = SyncedObject.lastGroupId
            };
            PacketByteBuf catchupBuff = MessageHandler.CompressMessage(NetworkMessageType.JoinCatchupMessage, joinCatchupData);
            SendMessage(userId, (byte)NetworkChannel.Reliable, catchupBuff.getBytes());
            
            PacketByteBuf message = MessageHandler.CompressMessage(NetworkMessageType.SceneTransferMessage, new SceneTransferData()
            {
                sceneIndex = SceneManager.GetActiveScene().buildIndex
            });
                
            SendMessage(userId, (byte)NetworkChannel.Reliable, message.getBytes());
        }

        private void MakeLobby()
        {
            LobbyTransaction lobbyTransaction = DiscordIntegration.lobbyManager.GetLobbyCreateTransaction();
            lobbyTransaction.SetCapacity(10);
            lobbyTransaction.SetLocked(false);
            lobbyTransaction.SetType(LobbyType.Private);
            DiscordIntegration.lobbyManager.CreateLobby(lobbyTransaction, onDiscordLobbyCreate);
        }

        private void onDiscordLobbyCreate(Result result, ref Lobby lobby)
        {
            if (result != Result.Ok)
            {
                return;
            }

            DiscordIntegration.lobby = lobby;
            
            DiscordIntegration.activity.Party = new ActivityParty()
            {
                Id = lobby.Id.ToString(),
                Size = new PartySize() { CurrentSize = 1, MaxSize = 10 }
            };
            DiscordIntegration.activity.Details = "This user is hosting a HBMP server!";
            DiscordIntegration.activity.State = "Killing with friends";
            DiscordIntegration.activity.Secrets = new ActivitySecrets()
            {
                Join = DiscordIntegration.lobbyManager.GetLobbyActivitySecret(lobby.Id)
            };
            DiscordIntegration.UpdateActivity();
            
            ConnectToDiscordServer();

            DiscordIntegration.lobbyManager.OnNetworkMessage += OnDiscordMessageRecieved;
            DiscordIntegration.lobbyManager.OnMemberConnect += OnDiscordUserJoined;
            DiscordIntegration.lobbyManager.OnMemberDisconnect += OnDiscordUserLeft;
        }
        
        public void BroadcastMessageExcept(byte channel, byte[] data, long toIgnore) => connectedUsers.ForEach((user) => { 
            if (user != toIgnore) { 
                SendMessage(user, channel, data); 
            } 
        });

        public override void BroadcastMessage(byte channel, byte[] data) => BroadcastMessageP2P(channel, data);
        
        public void CloseLobby() {
            foreach (byte byteId in DiscordIntegration.byteIds.Keys)
            {
                DisconnectMessageData disconnectMessageData = new DisconnectMessageData()
                {
                    userId = DiscordIntegration.GetLongId(byteId)
                };

                PacketByteBuf packetByteBuf =
                    MessageHandler.CompressMessage(NetworkMessageType.DisconnectMessage, disconnectMessageData);
                
                instance.BroadcastMessage((byte)NetworkChannel.Reliable, packetByteBuf.getBytes());
            }
            DiscordIntegration.Tick();
            DiscordIntegration.lobbyManager.DeleteLobby(DiscordIntegration.lobby.Id, (result) => { });
            DiscordIntegration.lobby = new Lobby();

            CleanData();
        }

        public override void UserDisconnectEvent(long lobbyId, long userId) {
            DiscordIntegration.activity.Party.Size.CurrentSize = 1 + connectedUsers.Count;
            DiscordIntegration.activityManager.UpdateActivity(DiscordIntegration.activity, (res) => { });
        }
        
        public override void Shutdown() {
            if (DiscordIntegration.hasLobby && !DiscordIntegration.isHost) {
                MelonLogger.Msg("Unable to close the server as a client!");
                return;
            }

            CloseLobby();
            DiscordIntegration.DefaultRichPresence();

            instance = null;
            activeNode = Client.instance;
        }
    }
}