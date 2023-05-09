using System.IO;

namespace photoPage
{
    internal class Program
    {
        private static string mOneLevelOfIndentation = new string ('\x20', 4);

        static void Main (string [] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine ("処理する画像ファイルの含まれる一つ以上のディレクトリーをプログラムの実行ファイルにドラッグ＆ドロップしてください。");
                    goto End;
                }

                foreach (string xPath in args)
                {
                    if (Path.IsPathFullyQualified (xPath) == false || Directory.Exists (xPath) == false)
                    {
                        Console.WriteLine ("パラメーターを認識できません: " + xPath);
                        continue;
                    }

                    DirectoryInfoAlt xSourceDirectory = new DirectoryInfoAlt (xPath);

                    if (xSourceDirectory.Files.Any (x => x.IsValidImage == false))
                    {
                        Console.WriteLine ("画像として読み込めないファイルが含まれています: " + xPath);
                        continue;
                    }

                    if (xSourceDirectory.Files.Any (x => x.IsValidImage) == false)
                    {
                        Console.WriteLine ("画像ファイルが一つも含まれていません: " + xPath);
                        continue;
                    }

                    if (xSourceDirectory.Files.DistinctBy (x => x.OriginalName, StringComparer.OrdinalIgnoreCase).Count () != xSourceDirectory.Files.Count ())
                    {
                        Console.WriteLine ("同じ名前のファイルが含まれています: " + xPath);
                        continue;
                    }

                    if (xSourceDirectory.Files.Any (x => x.ContainsExifDateTaken == false))
                    {
                        Console.WriteLine ("次の画像ファイルには Exif の DateTaken が含まれていません:");

                        Console.WriteLine (mOneLevelOfIndentation + string.Join ($"{Environment.NewLine}{mOneLevelOfIndentation}",
                            xSourceDirectory.Files.Where (x => x.ContainsExifDateTaken == false).Select (y => y.FileAlt.FullName).OrderBy (z => z, StringComparer.OrdinalIgnoreCase)));
                    }

                    // =============================================================================

                    string xTargetDirectoryPath;

                    while (true)
                    {
                        // サーバーにアップロードする前に必ずディレクトリー名を変更するため、ここでは適当でいい
                        // 接頭辞なども不要
                        xTargetDirectoryPath = Path.Join (Environment.GetFolderPath (Environment.SpecialFolder.DesktopDirectory), Guid.NewGuid ().ToString ("D"));

                        if (Directory.Exists (xTargetDirectoryPath) == false && File.Exists (xTargetDirectoryPath) == false)
                            break;
                    }

                    Directory.CreateDirectory (xTargetDirectoryPath);

                    // =============================================================================

                    string xTempArchiveFileName = "●.zip",
                        xTempArchiveFilePath = Path.Join (xTargetDirectoryPath, xTempArchiveFileName);

                    xSourceDirectory.CompressInto (xTempArchiveFilePath);

                    Console.WriteLine ("圧縮ファイルが作られました: " + xTempArchiveFilePath);

                    // =============================================================================

                    string xOriginalDirectoryPath = Path.Join (xTargetDirectoryPath, "Original");
                    Directory.CreateDirectory (xOriginalDirectoryPath);

                    string xResizedDirectoryPath = Path.Join (xTargetDirectoryPath, "Resized");
                    Directory.CreateDirectory (xResizedDirectoryPath);

                    int xMaxWidthAndHeight = 720,
                        xQuality = 75,
                        xHandledImageCount = 0,
                        xTotalImageCount = xSourceDirectory.Files.Count (); // この時点では全てが画像ファイル

                    foreach (FileInfoAlt xFile in xSourceDirectory.Files)
                    {
                        xFile.FileAlt.CopyTo (Path.Join (xOriginalDirectoryPath, xFile.OriginalName));

                        xFile.ResizeAndSaveImage (xMaxWidthAndHeight, xMaxWidthAndHeight, xQuality, Path.Join (xResizedDirectoryPath, xFile.OriginalName));

                        Console.Write (FormattableString.Invariant ($"\r画像を処理しています: {++ xHandledImageCount}/{xTotalImageCount}"));
                    }

                    Console.WriteLine ();
                }
            }

            catch (Exception xException)
            {
                Console.WriteLine ("エラーが発生しました:");

                // 雑な実装だが、今のところ十分
                // \r と \n で分割し、エントリーのトリミングおよび空のエントリーの削除により行分割
                // 入れ子の例外メッセージをまず見ないため、1行目に半角空白四つ、2行目以降に八つを入れてインデント

                Console.WriteLine (mOneLevelOfIndentation + string.Join ($"{Environment.NewLine}{mOneLevelOfIndentation}{mOneLevelOfIndentation}",
                    xException.ToString ().Split (new [] { '\r', '\n' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)));
            }

        End:
            Console.Write ("このウィンドウを閉じるには、任意のキーを押してください: ");
            Console.ReadKey (true);
            Console.WriteLine ();
        }
    }
}
