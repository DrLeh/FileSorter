using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileSorter
{
    class Program
    {
        public const string TVLocation = @"\\synology\video\TV";
        public const string MovieLocation = @"\\synology\video\Movies";
        public const string MusicLocation = @"\\synology\Music";
        public const string BookLocation = @"\\synology\root\Books";

        //public const string TVLocation = @"c:\Users\devon\desktop\test\Videos\TV";
        //public const string MovieLocation = @"c:\Users\devon\desktop\test\Videos\Movies";
        //public const string MusicLocation = @"c:\Users\devon\desktop\test\Music";
        //public const string BookLocation = @"c:\Users\devon\desktop\test\Book";

        public static readonly List<string> VideoExtensions = new List<string> { ".avi", ".mkv", ".mp4", };
        public static readonly List<string> MusicExtensions = new List<string> { ".mp3", ".occ", ".flac", };
        public static readonly List<string> BookExtensions = new List<string> { ".epub",  };

        static void Main(string[] args)
        {
            new Program();
        }

        public string BaseDirectory { get; set; }
        public Program()
        {
            BaseDirectory = @"\\synology\root\downloads";
            ProcessFiles(BaseDirectory);
        }

        public void ProcessFiles(string dir)
        {
            foreach (var file in Directory.EnumerateFiles(dir))
                ProcessFile(file);

            foreach (var dir2 in Directory.EnumerateDirectories(dir))
                ProcessFiles(dir2);
        }

        void ProcessFile(string file)
        {
            var extension = Path.GetExtension(file);

            if (MusicExtensions.Contains(extension))
                ProcessMusic(file);
            else if (VideoExtensions.Contains(extension))
                ProcessVideo(file);
            else if (BookExtensions.Contains(extension))
                Move(file, Path.Combine(BookLocation, Path.GetFileName(file)));
            else
                Console.WriteLine($"Unknown file type: {Path.GetFileName(file)}");
        }

        void ProcessMusic(string file)
        {
            var fileName = Path.GetFileName(file);
            var directory = Path.GetDirectoryName(file);
            if (directory == BaseDirectory)
            {
                Console.WriteLine($"Music file {fileName} found in base dir, don't know what folder to put it in");
            }
            else
            {
                var f = TagLib.File.Create(file);
                var tag = f.Tag;
                if (tag == null)
                {
                    Console.WriteLine($"Couldn't parse tag for {fileName}");
                    return;
                }

                var album = tag.Album;
                var artist = tag.Artists.FirstOrDefault() ?? tag.FirstAlbumArtist;
                if (album == null || artist == null)
                {
                    Console.WriteLine($"Couldn't find artist or album for {fileName}");
                    return;
                }

                var dir = Path.Combine(MusicLocation, artist, album);
                var newFile = Path.Combine(dir, fileName);

                Console.WriteLine($"Music file {fileName} moving to {artist}/{album}");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                Move(file, newFile);
            }
        }

        void ProcessVideo(string file)
        {
            if (IsTV(file))
                ProcessTV(file);
            else
                ProcessMovie(file);
        }

        void ProcessTV(string file)
        {
            var fileName = Path.GetFileName(file);
            var parentFolder = Path.GetDirectoryName(file);
            if (parentFolder == BaseDirectory)
            {
                Console.WriteLine($"Couldn't find parent folder to use for: {file}");
                return;
            }
            var showName = Path.GetFileName(parentFolder);
            var destination = Path.Combine(TVLocation, showName, fileName);

            Console.WriteLine($"Treating {fileName} as tv show: {showName}, moving to {destination}");
            Move(file, destination);
        }

        void ProcessMovie(string file)
        {
            var fileName = Path.GetFileName(file);
            string destination = Path.Combine(MovieLocation, fileName);

            Console.WriteLine($"Treating {fileName} as movie, moving");
            Move(file, destination);
        }

        bool IsTV(string file)
        {
            var regex = new Regex("[sS][0-9]*[eE][0-9]*");
            var isTV = file.Contains("season", StringComparison.OrdinalIgnoreCase)
                || file.Contains("episode", StringComparison.OrdinalIgnoreCase)
                || file.Contains("hdtv", StringComparison.OrdinalIgnoreCase)
                //todo: more
                || regex.IsMatch(file)
                ;

            return isTV;
        }

        public void Move(string from, string to)
        {
            var dir = Path.GetDirectoryName(to);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            bool copy = false;
            if (!copy)
            {
                if (File.Exists(to))
                    File.Delete(to);
                else
                {
                    File.Move(from, to);
                    if (File.Exists(to))
                        File.Delete(to);
                }
            }
            else
                File.Copy(from, to, true);
        }
    }

    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
    }
}
