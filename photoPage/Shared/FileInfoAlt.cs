using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace photoPage
{
    public class FileInfoAlt
    {
        public FileInfo FileAlt { get; private set; }

        public FileInfoAlt (string path)
        {
            FileAlt = new FileInfo (path);
        }

        private string? mOriginalName = null;

        public string OriginalName
        {
            get
            {
                if (mOriginalName == null)
                {
                    // 自分のファイル管理では、「ローカル日時のタイムスタンプ＋括弧内に元のファイル名」というのが基本
                    // タイムスタンプをつけるのは、複数のカメラで撮られた写真を時系列的に並べるため
                    // ローカル日時なのは、Exif に入っているのがローカル日時だから

                    Match xMatch = Regex.Match (Path.GetFileNameWithoutExtension (FileAlt.Name), @"^[0-9]{8}-[0-9]{6} \((?<OriginalNameWithoutExtension>.+)\)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

                    if (xMatch.Success)
                    {
                        // 元のファイル名のうち拡張子以外の部分を取得し、それを大文字にしても変化がないなら、拡張子も大文字にする
                        // というのは雑な実装で、数字、記号、ASCII 外の文字のみのファイル名に弱いが、
                        //     自分の知るカメラは、携帯のアプリも含めて全てが「大文字＋数字や記号」のファイル名

                        string xOriginalNameWithoutExtension = xMatch.Result ("${OriginalNameWithoutExtension}");

                        mOriginalName = xOriginalNameWithoutExtension +
                            (xOriginalNameWithoutExtension.ToUpperInvariant ().Equals (xOriginalNameWithoutExtension, StringComparison.Ordinal) ?
                                FileAlt.Extension.ToUpperInvariant () : FileAlt.Extension);
                    }

                    else mOriginalName = FileAlt.Name;
                }

                return mOriginalName;
            }
        }

        private void iLoadAsImage ()
        {
            try
            {
                // C#: Read Image Metadata Using BitmapMetadata Class
                // https://dukesoftware00.blogspot.com/2014/09/c-read-image-metadata-using.html

                // 画像の縮小に Magick.NET を使うが、Exif は WPF の機能で読むのに慣れている
                // ちゃんと調べたことがないが、WPF で何万と読んでも実用的な処理時間だった記憶がある

                using (FileStream xStream = FileAlt.OpenRead ()) // ファイルがなければ、ここで例外が飛ぶ
                {
                    BitmapFrame xFrame = BitmapFrame.Create (xStream);
                    BitmapMetadata xMetadata = (BitmapMetadata) xFrame.Metadata; // 画像なら、DateTaken がなくても、ここまでは成功する

                    mIsValidImage = true;

                    // DateTaken は、DateTime で得られた値を引数なしで ToString して返してくれる男前な仕様になっている
                    // 試したところ、Thread.CurrentThread.CurrentCulture により、得られる文字列の書式が変わった
                    // また、Nullable でないのに null をサクッと返してくることの遊び心も素晴らしい

                    // BitmapMetadata.cs
                    // https://source.dot.net/#PresentationCore/System/Windows/Media/Imaging/BitmapMetadata.cs

                    if (string.IsNullOrEmpty (xMetadata.DateTaken)) // DateTaken がない
                        mLocalDateTaken = FileAlt.LastWriteTime;

                    else
                    {
                        Task.Run (() =>
                        {
                            // 別スレッドの CurrentCulture を変更し、DateTaken の文字列の生成とそのパーズの両方を InvariantCulture で行う
                            // メインのスレッドの CurrentCulture を変更するのは作法としてダメな気がするため、非効率的だがファイルごとにスレッドを用意
                            // ToString が引数なしで行われるため、TryParse も引数なしで

                            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                            if (DateTime.TryParse (xMetadata.DateTaken, out DateTime xResult))
                                mLocalDateTaken = xResult;

                            else
                            {
#if DEBUG
                                // WPF のコードがいったん DateTime にできたもののラウンドトリップなので、まず失敗しない
                                // 別スレッドに移るが、Wait するので実質シングルスレッドのようなもので、エラーの原因にはならない

                                Console.WriteLine ("んなこたない: " + FileAlt.FullName);
#endif
                                mLocalDateTaken = FileAlt.LastWriteTime;
                            }
                        }).
                        Wait ();
                    }
                }
            }

            catch
            {
                mIsValidImage = false;
                mLocalDateTaken = default;
            }
        }

        private bool? mIsValidImage = null;

        public bool IsValidImage
        {
            get
            {
                if (mIsValidImage == null)
                    iLoadAsImage ();

                return mIsValidImage!.Value;
            }
        }

        private DateTime? mLocalDateTaken = null;

        /// <summary>
        /// IsValidImage が false なら default (DateTime)
        /// </summary>
        public DateTime LocalDateTaken
        {
            get
            {
                if (mLocalDateTaken == null)
                    iLoadAsImage ();

                return mLocalDateTaken!.Value;
            }
        }

        public void ResizeAndSaveImage (int maxWidth, int maxHeight, int quality, string filePath)
        {
            // Magick.NET/Readme.md at main · dlemstra/Magick.NET · GitHub
            // https://github.com/dlemstra/Magick.NET/blob/main/docs/Readme.md

            // Magick.NET/ReadingImages.md at main · dlemstra/Magick.NET · GitHub
            // https://github.com/dlemstra/Magick.NET/blob/main/docs/ReadingImages.md

            // Resize, InterpolativeResize, AdaptiveResize について調べた

            // まず、一般的には 2x2 の bilinear より 4x4 の bicubic の方が優れるとされる
            // Magick.net では、前者は bilinear、後者は catrom とされ、bilinear がデフォルト
            // 後者については generally imprecisely known as 'BiCubic' interpolation と

            // 実際にやってみると、Resize の結果が一番良く、
            //     他では、たとえば人によっては左右の目の大きさが倍ほど違うこともあった
            // 結果の悪さは、自分あるいは Magick.NET の実装に問題はないかと思うほど
            // Magick.NET のメソッドをそのまま呼んでいたので、可能性としては後者の方が高い

            // ネットのサンプルコードは、多数のサイトにおいて Resize のみ使う
            // 1) ほかの結果が良くない、2) よく使われている、3) 結果に特段の不満がない、
            //     の三つの理由により、自分も Resize で様子見

            // Magick.NET/ResizeImage.md at main · dlemstra/Magick.NET · GitHub
            // https://github.com/dlemstra/Magick.NET/blob/main/docs/ResizeImage.md

            // Resizing or Scaling -- ImageMagick Examples
            // https://imagemagick.org/Usage/resize/

            // Understanding Digital Image Interpolation
            // https://www.cambridgeincolour.com/tutorials/image-interpolation.htm

            // ImageMagick – Command-line Options
            // https://imagemagick.org/script/command-line-options.php#interpolate

            // Miscellaneous -- ImageMagick Examples
            // https://imagemagick.org/Usage/misc/#catrom

            // c# - How to adjust jpeg quality with Magick.Net - Stack Overflow
            // https://stackoverflow.com/questions/19884486/how-to-adjust-jpeg-quality-with-magick-net

            using (MagickImage xImage = new MagickImage (FileAlt))
            {
                xImage.Resize (maxWidth, maxHeight);
                xImage.Format = MagickFormat.Jpeg;
                xImage.Quality = quality;
                xImage.Write (filePath);
            }
        }
    }
}
