using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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

        public void GeneratePage (string filePath, string archiveFileName, string originalDirectoryName, string resizedDirectoryName)
        {
            StringBuilder xBuilder = new StringBuilder ();

            xBuilder.AppendLine ("<html>");
            xBuilder.AppendLine ("<head>");
            xBuilder.AppendLine ("<title>●</title>");
            xBuilder.AppendLine ("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>");

            // div 同士の上下マージンの打ち消しをシンプルに行うため、入れ子なしのフラットな構造に
            // 画面が狭くなると画像の左右が両端に接してほしいので、margin を 10px auto に
            // 各部の値が決め打ちなのは、時間がなくての手抜き
            // title, message, download, image を並べた上、title の上と image の下に 10px の padding を追加
            // とするなら、それら三つを div.info などに入れての上下 margin の方が合理的かもしれないが、
            //     ブラウザーごとのフォントの設定などにも影響されるため、まずは各 div の padding を調整していく
            // img に display: block を入れるのは、Firefox で画像のすぐ下に余計な空白を入れないため

            // html - Firefox adds extra space after image in div (or makes divs bigger) - Stack Overflow
            // https://stackoverflow.com/questions/25361598/firefox-adds-extra-space-after-image-in-div-or-makes-divs-bigger

            // box-sizing - CSS: Cascading Style Sheets | MDN
            // https://developer.mozilla.org/en-US/docs/Web/CSS/box-sizing

            xBuilder.AppendLine ("<style>");
            xBuilder.AppendLine ("body { margin: 0; font-family: sans-serif }");
            xBuilder.AppendLine ("div.title, div.message, div.download, div.image { margin: 10px auto; max-width: 1280px }");
            xBuilder.AppendLine ("div.title { padding: 10px 10px 0 10px; line-height: 1.5em; font-size: 1.5em }");
            xBuilder.AppendLine ("div.message { padding: 0 10px; line-height: 1.5em; font-size: 1em }");
            xBuilder.AppendLine ("div.download { padding: 0 10px 10px 10px; line-height: 1.5em; font-size: 1em }");
            xBuilder.AppendLine ("div.image img { display: block; width: 100% }");
            xBuilder.AppendLine ("</style>");

            xBuilder.AppendLine ("</head>");
            xBuilder.AppendLine ("<body>");
            xBuilder.AppendLine ("<div class=\"title\">●</div>");
            xBuilder.AppendLine ("<div class=\"message\">●</div>");
            xBuilder.AppendLine ("<div class=\"download\">");
            xBuilder.AppendLine ($"<a href=\"{WebUtility.HtmlEncode (archiveFileName)}\">●</a>");
            xBuilder.AppendLine ("</div>");

            foreach (FileInfoAlt xFile in Files.OrderBy (x => x.LocalDateTaken))
            {
                xBuilder.AppendLine ("<div class=\"image\">");
                xBuilder.AppendLine ($"<a href=\"{WebUtility.HtmlEncode ($"{originalDirectoryName}/{xFile.OriginalName}")}\" target=\"_blank\">");
                xBuilder.AppendLine ($"<img src=\"{WebUtility.HtmlEncode ($"{resizedDirectoryName}/{xFile.OriginalName}")}\"/>");
                xBuilder.AppendLine ("</a>");
                xBuilder.AppendLine ("</div>");
            }

            xBuilder.AppendLine ("</body>");
            xBuilder.AppendLine ("</html>");

            File.WriteAllText (filePath, xBuilder.ToString (), Encoding.UTF8);
        }
    }
}
