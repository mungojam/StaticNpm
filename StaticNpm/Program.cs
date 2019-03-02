using System;
using System.IO;
using System.IO.Compression;

namespace StaticNpm
{
    class Program
    {
        static void Main(string[] args)
        {
            var zipPath = @"C:\Users\mark\Dropbox\marks\Desktop\react-redux-6.0.1.tgz";

            var extractPath = @"C:\Users\mark\source\repos\static-npm\tt";

            // Normalizes the path.
            extractPath = Path.GetFullPath(extractPath);

            // Ensures that the last character on the extraction path
            // is the directory separator char. 
            // Without this, a malicious zip file could try to traverse outside of the expected
            // extraction path.
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractPath += Path.DirectorySeparatorChar;

            Decompress(new FileInfo(zipPath));
        }

        public static void Decompress(FileInfo fileToDecompress)
        {
            using (var originalFileStream = fileToDecompress.OpenRead())
            {
                var currentFileName = fileToDecompress.FullName;
                var newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (var decompressedFileStream = File.Create(newFileName))
                {
                    using (var decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine($"Decompressed: {fileToDecompress.Name}");
                    }
                }
            }
        }
    }
}
