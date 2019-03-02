using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace static_npm
{
    public class PackageArchive
    {

        public void GetPackageJson(FileInfo file)
        {
            using var inStream = file.OpenRead();
            using var gzipStream = new GZipInputStream(inStream);

            using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream);

            tarArchive.ExtractContents(@"C:\tt");
        }
    }
}
