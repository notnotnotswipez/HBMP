using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using HBMF;
using HBMP.Messages;
using HBMP.Object;
using HBMP.Representations;
using MelonLoader;
using UnityEngine;

namespace HBMP.Nodes
{
    public class Node
    {
        public static Node activeNode;
        public List<long> connectedUsers = new List<long>();
        public static bool isServer => activeNode is Server;
        public Dictionary<long, User> userDatas = new Dictionary<long, User>();
        
        public void ConnectToDiscordServer() {
            DiscordIntegration.lobbyManager.ConnectNetwork(DiscordIntegration.lobby.Id);
            MelonLogger.Msg("Connected to Discord!");
            // Opens all the network channels for sending messages
            DiscordIntegration.lobbyManager.OpenNetworkChannel(DiscordIntegration.lobby.Id, (byte)NetworkChannel.Reliable, true);
            DiscordIntegration.lobbyManager.OpenNetworkChannel(DiscordIntegration.lobby.Id, (byte)NetworkChannel.Unreliable, false);
            DiscordIntegration.lobbyManager.OpenNetworkChannel(DiscordIntegration.lobby.Id, (byte)NetworkChannel.Attack, true);
            DiscordIntegration.lobbyManager.OpenNetworkChannel(DiscordIntegration.lobby.Id, (byte)NetworkChannel.Object, true);
            DiscordIntegration.lobbyManager.OpenNetworkChannel(DiscordIntegration.lobby.Id, (byte)NetworkChannel.Transaction, true);
            MelonLogger.Msg("Connected to Network Channels!");
        }
        
        public void OnDiscordMessageRecieved(long lobbyId, long userId, byte channelId, byte[] data)
        {
            if (data.Length <= 0) // Idk
                throw new Exception("Data was invalid!");

            byte messageType = data[0];
            byte[] realData = new byte[data.Length - sizeof(byte)];

            for (int b = sizeof(byte); b < data.Length; b++)
                realData[b - sizeof(byte)] = data[b];

            PacketByteBuf packetByteBuf = new PacketByteBuf(realData);
            
            MessageHandler.ReadMessage((NetworkMessageType)messageType, packetByteBuf, userId);
        }
        
        public void SendMessage(long userId, byte channel, byte[] data) {
            if (DiscordIntegration.lobby.Id != 0) {
                DiscordIntegration.lobbyManager.SendNetworkMessage(DiscordIntegration.lobby.Id, userId, (byte)channel, data);
            }
        }
        
        public void CreatePlayerRep(long userId)
        {
            if (connectedUsers.Contains(userId))
                return;
            
            MelonLogger.Msg("Added "+userId+" to connected users.");
            connectedUsers.Add(userId);
            DiscordIntegration.userManager.GetUser(userId, OnDiscordUserFetched);
        }
        
        public void OnDiscordUserFetched(Result result, ref User user) {
            MelonLogger.Msg("Fetched user: "+user.Username);
            PlayerRepresentation.representations.Add(user.Id, new PlayerRepresentation(user));
            MelonLogger.Msg("Added representation");
            userDatas.Add(user.Id, user);
            MelonLogger.Msg("Added userdata");
        }
        
        public void OnDiscordUserJoined(long lobbyId, long userId) {
            CreatePlayerRep(userId);
            UserConnectedEvent(lobbyId, userId);
        }
        
        public void OnDiscordUserLeft(long lobbyId, long userId) {
            MelonLogger.Msg("Disconnected user: "+PlayerRepresentation.representations[userId].username);

            PlayerRepresentation.representations[userId].DeleteRepresentation();
            PlayerRepresentation.representations.Remove(userId);
            userDatas.Remove(userId);
            connectedUsers.Remove(userId);
            DiscordIntegration.RemoveUser(userId);

            UserDisconnectEvent(lobbyId, userId);
        }

        public void CleanData()
        {
            foreach (PlayerRepresentation rep in PlayerRepresentation.representations.Values)  {
                GameObject.Destroy(rep.playerRep);
            }
            connectedUsers.Clear();
            PlayerRepresentation.representations.Clear();
            SyncedObject.CleanData();
            DiscordIntegration.byteIds.Clear();
            userDatas.Clear();
            DiscordIntegration.lastByteId = 0;
            
            DiscordIntegration.lobbyManager.OnNetworkMessage -= OnDiscordMessageRecieved;
            DiscordIntegration.lobbyManager.OnMemberConnect -= OnDiscordUserJoined;
            DiscordIntegration.lobbyManager.OnMemberDisconnect -= OnDiscordUserLeft;
        }

        public virtual void BroadcastMessage(byte channel, byte[] data) { }
        
        public void BroadcastMessageP2P(byte channel, byte[] data) {
            connectedUsers.ForEach((user) => { SendMessage(user, channel, data); });

            if (!isServer)
            {
                SendMessage(DiscordIntegration.lobby.OwnerId, channel, data);
            }
        }
        
        public virtual void UserConnectedEvent(long lobbyId, long userId) { }
        
        public virtual void UserDisconnectEvent(long lobbyId, long userId) { }

        public virtual void Shutdown() { }
    }
}