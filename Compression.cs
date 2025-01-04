using System.Runtime.InteropServices;

namespace Compression;

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

        // If uncompressed is smaller, store uncompressed data.
        if (data.Length * sizeof(int) < buffer.Length)
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
            // Prepend a '1' to mark the data as compressed
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
                {
                    decompressed.Add(value);
                }
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

        public void InsertByte(int index, byte value)
        {
            bytes.Insert(index, value);
        }

        public void WriteByte(byte v) => bytes.Add(v);

        public void WriteObj<T>(T v) where T : struct
        {
            byte[] tBytes = StructToBytes(v);
            bytes.AddRange(tBytes);
        }

        public void ClearBytes() => bytes.Clear();

        public List<byte> GetAllBytes() => new List<byte>(bytes);

        public T ReadObj<T>(int position) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            var tBytes = bytes.GetRange(position, size).ToArray();
            return BytesToStruct<T>(tBytes);
        }

        public byte ReadByte(int position) => bytes[position];

        static byte[] StructToBytes<T>(T obj) where T : struct
        {
            int size = Marshal.SizeOf(obj);
            byte[] result = new byte[size];

            Span<byte> span = result;
            MemoryMarshal.Write(span, ref obj);

            return result;
        }

        static T BytesToStruct<T>(byte[] data) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            if (data.Length != size)
                throw new ArgumentException("Byte array size mismatch.");

            return MemoryMarshal.Read<T>(data);
        }
    }

    class ByteBufferReader
    {
        private readonly ByteBuffer _buffer;
        private int _readerPosition;

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

        public T ReadObj<T>() where T : struct
        {
            var v = _buffer.ReadObj<T>(_readerPosition);
            SkipBytes(Marshal.SizeOf<T>());
            return v;
        }

        public bool IsAtEndOfBuffer()
        {
            return _readerPosition >= _buffer.Length;
        }

        private void SkipBytes(int count) => _readerPosition += count;
    }
}
