using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;
using Discord;
using HBMP.Extensions;
using HBMP.Nodes;

namespace HBMP
{
    public class DiscordIntegration
    {
        private static Discord.Discord discord;
        public static UserManager userManager;
        public static ActivityManager activityManager;
        public static LobbyManager lobbyManager;
        public static User currentUser;
        public static Activity activity;
        public static Lobby lobby;
        public static bool hasLobby => lobby.Id != 0;

        public static bool isHost => hasLobby && lobby.OwnerId == currentUser.Id;

        public static bool isConnected => hasLobby && lobby.OwnerId != currentUser.Id;
        
        public static Dictionary<byte, long> byteIds = new Dictionary<byte, long>();
        public static byte localByteId = 0;
        public static byte lastByteId = 1;

        public static void Init()
        {
            discord = new Discord.Discord(988289883107917835, (UInt64)CreateFlags.Default);
            userManager = discord.GetUserManager();
            activityManager = discord.GetActivityManager();
            lobbyManager = discord.GetLobbyManager();
            userManager.OnCurrentUserUpdate += () =>
                {
                    currentUser = userManager.GetCurrentUser();
                    MelonLogger.Msg($"Current Discord User: {currentUser.Username}");
                };
            DefaultRichPresence();
        }
        
        public static void Update() => discord.RunCallbacks();

        public static void Flush() => lobbyManager.FlushNetwork();
        
        public static void Tick() {
            Update();
            Flush();
        }
        
        public static void DefaultRichPresence()
        {
            activity = new Activity()
            {
                State = "Playing alone",
                Details = "Not connected to a server",
                Instance = true,
                Assets =
                {
                    LargeImage = "hardbulletmultiplayerlogo",
                    LargeText = Mod.GetVersionString()
                }
            };

            activity.Instance = false;

            UpdateActivity();
        }

        public static void RegisterUser(long userId, byte byteId)
        {
            if (byteIds.ContainsKey(byteId))
            {
                return;
            }

            byteIds.Add(byteId, userId);
        } 
        
        public static byte CreateByteId() => lastByteId++;
        
        public static void RemoveUser(long userId) => byteIds.Remove(GetByteId(userId));
        
        public static byte GetByteId(long longId) {
            if (longId == currentUser.Id) return localByteId;
            
            return byteIds.FirstOrDefault(o => o.Value == longId).Key;
        }
        public static long GetLongId(byte shortId) {
            if (shortId == 0) return lobby.OwnerId;

            return byteIds.TryIdx(shortId);
        }

        public static byte RegisterUser(long userId) {
            byte byteId = CreateByteId();
            RegisterUser(userId, byteId);
            return byteId;
        }


        public static void UpdateActivity()
        {
            activityManager.UpdateActivity(activity, (result) => {});
        }
    }
}
