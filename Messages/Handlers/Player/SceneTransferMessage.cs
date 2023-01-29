using MelonLoader;
using UnityEngine.SceneManagement;

namespace HBMP.Messages.Handlers
{
    public class SceneTransferMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            SceneTransferData sceneTransferData = (SceneTransferData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte((byte)sceneTransferData.sceneIndex);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, ulong sender)
        {
            int sceneIndex = (int) packetByteBuf.ReadByte();
            MelonLogger.Msg("Attempting to load scene with scene index: "+sceneIndex);
            SceneManager.LoadScene(sceneIndex);
        }

        public override void ReadDataServer(PacketByteBuf packetByteBuf, ulong sender)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SceneTransferData : MessageData
    {
        public int sceneIndex;
    }
}