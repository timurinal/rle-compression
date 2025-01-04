# rle-compression
Basic single-file RLE compression in C#

Use however you want, credit is not required but appreciated :)

Works with any C# struct. If you use it with custom structs, make sure to implement `Equals(object? other)` to avoid any expensive boxing

## Usage

```csharp
using Compression;

class Program 
{
	static void Main(string[] args) 
	{
		int[] data = [ 1, 1, 1, 2, 3, 3, 3, 3, 3, 4, 4, 4, 5, 5, 5 ]

		byte[] compressed = Compression.Compress(data);

		int[] decompressed = Compression.Decompress<int>(compressed);
	}
}
```
