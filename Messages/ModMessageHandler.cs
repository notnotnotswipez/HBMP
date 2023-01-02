using System;
using HBMP.Messages.Handlers;
using HBMP.Nodes;
using MelonLoader;

namespace HBMP.Messages
{
    public class ModMessageHandler
    {
        public static void CompressAndSendMessage(ushort modMessageId, MessageData messageData)
        {
            PacketByteBuf packetByteBuf =
                MessageHandler.CompressMessage(modMessageId, messageData);
            SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf);
        }

        public static void RegisterModMessage(ushort modMessageId, ModExtensionMessage modExtensionMessage)
        {
            string name = modExtensionMessage.GetType().Name;
            if (ModMessage.Extensions.ContainsKey(modMessageId))
            {
                MelonLogger.Msg(ConsoleColor.Green, "Mod Message Registry Conflict (HBMPSteamNetwork)...");
                MelonLogger.Msg(ConsoleColor.Red, name+" attempted to register with ID: "+modMessageId+" but that spot is already occupied by: "+ModMessage.Extensions[modMessageId].GetType().Name);
                MelonLogger.Msg(ConsoleColor.Red, "If you are a mod developer, you should fix this conflict, either change your ID or talk to the other mod author if possible.");
                MelonLogger.Msg(ConsoleColor.Red, "Continuing without registering...");
                return;
            }
            ModMessage.Extensions.Add(modMessageId, modExtensionMessage);
            MelonLogger.Msg(ConsoleColor.Yellow+"Registered Mod Message for HBMPSteamNetwork: ");
            MelonLogger.Msg(ConsoleColor.Magenta+name+" registered with ID: "+modMessageId);
        }
    }
}