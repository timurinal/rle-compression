using System.Text;

namespace TInal;

public static class Compression
{
    public static byte[] Compress(int[] data)
    {
        ByteBuffer buffer = new ByteBuffer();

        for (int i = 0; i < data.Length; )
        {
            var value = data[i];

            if (i + 1 < data.Length && data[i + 1] == value)
            {
                byte repeatCount = 0;

                int r = 0;
                while (i + r < data.Length && data[i + r] == value || repeatCount < byte.MaxValue)
                {
                    repeatCount++;
                    r++;
                }
            
                buffer.WriteByte(repeatCount);
                buffer.WriteInt32(value);
            
                i += repeatCount;
            }
            else
            {
                buffer.WriteByte(1); // Single occurrence
                buffer.WriteInt32(value);
                i++; // Move to the next element
            }
        }

        if (data.Length * sizeof(int) < buffer.Length) // Data is smaller uncompressed
        {
            buffer.ClearBytes();
            buffer.WriteByte(0);

            for (int i = 0; i < data.Length; i++)
            {
                buffer.WriteInt32(data[i]);
            }
            
            return buffer.GetAllBytes().ToArray();
        }
        else
        {
            buffer.InsertByte(0, 1);
            return buffer.GetAllBytes().ToArray();
        }
    }
    
    public static int[] Decompress(byte[] data)
    {
        ByteBufferReader reader = new ByteBufferReader(data);

        List<int> decompressed = new();

        bool compressed = reader.ReadByte() == 1;

        if (compressed)
        {
            while (!reader.IsAtEndOfBuffer())
            {
                byte repeatCount = reader.ReadByte();
                int value = reader.ReadInt32();
            
                for (int i = 0; i < repeatCount; i++)
                    decompressed.Add(value);
            }
        }
        else
        {
            while (!reader.IsAtEndOfBuffer())
            {
                decompressed.Add(reader.ReadInt32());
            }
        }
        
        return decompressed.ToArray();
    }
    
    struct ByteBuffer
    {
        public int Length => _bytes.Count;
        
        private List<byte> _bytes;

        public ByteBuffer()
        {
            _bytes = new();
        }
        
        public ByteBuffer(byte[] bytes)
        {
            _bytes = [..bytes];
        }
        
        public ByteBuffer(List<byte> bytes)
        {
            _bytes = bytes;
        }

        public void InsertByte(int index, byte value)
        {
            _bytes.Insert(index, value);
        }
        
        public void WriteByte(byte v)
        {
            _bytes.Add(v);
        }
        
        public void WriteFloat(float v)
        {
            byte[] bytes = BitConverter.GetBytes(v);
            _bytes.AddRange(bytes);
        }
        
        public void WriteInt16(Int16 v)
        {
            byte[] bytes = BitConverter.GetBytes(v);
            _bytes.AddRange(bytes);
        }
        public void WriteInt32(Int32 v)
        {
            byte[] bytes = BitConverter.GetBytes(v);
            _bytes.AddRange(bytes);
        }
        public void WriteInt64(Int64 v)
        {
            byte[] bytes = BitConverter.GetBytes(v);
            _bytes.AddRange(bytes);
        }
        public void WriteUInt16(UInt16 v)
        {
            byte[] bytes = BitConverter.GetBytes(v);
            _bytes.AddRange(bytes);
        }
        public void WriteUInt32(UInt32 v)
        {
            byte[] bytes = BitConverter.GetBytes(v);
            _bytes.AddRange(bytes);
        }
        public void WriteUInt64(UInt64 v)
        {
            byte[] bytes = BitConverter.GetBytes(v);
            _bytes.AddRange(bytes);
        }
        
        public void WriteBool(bool v)
        {
            byte[] bytes = new byte[] { v ? (byte)1 : (byte)0 };
            _bytes.AddRange(bytes);
        }
        
        public void WriteChar(char v)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(v.ToString());
            _bytes.AddRange(bytes);
        }
        public void WriteString(string v)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(v);
            _bytes.AddRange(bytes);
        }
        public void WriteSizedString(string v)
        {
            WriteInt32(v.Length);
            byte[] bytes = Encoding.Unicode.GetBytes(v);
            _bytes.AddRange(bytes);
        }

        public byte ReadByte(int position)
        {
            return _bytes[position];
        }

        public float ReadFloat(int position)
        {
            int size = sizeof(float);
            var tBytes = _bytes.GetRange(position, size).ToArray();
            return BitConverter.ToSingle(tBytes);
        }
        
        public Int16 ReadInt16(int position)
        {
            int size = sizeof(Int16);
            var tBytes = _bytes.GetRange(position, size).ToArray();
            return BitConverter.ToInt16(tBytes);
        }
        public Int32 ReadInt32(int position)
        {
            int size = sizeof(Int32);
            var tBytes = _bytes.GetRange(position, size).ToArray();
            return BitConverter.ToInt32(tBytes);
        }
        public Int64 ReadInt64(int position)
        {
            int size = sizeof(Int64);
            var tBytes = _bytes.GetRange(position, size).ToArray();
            return BitConverter.ToInt64(tBytes);
        }
        public UInt16 ReadUInt16(int position)
        {
            int size = sizeof(UInt16);
            var tBytes = _bytes.GetRange(position, size).ToArray();
            return BitConverter.ToUInt16(tBytes);
        }
        public UInt32 ReadUInt32(int position)
        {
            int size = sizeof(UInt32);
            var tBytes = _bytes.GetRange(position, size).ToArray();
            return BitConverter.ToUInt32(tBytes);
        }
        public UInt64 ReadUInt64(int position)
        {
            int size = sizeof(UInt64);
            var tBytes = _bytes.GetRange(position, size).ToArray();
            return BitConverter.ToUInt64(tBytes);
        }
        
        public bool ReadBool(int position)
        {
            return _bytes[position] != 0;
        }

        public char ReadChar(int position)
        {
            int size = sizeof(char);
            var tBytes = _bytes.GetRange(position, size).ToArray();
            return Encoding.Unicode.GetChars(tBytes)[0];
        }
        public string ReadString(int position, int length)
        {
            var tBytes = _bytes.GetRange(position, length).ToArray();
            return Encoding.Unicode.GetString(tBytes);
        }
        public string ReadSizedString(int position)
        {
            int length = ReadInt32(position);

            position += sizeof(Int32);

            return ReadString(position, length);
        }

        public void ClearBytes() => _bytes.Clear();
        
        public List<byte> GetAllBytes() => _bytes;

        public ByteBufferReader GetReader() => new(this);
    }
    
    class ByteBufferReader
    {
        private ByteBuffer _buffer;

        private int _readerPosition;

        public ByteBufferReader(ByteBuffer buffer)
        {
            _buffer = buffer;

            _readerPosition = 0;
        }
        
        public ByteBufferReader(byte[] bytes)
        {
            _buffer = new ByteBuffer(bytes);

            _readerPosition = 0;
        }

        public byte ReadByte()
        {
            var v = _buffer.ReadByte(_readerPosition);
            SkipBytes(1);
            return v;
        }
        
        public float ReadFloat()
        {
            var v = _buffer.ReadFloat(_readerPosition);
            int size = sizeof(float);
            SkipBytes(size);
            return v;
        }
        
        public Int16 ReadInt16()
        {
            var v = _buffer.ReadInt16(_readerPosition);
            int size = sizeof(Int16);
            SkipBytes(size);
            return v;
        }
        public Int32 ReadInt32()
        {
            var v = _buffer.ReadInt32(_readerPosition);
            int size = sizeof(Int32);
            SkipBytes(size);
            return v;
        }
        public Int64 ReadInt64()
        {
            var v = _buffer.ReadInt64(_readerPosition);
            int size = sizeof(Int64);
            SkipBytes(size);
            return v;
        }
        public UInt16 ReadUInt16()
        {
            var v = _buffer.ReadUInt16(_readerPosition);
            int size = sizeof(UInt16);
            SkipBytes(size);
            return v;
        }
        public UInt32 ReadUInt32()
        {
            var v = _buffer.ReadUInt32(_readerPosition);
            int size = sizeof(UInt32);
            SkipBytes(size);
            return v;
        }
        public UInt64 ReadUInt64()
        {
            var v = _buffer.ReadUInt64(_readerPosition);
            int size = sizeof(UInt64);
            SkipBytes(size);
            return v;
        }
        
        public bool ReadBool()
        {
            var v = _buffer.ReadBool(_readerPosition);
            SkipBytes(1);
            return v;
        }
        
        public char ReadChar()
        {
            var v = _buffer.ReadChar(_readerPosition);
            int size = sizeof(char);
            SkipBytes(size);
            return v;
        }
        
        public string ReadString(int length)
        {
            var v = _buffer.ReadString(_readerPosition, length);
            SkipBytes(length);
            return v;
        }
        
        public string ReadSizedString()
        {
            int length = _buffer.ReadInt32(_readerPosition);
            var v = _buffer.ReadSizedString(_readerPosition);
            SkipBytes(sizeof(Int32) + length);
            return v;
        }

        public void SkipBytes(int count) => _readerPosition += count;
        public void SetReaderPosition(int position) => _readerPosition = position;

        public bool IsAtEndOfBuffer()
        {
            return _readerPosition >= _buffer.Length;
        }
    }
}
