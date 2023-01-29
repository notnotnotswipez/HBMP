using System.Linq;
using Discord;
using MelonLoader;
using Steamworks;
using Result = Discord.Result;

namespace HBMP
{
    public class DiscordRichPresence
    {
        private static global::Discord.Discord discord;
        public static LobbyManager lobbyManager;
        public static ActivityManager activityManager;
        public static Activity activity;
        public static Lobby currentLobby;

        public static bool hasLobby;
        
        public static void Init()
        {
            discord = new global::Discord.Discord(988289883107917835, (ulong)CreateFlags.Default);
            activityManager = discord.GetActivityManager();
            lobbyManager = discord.GetLobbyManager();
            
            DefaultRichPresence();
            
            activityManager.OnActivityJoin += secret =>
                lobbyManager.ConnectLobbyWithActivitySecret(secret, DiscordJoinLobby);
        }
        
        private static void DiscordJoinLobby(Result result, ref Lobby lobby)
        {
            if (hasLobby)
            {
                MelonLogger.Error("You are already in a lobby!");
                return;
            }

            if (result != Result.Ok)
                return;

            currentLobby = lobby;
            
            MelonLogger.Msg("Joined DISCORD lobby with id: " + lobby.Id);
            
            lobbyManager.OnMemberConnect += UserConnectedEvent;
            lobbyManager.OnMemberDisconnect += UserDisconnectEvent;
            
            var users = lobbyManager.GetMemberUsers(lobby.Id);

            activity.Party = new ActivityParty
            {
                Id = lobby.Id.ToString(),
                Size = new PartySize { CurrentSize = users.Count(), MaxSize = (int)lobby.Capacity }
            };

            activity.Details = "This user is connected to a HBMP server!";
            activity.State = "Killing with friends";

            activity.Secrets = new ActivitySecrets
            {
                Join = lobbyManager.GetLobbyActivitySecret(lobby.Id)
            };

            activity.Instance = true;

            SteamId steamId = ulong.Parse(lobbyManager.GetLobbyMetadataValue(lobby.Id, "steamLobbyId"));
            MelonLogger.Msg("JOINING STEAM LOBBY.");
            SteamMatchmaking.JoinLobbyAsync(steamId);

            UpdateActivity();
        }
        
        private static void UserConnectedEvent(long lobbyId, long userId)
        {
            activity.Party.Size.CurrentSize = 1 + activity.Party.Size.CurrentSize;
            UpdateActivity();
        }
        
        private static void UserDisconnectEvent(long lobbyId, long userId)
        {
            activity.Party.Size.CurrentSize -= 1;
            UpdateActivity();
        }

        public static void Update()
        {
            discord.RunCallbacks();
        }
        
        public static void MakeDiscordLobby()
        {
            var lobbyTransaction = lobbyManager.GetLobbyCreateTransaction();
            lobbyTransaction.SetCapacity(10);
            lobbyTransaction.SetLocked(false);
            lobbyTransaction.SetType(LobbyType.Private);
            lobbyManager.CreateLobby(lobbyTransaction, onDiscordLobbyCreate);
        }

        private static void onDiscordLobbyCreate(Result result, ref Lobby lobby)
        {
            if (result != Result.Ok) return;
            currentLobby = lobby;
            HostRichPresence(lobby);
        }

        public static void HostRichPresence(Lobby lobby)
        {
            activity.Party = new ActivityParty
            {
                Id = lobby.Id.ToString(),
                Size = new PartySize { CurrentSize = 1, MaxSize = 10 }
            };
            
            activity.Details = "This user is hosting a HBMP server!";
            activity.State = "Killing with Friends";
            activity.Secrets = new ActivitySecrets
            {
                Join = lobbyManager.GetLobbyActivitySecret(lobby.Id)
            };
            LobbyTransaction transaction = lobbyManager.GetLobbyUpdateTransaction(lobby.Id);
            transaction.SetMetadata("steamLobbyId", SteamIntegration.Instance.currentLobby.Id.ToString());
            
            lobbyManager.UpdateLobby(lobby.Id, transaction, (result) =>
            {
                
            });

            hasLobby = true;
            
            UpdateActivity();
        }

        public static void DefaultRichPresence()
        {
            activity = new Activity
            {
                State = "Playing alone",
                Details = "Not connected to a server",
                Instance = true,
                Assets =
                {
                    LargeImage = "blmp",
                }
            };

            activity.Instance = false;

            UpdateActivity();
        }
        
        public static void UpdateActivity()
        {
            activityManager.UpdateActivity(activity, result => { });
        }
    }
}