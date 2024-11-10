# rle-compression
Basic single-file RLE compression in C#

Use however you want, credit is not required but appreciated :)

Currently only works with `int[]` however I'm working on making work with generic types

## Usage

```csharp
using TInal;

class Program 
{
	static void Main(string[] args) 
	{
		int[] data = [ 1, 1, 1, 2, 3, 3, 3, 3, 3, 4, 4, 4, 5, 5, 5 ]

		byte[] compressed = Compression.Compress(data);

		int[] decompressed = Compression.Decompress(compressed);
	}
}
```
