using System;
using System.Collections.Generic;
using System.Text;
using HBMP.DataType;

namespace HBMP.Messages
{
    public class PacketByteBuf
    {
        public int byteIndex = 0;
        private List<byte> byteList = new List<byte>();
        private byte[] bytes;

        public PacketByteBuf(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public PacketByteBuf()
        {
            bytes = new byte[]{};
        }

        public byte[] getBytes()
        {
            return bytes;
        }

        public byte ReadByte()
        {
            return getBytes()[byteIndex++];
        }
        
        public SimplifiedTransform ReadSimpleTransform()
        {
            List<byte> transformBytes = new List<byte>();
            for (int i = byteIndex; i < byteIndex+SimplifiedTransform.size; i++) {
                transformBytes.Add(getBytes()[i]);
            }
            SimplifiedTransform simpleTransform = SimplifiedTransform.FromBytes(transformBytes.ToArray());
            byteIndex += SimplifiedTransform.size;

            return simpleTransform;
        }

        public void WriteSimpleTransform(SimplifiedTransform simplifiedTransform)
        {
            foreach (byte b in simplifiedTransform.GetBytes()) {
                byteList.Add(b);
            }
        }

        public String ReadString()
        {
            byte[] pathBytes = new byte[getBytes().Length - byteIndex];
            for (int i = 0; i < pathBytes.Length; i++)
                pathBytes[i] = getBytes()[byteIndex++];
            return Encoding.UTF8.GetString(pathBytes);
        }

        public float ReadLong()
        {
            long longNum = BitConverter.ToInt64(getBytes(), byteIndex);
            byteIndex += sizeof(long);
            return longNum;
        }
        
        public int ReadInt()
        {
            int longNum = BitConverter.ToInt32(getBytes(), byteIndex);
            byteIndex += sizeof(int);
            return longNum;
        }

        public bool ReadBoolean()
        {
            return Convert.ToBoolean(getBytes()[byteIndex++]);
        }

        public ushort ReadUShort()
        {
            ushort longNum = BitConverter.ToUInt16(getBytes(), byteIndex);
            byteIndex += sizeof(ushort);
            return longNum;
        }

        public void WriteType(NetworkMessageType networkMessageType)
        {
            byteList.Add((byte)networkMessageType);
        }

        public void WriteString(String str)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(str);
            foreach (byte b in utf8) {
                byteList.Add(b);
            }
        }

        public void WriteBool(bool boolean)
        {
            byteList.Add(Convert.ToByte(boolean));
        }

        public void WriteInt(int integer)
        {
            foreach (byte b in BitConverter.GetBytes(integer)) {
                byteList.Add(b);
            }
        }
        
        public void WriteDouble(double doub)
        {
            foreach (byte b in BitConverter.GetBytes(doub)) {
                byteList.Add(b);
            }
        }
        
        public void WriteUShort(ushort shor)
        {
            foreach (byte b in BitConverter.GetBytes(shor)) {
                byteList.Add(b);
            }
        }

        public void WriteByte(byte b)
        {
            byteList.Add(b);
        }
        
        public void WriteLong(long longNum)
        {
            foreach (byte b in BitConverter.GetBytes(longNum)) {
                byteList.Add(b);
            }
        }

        public void WriteFloat(float floatNum)
        {
            foreach (byte b in BitConverter.GetBytes(floatNum)) {
                byteList.Add(b);
            }
        }

        public void create()
        {
            bytes = byteList.ToArray();
        }
    }
}