using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WallpaperFromVideo
{
    public class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int ATTEMPT_LIMIT = 100;
        private const string IGNORE_VIDEOS_FILE = "IgnoreVideos.txt";
        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        private static readonly List<string> videoExtensions = new List<string>() {
            ".avi", ".avchd", ".flv", ".m4p", ".m4v", ".mkv", ".mp2", ".mp4",
            ".mpeg", ".mpe", ".mpg", ".mpv", ".mov", ".qt", ".webm", ".wmv"};

        private static string wallpaperFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wallpaper.png");
        private static string ffmpegDirectory;

        public static void Main(string[] args)
        {
            // Handle arguments
            if (args.Length == 0)
            {
                ExitWithMessage("Please provide the file/folder path as a command line argument.");
            }
            string path = args[0];
            int searchDepth = 1;
            if (args.Length >= 2 && !int.TryParse(args[1], out searchDepth))
            {
                Console.WriteLine("Invalid search depth. Using default value of 1\n");
            }

            // Find ffmpeg
            string currentDir = Directory.GetCurrentDirectory();
            if (args.Length >= 3 && args[2].EndsWith("ffmpeg.exe"))
            {
                if (File.Exists(Path.Combine(currentDir, args[2]))) //relative path
                {
                    ffmpegDirectory = Path.GetDirectoryName(Path.Combine(currentDir, args[2]));
                }
                else if (args.Length >= 3 && File.Exists(args[2])) //absolute path
                {
                    ffmpegDirectory = Path.GetDirectoryName(args[2]);
                }
            }
            if (ffmpegDirectory == null && File.Exists("ffmpeg.exe"))
            {
                ffmpegDirectory = Directory.GetCurrentDirectory();
            }
            if (ffmpegDirectory == null)
            {
                ExitWithMessage("ffmpeg.exe not found\n"
                    + "It must be in the same directory or its location passed by the 3rd argument.");
            }

            // Get all video files at specified location
            List<string> videoFiles = new List<string>();
            if (File.Exists(path))
            {
                if (!IsVideoFile(path))
                {
                    ExitWithMessage("Unsupported file type(s). Supported types are " + string.Join(", ", videoExtensions));
                }
                videoFiles.Add(path);
            }
            else if (Directory.Exists(path))
            {
                videoFiles = GetVideoFiles(path, searchDepth);
            }
            else
            {
                ExitWithMessage($"{path} is not a valid file or directory.");
            }

            // Do not consider ignored videos
            if (File.Exists(IGNORE_VIDEOS_FILE))
            {
                using (StreamReader reader = new StreamReader(IGNORE_VIDEOS_FILE))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        videoFiles.Remove(line);
                    }
                }
            }

            // Extract an image from a random time in a random video
            string imagePath = ExtractImageFromVideos(videoFiles);

            // Set the desktop wallpaper to the wallpaper.png file
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }

        // Obtain a list of all supported videos in the given folder at a given search depth
        private static List<string> GetVideoFiles(string folderPath, int searchDepth)
        {
            List<string> allFiles = new List<string>();

            try
            {
                allFiles.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                return new List<string>();
            }

            // If search depth is greater than 1, search subfolders
            if (searchDepth > 1)
            {
                string[] subfolders = Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly);

                foreach (string subfolder in subfolders)
                {
                    allFiles.AddRange(GetVideoFiles(subfolder, searchDepth - 1));
                }
            }

            // Filter the list to only include video files
            List<string> videoFiles = new List<string>();
            foreach (string file in allFiles)
            {
                if (IsVideoFile(file))
                {
                    videoFiles.Add(file);
                }
            }

            return videoFiles;
        }

        private static bool IsVideoFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return videoExtensions.Contains(extension);
        }

        // Extracts an image from a random time in a random usable video
        private static string ExtractImageFromVideos(List<string> videoFiles)
        {
            Random rnd = new Random();

            // Choose video files randomly until a usable one is found
            string chosenPath = "";
            TimeSpan videoDuration = TimeSpan.Zero;
            for(int attemptCount = 0; attemptCount < ATTEMPT_LIMIT; attemptCount++) {
                if (videoFiles.Count == 0)
                {
                    ExitWithMessage("No usable video found.\n"
                        + $"Check the specified path and {IGNORE_VIDEOS_FILE} if it exists.");
                }
                chosenPath = videoFiles[rnd.Next(videoFiles.Count)];
                Console.WriteLine(chosenPath);

                // Use ffmpeg to get the duration of the video
                ProcessStartInfo ffmpegInfo = new ProcessStartInfo(Path.Combine(ffmpegDirectory, "ffmpeg"), $"-i \"{chosenPath}\"");
                ffmpegInfo.UseShellExecute = false;
                ffmpegInfo.RedirectStandardError = true;
                Process ffmpegProcess = Process.Start(ffmpegInfo);
                string ffmpegOutput = ffmpegProcess.StandardError.ReadToEnd();
                ffmpegProcess.WaitForExit();

                // Parse the duration from the ffmpeg output
                Regex durationRegex = new Regex(@"Duration: (\d\d:\d\d:\d\d\.\d\d)", RegexOptions.IgnoreCase);
                Match durationMatch = durationRegex.Match(ffmpegOutput);
                if (durationMatch.Success)
                {
                    videoDuration = TimeSpan.Parse(durationMatch.Groups[1].Value);
                    break;
                }
                else
                {
                    Console.WriteLine($"Unreadable video duration :(    Logging above file to {IGNORE_VIDEOS_FILE}\n");
                    LogUnusableFile(chosenPath);
                    videoFiles.Remove(chosenPath);
                }
            }
            if(videoDuration == TimeSpan.Zero) //no usable video found
            {
                ExitWithMessage($"\nNo usable video found after {ATTEMPT_LIMIT} attempts. Giving up...");
            }

            // Choose a random time within the video duration
            double randomSeconds = rnd.NextDouble() * videoDuration.TotalSeconds;
            TimeSpan randomTime = TimeSpan.FromSeconds(randomSeconds);
            Console.WriteLine($"  Selected time {randomTime} ({(int)(100 * randomSeconds / videoDuration.TotalSeconds)}%)\n"
                + $"     from total {videoDuration}");

            // Use ffmpeg to extract an image at the chosen time
            ProcessStartInfo ffmpegExtractInfo = new ProcessStartInfo(Path.Combine(ffmpegDirectory, "ffmpeg"),
                $"-y -ss {randomTime} -i \"{chosenPath}\" -vframes 1 -f image2 \"{wallpaperFilePath}\"");
            ffmpegExtractInfo.UseShellExecute = false;
            ffmpegExtractInfo.RedirectStandardError = true;
            Process ffmpegExtractProcess = Process.Start(ffmpegExtractInfo);
            string errorMessage = ffmpegExtractProcess.StandardError.ReadToEnd();
            ffmpegExtractProcess.WaitForExit();
            if (errorMessage.Contains("Could not open file"))
            {
                ExitWithMessage("Error: ffmpeg was not able to output wallpaper.png\n"
                    + "Please run this program with write permissions to its current directory.");
            }
            return wallpaperFilePath;
        }

        // Log the unuable video so it will not be used in future attempts
        private static void LogUnusableFile(string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), IGNORE_VIDEOS_FILE);
            try
            {
                using (StreamWriter writer = File.AppendText(filePath))
                {
                    writer.WriteLine(fileName);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                ExitWithMessage($"Error writing to {IGNORE_VIDEOS_FILE}: {ex.Message}");
            }
        }

        // Exit the program while leaving a message for the user.
        private static void ExitWithMessage(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine("Press any key to close the console window...");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
