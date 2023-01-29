using System;
using System.Collections;
using System.Runtime.InteropServices;
using HBMP.Messages;
using HBMP.Nodes;
using MelonLoader;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace HBMP.Sockets
{
    public class Sockets
    {
        public class ConnectionCoroutines
        {
            public static IEnumerator WaitAndSendGreeting()
            {
                yield return new WaitForSecondsRealtime(0.2f);
                Utils.Utils.EmptyMessageData emptyMessageData = new Utils.Utils.EmptyMessageData(new PacketByteBuf());

                PacketByteBuf message = PacketHandler.CompressMessage(PacketType.PlayerGreetingMessage, emptyMessageData);
                SteamPacketNode.SendMessageDirectToServer(NetworkChannel.Transaction,message.getBytes());
                
                MelonLogger.Msg("Sent greeting packet to server!");
            }
        }

        public class ClientSocket : ConnectionManager
        {
            public override void OnConnecting(ConnectionInfo data) {
                base.OnConnecting(data);
                MelonLogger.Msg("Connecting to socket...", ConsoleColor.Green);
            }
			
            public override void OnConnected(ConnectionInfo data) {
                base.OnConnected(data);
                
                MelonLogger.Msg("CONNECTED to socket!", ConsoleColor.Yellow);
                MelonCoroutines.Start(ConnectionCoroutines.WaitAndSendGreeting());
            }

            public override void OnDisconnected(ConnectionInfo data) {
                MelonLogger.Msg("Steamworks Disconnected from Socket.");
            }
			
            public override void OnMessage(IntPtr olddata, int size, Int64 messageNum, Int64 recvTime, int channel) {
                var data = new byte[size];
                Marshal.Copy(olddata, data, 0, size);

                if (data.Length <= 0)
                    throw new Exception("Data was invalid!");
                
                byte messageType = data[0];
                byte[] realData = new byte[data.Length - sizeof(byte)];
                for (int b = sizeof(byte); b < data.Length; b++)
                    realData[b - sizeof(byte)] = data[b];
                PacketByteBuf packetByteBuf = new PacketByteBuf(realData);
                PacketType packetType = (PacketType)messageType;

                PacketHandler.ReadMessage(packetType, packetByteBuf, 0, false);
            }
        }

        public class ServerSocket : SocketManager
        {
            public override void OnConnecting(Connection connection, ConnectionInfo data)
            {
                base.OnConnecting(connection, data);
                connection.Accept();
            }

            public override void OnConnected(Connection connection, ConnectionInfo data)
            {
                base.OnConnected(connection, data);
                MelonLogger.Msg("Hello from the server socket! We connected to somebody!", ConsoleColor.Yellow);
            }

            public override void OnDisconnected(Connection connection, ConnectionInfo info)
            {
                // This is handled by the lobby.
                MelonLogger.Msg("Server has disconnected.", ConsoleColor.Red);
            }

            public override void OnMessage(Connection connection, NetIdentity identity, IntPtr olddata, int size,
                long messageNum, long recvTime, int channel)
            {
                ulong steamid = identity.SteamId.Value;
                if (!SteamPacketNode.cachedConnections.ContainsKey(steamid))
                {
                    SteamPacketNode.cachedConnections.Add(steamid, connection);
                    MelonLogger.Msg("Stored connection from user: "+steamid+", this is the first time we received a packet from them!", ConsoleColor.Yellow);
                }

                var data = new byte[size];
                Marshal.Copy(olddata, data, 0, size);

                if (data.Length <= 0)
                    throw new Exception("Data was invalid!");
                
                byte messageType = data[0];

                byte[] realData = new byte[data.Length - sizeof(byte)];
                for (int b = sizeof(byte); b < data.Length; b++)
                    realData[b - sizeof(byte)] = data[b];
                PacketByteBuf packetByteBuf = new PacketByteBuf(realData);
                PacketType packetType = (PacketType)messageType;

                PacketHandler.ReadMessage(packetType, packetByteBuf, steamid, true);
            }
        }
    }
}