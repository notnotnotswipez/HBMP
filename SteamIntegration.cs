using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBMP.Messages;
using HBMP.Nodes;
using HBMP.Object;
using HBMP.Representations;
using MelonLoader;
using Steamworks.Data;
using Lobby = Steamworks.Data.Lobby;
using Result = Steamworks.Result;

namespace HBMP
{
    public class SteamIntegration
    {
        public static SteamIntegration Instance;
        public static uint gameAppId = 480;

        public string currentName { get; set; } 
        public static SteamId currentId { get; set; }

        public static bool hasLobby = false;

        public static ulong ownerId = 0;

        private string ownerIdIdentifier = "ownerId";

        private bool connectedToSteam = false;

        public static List<ulong> connectedIds = new List<ulong>();
        public static Dictionary<SteamId, Friend> userData = new Dictionary<SteamId, Friend>();
        public static Dictionary<byte, ulong> byteIds = new Dictionary<byte, ulong>();
        public static Dictionary<NetworkChannel, SendType> networkChannels = new Dictionary<NetworkChannel, SendType>();
        public static Dictionary<NetworkChannel, SendType> reliableChannels = new Dictionary<NetworkChannel, SendType>();

        public Lobby currentLobby;
        private Lobby hostedMultiplayerLobby;

        public static bool isHost = false;

        private bool applicationHasQuit = false;

        public static byte localByteId = 0;
        public static byte lastByteId = 1;

        public static Sockets.Sockets.ClientSocket clientSocket;
        public static Sockets.Sockets.ServerSocket serverSocket;

        public SteamIntegration()
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
                MelonLogger.Msg("Registered "+longId+" as byte: "+byteId);
                byteIds.Add(byteId, longId);
            }
        }

        public static void Init()
        {
            if (Instance == null)
            {
                SteamIntegration steamIntegration = new SteamIntegration();
            }
        }

        public static void Disconnect(bool fullyShutDown)
        {
            if (clientSocket != null)
            {
                clientSocket.Close();
                clientSocket = null;
            }

            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket = null;
            }

            if (isHost)
            {
                DiscordRichPresence.lobbyManager.DeleteLobby(DiscordRichPresence.currentLobby.Id, result => { });
                DiscordRichPresence.currentLobby = new Discord.Lobby();

                DiscordRichPresence.hasLobby = false;
                DiscordRichPresence.DefaultRichPresence();
            }
            else
            {
                DiscordRichPresence.lobbyManager.DisconnectLobby(DiscordRichPresence.currentLobby.Id, result => { });
                DiscordRichPresence.currentLobby = new Discord.Lobby();

                DiscordRichPresence.hasLobby = false;
                DiscordRichPresence.DefaultRichPresence();
            }

            hasLobby = false;
            isHost = false;
            Instance.CleanData();
            Instance.LeaveLobby();
            if (fullyShutDown)
            {
                SteamClient.Shutdown();
            }
        }

        public void Connect(ulong steamId)
        {
            MelonLogger.Msg("Connecting to "+steamId, ConsoleColor.Blue);
            clientSocket = SteamNetworkingSockets.ConnectRelay<Sockets.Sockets.ClientSocket>(steamId);
        }

        public void OpenNetworkChannels()
        {
            networkChannels.Add(NetworkChannel.Unreliable, SendType.Unreliable);
            networkChannels.Add(NetworkChannel.Reliable, SendType.Reliable);
            networkChannels.Add(NetworkChannel.Object, SendType.Reliable);
            networkChannels.Add(NetworkChannel.Attack, SendType.Reliable);
            networkChannels.Add(NetworkChannel.Transaction, SendType.Reliable);
            
            reliableChannels.Add(NetworkChannel.Reliable, SendType.Reliable);
            reliableChannels.Add(NetworkChannel.Object, SendType.Reliable);
            reliableChannels.Add(NetworkChannel.Attack, SendType.Reliable);
            reliableChannels.Add(NetworkChannel.Transaction, SendType.Reliable);
        }

        public void CleanData()
        {
            foreach (PlayerRepresentation rep in PlayerRepresentation.representations.Values)  {
                rep.DeleteRepresentation();
            }
            SteamPacketNode.cachedConnections.Clear();
            connectedIds.Clear();
            PlayerRepresentation.representations.Clear();
            SyncedObject.CleanData();
            byteIds.Clear();
            userData.Clear();
            lastByteId = 1;
            localByteId = 0;
            MainMod.idsReadyForPlayerInfo = new List<ulong>();
        }

        void Start()
        {
            MelonLogger.Msg("Running start method.");
            // Callbacks
            SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedCallback;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnChatMessage += OnChatMessageCallback;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnectedCallback;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeaveCallback;
            SteamFriends.OnGameLobbyJoinRequested += OnInviteClicked;

            MelonLogger.Msg("Finished registering start method.");
        }

        public void Update()
        {
            try
            {
                clientSocket?.Receive(6000);
            }
            catch (Exception e)
            {
                // ignore
            }
            
            try
            {
                serverSocket?.Receive(6000);
            }
            catch (Exception e)
            {
                // ignore
            }
            
            SteamClient.RunCallbacks();
        }
        
        void OnLobbyMemberDisconnectedCallback(Lobby lobby, Friend friend)
        {
            ProcessMemberLeft(friend);
        }

        void OnLobbyMemberLeaveCallback(Lobby lobby, Friend friend)
        {
            ProcessMemberLeft(friend);
        }

        public static void ProcessMemberLeft(Friend friend)
        {
            if (friend.Id != currentId)
            {
                DiscordRichPresence.activity.Party.Size.CurrentSize -= 1;
                DiscordRichPresence.UpdateActivity();
                
                try
                {
                    PlayerRepresentation playerRepresentation = PlayerRepresentation.representations[friend.Id];
                    playerRepresentation.DeleteRepresentation();
                    
                    if (isHost)
                    {
                        SteamPacketNode.cachedConnections.Remove(friend.Id);
                    }

                    connectedIds.Remove(friend.Id);
                    userData.Remove(friend.Id);
                    byteIds.Remove(GetByteId(friend.Id));
                }
                catch
                {
                    MelonLogger.Msg("Unable to update disconnected player nameplate / process disconnect cleanly");
                }
            }
        }

        void OnLobbyGameCreatedCallback(Lobby lobby, uint ip, ushort port, SteamId steamId)
        {
            // MelonLogger.Msg("Created game.");
        }

        void OnChatMessageCallback(Lobby lobby, Friend friend, string message)
        {
            if (friend.Id != currentId)
            {
                MelonLogger.Msg("incoming chat message");
                MelonLogger.Msg(message);
            }
        }
        
        void OnLobbyEnteredCallback(Lobby lobby)
        {
            if (lobby.MemberCount !=
                1)
            {
                ownerId = ulong.Parse(lobby.GetData(ownerIdIdentifier));
                hasLobby = true;
                lobby.SendChatString("I have connected to the lobby!");
                currentLobby = lobby;
                HandlePlayerConnection(new Friend(ownerId));
                foreach (var connectedFriend in currentLobby.Members)
                {
                    if (connectedFriend.Id != currentId)
                    {
                        HandlePlayerConnection(connectedFriend);
                    }
                }
                Connect(ownerId);
            }
        }
        async void OnInviteClicked(Lobby joinedLobby, SteamId id)
        {
            RoomEnter joinedLobbySuccess = await joinedLobby.Join();
            if (joinedLobbySuccess != RoomEnter.Success)
            {
                MelonLogger.Error("Lobby could not be joined.");
            }
            else
            {
                isHost = false;
                hasLobby = true;
                MelonLogger.Msg("Joined lobby.");
                currentLobby = joinedLobby;
            }
        }

        void OnLobbyCreated(Result result, Lobby lobby)
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
                hasLobby = true;
            }
        }

        void OnLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            // Not us, means its another player.
            if (friend.Id != currentId)
            {
                DiscordRichPresence.activity.Party.Size.CurrentSize = 1 + DiscordRichPresence.activity.Party.Size.CurrentSize;
                DiscordRichPresence.UpdateActivity();

                MelonLogger.Msg(friend.Name+" has joined the lobby.");
                HandlePlayerConnection(friend);
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

            if (friend.Id == currentId) return;
                
            MelonLogger.Msg("Added "+friend.Name+" to connected users.");
            connectedIds.Add(friend.Id);
            
            MelonLogger.Msg("Fetched user: "+friend.Name);
            if (!PlayerRepresentation.representations.ContainsKey(friend.Id))
            {
                PlayerRepresentation.representations.Add(friend.Id, new PlayerRepresentation(friend));
            }
            MelonLogger.Msg("Added representation");
            userData.Add(friend.Id, friend);
            MelonLogger.Msg("Added userdata");
        }

        private void LeaveLobby()
        {
            isHost = false;
            hasLobby = false;
            try
            {
                currentLobby.Leave();
            }
            catch
            {
                MelonLogger.Error("Error leaving current lobby");
                return;
            }

            connectedIds.Clear();
            userData.Clear();
        }

        public async Task CreateLobby()
        {
            if (hasLobby)
            {
                MelonLogger.Msg("Already in a lobby! Cannot create another one.");
                return;
            }

            try
            {
                var createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(10);
                if (!createLobbyOutput.HasValue)
                {
                    MelonLogger.Error("Lobby was not created properly!");
                    return;
                }

                serverSocket = SteamNetworkingSockets.CreateRelaySocket<Sockets.Sockets.ServerSocket>();
                
                hostedMultiplayerLobby = createLobbyOutput.Value;

                hostedMultiplayerLobby.SetData(ownerIdIdentifier, currentId.ToString());

                hostedMultiplayerLobby.SetFriendsOnly();
                hostedMultiplayerLobby.SetJoinable(true);

                currentLobby = hostedMultiplayerLobby;

                isHost = true;
                hasLobby = true;
                
                MelonLogger.Msg("Created lobby.");
                Connect(currentId);
                
                DiscordRichPresence.MakeDiscordLobby();
            }
            catch (Exception exception)
            {
                MelonLogger.Msg("Failed to create multiplayer lobby");
                MelonLogger.Msg(exception.ToString());
            }
        }

        public static byte GetByteId(SteamId longId) {
            if (longId == currentId) return localByteId;
            
            return byteIds.FirstOrDefault(o => o.Value == longId).Key;
        }
        public static SteamId GetLongId(byte shortId) {
            if (shortId == 0) return ownerId;

            if (byteIds.ContainsKey(shortId))
            {
                return byteIds[shortId];
            }
            return ownerId;
        }
    }
}