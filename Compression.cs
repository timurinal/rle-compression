using System.Runtime.InteropServices;

namespace TInal;

public static class Compression
{
    public static byte[] Compress<T>(T[] data) where T : struct
    {
        ByteBuffer buffer = new ByteBuffer();

        for (int i = 0; i < data.Length;)
        {
            var value = data[i];

            if (i + 1 < data.Length && data[i + 1].Equals(value))
            {
                byte repeatCount = 0;

                int r = 0;
                while (repeatCount < byte.MaxValue && i + r < data.Length && data[i + r].Equals(value))
                {
                    repeatCount++;
                    r++;
                }

                buffer.WriteByte(repeatCount);
                buffer.WriteObj(value);

                i += repeatCount;
            }
            else
            {
                buffer.WriteByte(1); // Single occurrence
                buffer.WriteObj(value);
                i++; // Move to the next element
            }
        }

        if (data.Length * sizeof(int) < buffer.Length) // Data is smaller uncompressed
        {
            buffer.ClearBytes();
            buffer.WriteByte(0);

            for (int i = 0; i < data.Length; i++)
            {
                buffer.WriteObj(data[i]);
            }

            return buffer.GetAllBytes().ToArray();
        }
        else
        {
            buffer.InsertByte(0, 1);
            return buffer.GetAllBytes().ToArray();
        }
    }

    public static T[] Decompress<T>(byte[] data) where T : struct
    {
        ByteBufferReader reader = new ByteBufferReader(data);

        List<T> decompressed = new();

        bool compressed = reader.ReadByte() == 1;

        if (compressed)
        {
            while (!reader.IsAtEndOfBuffer())
            {
                byte repeatCount = reader.ReadByte();
                T value = reader.ReadObj<T>();

                for (int i = 0; i < repeatCount; i++)
                    decompressed.Add(value);
            }
        }
        else
        {
            while (!reader.IsAtEndOfBuffer())
            {
                decompressed.Add(reader.ReadObj<T>());
            }
        }

        return decompressed.ToArray();
    }

    struct ByteBuffer
    {
        private List<byte> bytes;

        public int Length => bytes.Count;

        public ByteBuffer()
        {
            bytes = new List<byte>();
        }

        public ByteBuffer(byte[] bytes)
        {
            this.bytes = new List<byte>(bytes);
        }

        public ByteBuffer(List<byte> bytes)
        {
            this.bytes = new List<byte>(bytes);
        }

        public void InsertByte(int index, byte value)
        {
            bytes.Insert(index, value);
        }

        public void WriteByte(byte v) => bytes.Add(v);
        public void WriteFloat(float v) => bytes.AddRange(BitConverter.GetBytes(v));
        public void WriteInt16(Int16 v) => bytes.AddRange(BitConverter.GetBytes(v));
        public void WriteInt32(Int32 v) => bytes.AddRange(BitConverter.GetBytes(v));
        public void WriteInt64(Int64 v) => bytes.AddRange(BitConverter.GetBytes(v));
        public void WriteUInt16(UInt16 v) => bytes.AddRange(BitConverter.GetBytes(v));
        public void WriteUInt32(UInt32 v) => bytes.AddRange(BitConverter.GetBytes(v));
        public void WriteUInt64(UInt64 v) => bytes.AddRange(BitConverter.GetBytes(v));
        public void WriteBool(bool v) => bytes.AddRange(BitConverter.GetBytes(v));
        public void WriteChar(char v) => bytes.AddRange(BitConverter.GetBytes(v));
        public void WriteString(string v) => bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(v));

        public void WriteSizedString(string v)
        {
            var stringBytes = System.Text.Encoding.UTF8.GetBytes(v);
            WriteInt32(stringBytes.Length);
            bytes.AddRange(stringBytes);
        }

        public void WriteObj<T>(T v) where T : struct
        {
            byte[] tBytes = StructToBytes(v);
            bytes.AddRange(tBytes);
        }

        public T ReadObj<T>(int position) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            var tBytes = bytes.GetRange(position, size).ToArray();
            return BytesToStruct<T>(tBytes);
        }

        public byte ReadByte(int position) => bytes[position];
        public float ReadFloat(int position) => BitConverter.ToSingle(bytes.ToArray(), position);
        public Int16 ReadInt16(int position) => BitConverter.ToInt16(bytes.ToArray(), position);
        public Int32 ReadInt32(int position) => BitConverter.ToInt32(bytes.ToArray(), position);
        public Int64 ReadInt64(int position) => BitConverter.ToInt64(bytes.ToArray(), position);
        public UInt16 ReadUInt16(int position) => BitConverter.ToUInt16(bytes.ToArray(), position);
        public UInt32 ReadUInt32(int position) => BitConverter.ToUInt32(bytes.ToArray(), position);
        public UInt64 ReadUInt64(int position) => BitConverter.ToUInt64(bytes.ToArray(), position);
        public bool ReadBool(int position) => BitConverter.ToBoolean(bytes.ToArray(), position);
        public char ReadChar(int position) => BitConverter.ToChar(bytes.ToArray(), position);

        public string ReadString(int position, int length) =>
            System.Text.Encoding.UTF8.GetString(bytes.ToArray(), position, length);

        public string ReadSizedString(int position)
        {
            int length = ReadInt32(position);
            return ReadString(position + sizeof(int), length);
        }

        public void ClearBytes() => bytes.Clear();

        public List<byte> GetAllBytes() => new List<byte>(bytes);

        public ByteBufferReader GetReader() => new ByteBufferReader(this);

        static byte[] StructToBytes<T>(T obj) where T : struct
        {
            int size = Marshal.SizeOf(obj);
            byte[] bytes = new byte[size];

            Span<byte> span = bytes;
            MemoryMarshal.Write(span, obj);

            return bytes;
        }

        static T BytesToStruct<T>(byte[] bytes) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));

            if (bytes.Length != size) throw new ArgumentException("Byte array size mismatch");

            return MemoryMarshal.Read<T>(bytes);
        }
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

        public T ReadObj<T>() where T : struct
        {
            var v = _buffer.ReadObj<T>(_readerPosition);
            SkipBytes(Marshal.SizeOf<T>());
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
