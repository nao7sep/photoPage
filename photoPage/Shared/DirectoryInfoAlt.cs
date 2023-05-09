using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace photoPage
{
    public class DirectoryInfoAlt
    {
        public DirectoryInfo DirectoryAlt { get; private set; }

        public FileInfoAlt [] Files { get; private set; }

        public DirectoryInfoAlt (string path)
        {
            DirectoryAlt = new DirectoryInfo (path);
            Files = Directory.GetFiles (path, "*.*", SearchOption.AllDirectories).Select (x => new FileInfoAlt (x)).ToArray ();
        }

        public void CompressInto (string filePath)
        {
            using (FileStream xStream = new FileStream (filePath, FileMode.Create))
            using (ZipArchive xArchive = new ZipArchive (xStream, ZipArchiveMode.Create, leaveOpen: false))
            {
                foreach (FileInfoAlt xFile in Files)
                {
                    // 現時点では画像以外をアーカイブに含める想定がないが、一応、画像なら無圧縮と条件分岐

                    if (xFile.IsValidImage)
                        xArchive.CreateEntryFromFile (xFile.FileAlt.FullName, xFile.OriginalName, CompressionLevel.NoCompression);

                    else xArchive.CreateEntryFromFile (xFile.FileAlt.FullName, xFile.OriginalName);
                }
            }
        }
    }
}
