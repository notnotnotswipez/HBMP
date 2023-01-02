using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBMP.Extensions;
using HBMP.Messages;
using HBMP.Messages.Handlers;
using HBMP.Nodes;
using HBMP.Object;
using HBMP.Representations;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HBMP
{
    public class SteamManager
    {
        public static SteamManager Instance;
        public static uint gameAppId = 1294760;

        public string currentName { get; set; }
        public static SteamId currentId { get; set; }
        private string playerSteamIdString;

        public bool isConnectedToLobby = false;

        private string ownerIdIdentifier = "ownerId";

        public string PlayerSteamIdString
        {
            get => playerSteamIdString;
        }

        private bool connectedToSteam = false;

        public static List<SteamId> connectedIds = new List<SteamId>();
        public static Dictionary<SteamId, Friend> userData = new Dictionary<SteamId, Friend>();
        public static Dictionary<byte, ulong> byteIds = new Dictionary<byte, ulong>();
        public static Dictionary<NetworkChannel, P2PSend> networkChannels = new Dictionary<NetworkChannel, P2PSend>();

        public Lobby currentLobby;
        private Lobby hostedMultiplayerLobby;

        public bool isHost = false;

        private bool applicationHasQuit = false;

        public static byte localByteId = 0;
        public static byte lastByteId = 1;

        public SteamManager()
        {
            Instance = this;
            currentName = "";
            // Create client
            SteamClient.Init(gameAppId, true);

            if (!SteamClient.IsValid)
            {
                MelonLogger.Msg("Steam client not valid");
                throw new Exception();
            }

            currentName = SteamClient.Name;
            currentId = SteamClient.SteamId;
            playerSteamIdString = currentId.ToString();
            connectedToSteam = true;
            MelonLogger.Msg("Steam initialized: " + currentName);
            OpenNetworkChannels();
            MelonLogger.Msg("Opened network channels");
            Instance.Start();
        }

        public static void RegisterUser(byte byteId, ulong longId)
        {
            if (!byteIds.ContainsKey(byteId))
            {
                byteIds.Add(byteId, longId);
            }
        }

        public static SteamId GetSteamId(ulong numerical)
        {
            foreach (var connected in connectedIds)
            {
                if (connected.Value == numerical)
                {
                    return connected;
                }
            }

            return new SteamId();
        }

        public static void Init()
        {
            if (Instance == null)
            {
                SteamManager steamManager = new SteamManager();
            }
        }

        public static void Disconnect(bool fullyShutDown)
        {
            Instance.isConnectedToLobby = false;
            Instance.isHost = false;
            Instance.CleanData();
            Instance.leaveLobby();
            if (fullyShutDown)
            {
                SteamClient.Shutdown();
            }
        }

        public void OpenNetworkChannels()
        {
            networkChannels.Add(NetworkChannel.Unreliable, P2PSend.UnreliableNoDelay);
            networkChannels.Add(NetworkChannel.Reliable, P2PSend.Reliable);
            networkChannels.Add(NetworkChannel.Object, P2PSend.Reliable);
            networkChannels.Add(NetworkChannel.Attack, P2PSend.Reliable);
            networkChannels.Add(NetworkChannel.Transaction, P2PSend.Reliable);
        }

        public void CleanData()
        {
            foreach (PlayerRepresentation rep in PlayerRepresentation.representations.Values)  {
                GameObject.Destroy(rep.playerRep);
            }
            connectedIds.Clear();
            PlayerRepresentation.representations.Clear();
            SyncedObject.CleanData();
            byteIds.Clear();
            userData.Clear();
            lastByteId = 0;
        }

        public bool ConnectedToSteam()
        {
            return connectedToSteam;
        }

        void Start()
        {
            MelonLogger.Msg("Running start method.");
            // Callbacks
            SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedCallback;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoinedCallback;
            SteamMatchmaking.OnChatMessage += OnChatMessageCallback;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnectedCallback;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeaveCallback;
            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequestedCallback;

            MelonLogger.Msg("Finished registering start method.");
            
            UpdateRichPresenceStatus();
            
            MelonLogger.Msg("Updated rich presence.");
        }

        public void Update()
        {
            SteamClient.RunCallbacks();
        }
        
        void OnLobbyMemberDisconnectedCallback(Lobby lobby, Friend friend)
        {
            OtherLobbyMemberLeft(friend);
        }

        void OnLobbyMemberLeaveCallback(Lobby lobby, Friend friend)
        {
            OtherLobbyMemberLeft(friend);
        }

        private void OtherLobbyMemberLeft(Friend friend)
        {
            if (friend.Id != currentId)
            {
                MelonLogger.Msg("Opponent has left the lobby");
                
                try
                {
                    PlayerRepresentation playerRepresentation = PlayerRepresentation.representations[friend.Id];
                    playerRepresentation.DeleteRepresentation();
                    connectedIds.Remove(friend.Id);
                    userData.Remove(friend.Id);
                    byteIds.Remove(GetByteId(friend.Id));
                    SteamNetworking.CloseP2PSessionWithUser(friend.Id);
                    // Handle game / UI changes that need to happen when other player leaves
                }
                catch
                {
                    MelonLogger.Msg("Unable to update disconnected player nameplate / process disconnect cleanly");
                }
            }
        }

        void OnLobbyGameCreatedCallback(Lobby lobby, uint ip, ushort port, SteamId steamId)
        {
            MelonLogger.Msg("Created game.");
        }

        private void AcceptP2P(SteamId opponentId)
        {
            try
            {
                // For two players to send P2P packets to each other, they each must call this on the other player
                SteamNetworking.AcceptP2PSessionWithUser(opponentId);
            }
            catch
            {
                MelonLogger.Msg("Unable to accept P2P Session with user");
            }
        }

        void OnChatMessageCallback(Lobby lobby, Friend friend, string message)
        {
            // Received chat message
            if (friend.Id != currentId)
            {
                MelonLogger.Msg("incoming chat message");
                MelonLogger.Msg(message);
            }
        }

        // Called whenever you first enter lobby
        void OnLobbyEnteredCallback(Lobby lobby)
        {
            // You joined this lobby
            if (lobby.MemberCount !=
                1) // I do this because this callback triggers on host, I only wanted to use for players joining after host
            {
                isConnectedToLobby = true;
                // You will need to have gotten OpponentSteamId from various methods before (lobby data, joined invite, etc)
                foreach (var connectedFriend in currentLobby.Members)
                {
                    if (connectedFriend.Id != currentId)
                    {
                        HandlePlayerConnection(connectedFriend);
                    }
                }
            }
        }

        // Accepted Steam Game Invite
        async void OnGameLobbyJoinRequestedCallback(Lobby joinedLobby, SteamId id)
        {
            // Attempt to join lobby
            RoomEnter joinedLobbySuccess = await joinedLobby.Join();
            if (joinedLobbySuccess != RoomEnter.Success)
            {
                MelonLogger.Msg("failed to join lobby");
            }
            else
            {
                isHost = false;
                isConnectedToLobby = true;
                MelonLogger.Msg("Joined lobby.");
                currentLobby = joinedLobby;
            }
        }

        void OnLobbyCreatedCallback(Result result, Lobby lobby)
        {
            if (result != Result.OK)
            {
                MelonLogger.Msg("lobby creation result not ok");
                MelonLogger.Msg(result.ToString());
            }
            else
            {
                MelonLogger.Msg("Created lobby successfully");
                isHost = true;
                isConnectedToLobby = true;
            }
        }

        void OnLobbyMemberJoinedCallback(Lobby lobby, Friend friend)
        {
            // The lobby member joined
            
            if (friend.Id != currentId)
            {
                MelonLogger.Msg(friend.Name+" has joined the lobby.");
                HandlePlayerConnection(friend);

                if (isHost)
                {
                    foreach (var valuePair in byteIds)
                    {
                        if (valuePair.Value == currentId) continue;

                        var addMessageData = new ShortIdMessageData()
                        {
                            userId = valuePair.Value,
                            byteId = valuePair.Key
                        };
                        var packetByteBuf =
                            MessageHandler.CompressMessage(NetworkMessageType.ShortIdUpdateMessage, addMessageData);
                        SteamPacketNode.BroadcastMessage(NetworkChannel.Reliable, packetByteBuf);
                    }

                    var idMessageData = new ShortIdMessageData
                    {
                        userId = friend.Id,
                        byteId = RegisterUser(friend.Id)
                    };
                    var secondBuff = MessageHandler.CompressMessage(NetworkMessageType.ShortIdUpdateMessage, idMessageData);
                    SteamPacketNode.BroadcastMessage((byte)NetworkChannel.Reliable, secondBuff);

                    var joinCatchupData = new JoinCatchupData
                    {
                        lastId = SyncedObject.lastId,
                        lastGroupId = SyncedObject.lastGroupId
                    };
                    var catchupBuff = MessageHandler.CompressMessage(NetworkMessageType.JoinCatchupMessage, joinCatchupData);
                    SteamPacketNode.SendMessage(friend.Id, NetworkChannel.Reliable, catchupBuff);
                    
                    MelonLogger.Msg("Registered "+friend.Name+" as a byte");
                }
            }
        }
        
        public static byte RegisterUser(ulong userId)
        {
            var byteId = lastByteId++;
            RegisterUser(byteId, userId);
            return byteId;
        }
        
        public void HandlePlayerConnection(Friend friend)
        {
            if (connectedIds.Contains(friend.Id))
                return;
            
            AcceptP2P(friend.Id);
            MelonLogger.Msg("Added "+friend.Name+" to connected users.");
            connectedIds.Add(friend.Id);
            
            MelonLogger.Msg("Fetched user: "+friend.Name);
            PlayerRepresentation.representations.Add(friend.Id, new PlayerRepresentation(friend));
            MelonLogger.Msg("Added representation");
            userData.Add(friend.Id, friend);
            MelonLogger.Msg("Added userdata");
        }
        

        // I have a screen in game with UI that displays open multiplayer lobbies, I use this method to grab lobby data for UI and joining

        private void leaveLobby()
        {
            isHost = false;
            isConnectedToLobby = false;
            try
            {
                currentLobby.Leave();
            }
            catch
            {
                MelonLogger.Msg("Error leaving current lobby");
            }

            try
            {
                foreach (SteamId connectedId in connectedIds) {
                    SteamNetworking.CloseP2PSessionWithUser(connectedId);
                }
                connectedIds.Clear();
                userData.Clear();
                Instance.CleanData();
            }
            catch
            {
                MelonLogger.Msg("Error closing P2P session with opponent");
            }
        }

        public async Task<bool> CreateFriendLobby()
        {
            if (isConnectedToLobby)
            {
                MelonLogger.Msg("Already in a lobby! Cannot create another one.");
                return true;
            }

            try
            {
                var createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(10);
                if (!createLobbyOutput.HasValue)
                {
                    MelonLogger.Msg("Lobby created but not correctly instantiated");
                    throw new Exception();
                }

                hostedMultiplayerLobby = createLobbyOutput.Value;
                hostedMultiplayerLobby.SetData("friendLobby", "true");
                hostedMultiplayerLobby.SetData(ownerIdIdentifier, playerSteamIdString);
                hostedMultiplayerLobby.SetFriendsOnly();

                currentLobby = hostedMultiplayerLobby;

                isHost = true;
                isConnectedToLobby = true;
                
                MelonLogger.Msg("Created lobby.");

                return true;
            }
            catch (Exception exception)
            {
                MelonLogger.Msg("Failed to create multiplayer lobby");
                MelonLogger.Msg(exception.ToString());
                return false;
            }
        }

        // Allows you to open friends list where game invites will have lobby id
        public void OpenFriendOverlayForGameInvite()
        {
            SteamFriends.OpenGameInviteOverlay(currentLobby.Id);
        }


        public void UpdateRichPresenceStatus()
        {
            if (connectedToSteam)
            {
                string richPresenceKey = "steam_display";

                SteamFriends.SetRichPresence(richPresenceKey, "Playing HBMP");
            }
        }
        
        public static byte GetByteId(SteamId longId) {
            if (longId == currentId) return localByteId;
            
            return byteIds.FirstOrDefault(o => o.Value == longId).Key;
        }
        public static SteamId GetLongId(byte shortId) {
            if (shortId == 0) return Instance.currentLobby.Owner.Id;

            return byteIds.TryIdx(shortId);
        }
    }
}