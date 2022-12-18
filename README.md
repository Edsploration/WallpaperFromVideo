# WallpaperFromVideo
A command-line tool that extracts a random frame from a specified video, or a random video in a specified folder, and sets that image to the Windows desktop wallpaper.
1. Point it to your favorite cinematic video files.
2. Bind it to startup, a hotkey, or a timer.
3. Enjoy endless fresh wallpapers!

![six example screenshots](https://github.com/Edsploration/i/blob/main/WallpaperFromVideo/examples.png)

## Requirements

- [FFmpeg](https://ffmpeg.org/download.html#build-windows)
  - Place ffmpeg.exe in the same directory as the executable.
  - Or point to your copy ffmpeg.exe with the 3rd launch option when running this program.
- .NET Framework 4.8.1 (included in Windows 10 & 11)

## Command Line
```WallpaperFromVideo.exe [input-path] [search-depth] [path-to-ffmpeg]```

Run the program with 1, 2, or all 3 arguments in order:
1. [input-path] The video file or folder containing video files to extract a random image from.
2. [search-depth] If a folder is provided for input, the search depth.
   - `1` for just the base folder
   - `2` to include immediate subfolders
   - `3` to include subfolders of subfolders
   - etc.
3. [path-to-ffmpeg] The absolute or relative path to ffmpeg.exe, if it is not included in the same directory as the executable.
### Extract a random frame from a specific video
```
WallpaperFromVideo.exe "C:\path\to\video.mp4"
```
### Use a random video in a specific folder
```
WallpaperFromVideo.exe "C:\path\to\folder"
```
### Include subfolders up to a specific search depth (default of 1)
```
WallpaperFromVideo.exe "C:\path\to\folder" 3
```
### Use ffmpeg in a separate folder
```
WallpaperFromVideo.exe "C:\path\to\folder" 1 "C:\path\to\ffmpeg.exe"
```
## Launch with a Click
- An example .bat file is included and must be edited to be used.
- A shortcut file can also be used by adding the launch options to its Target field.

## Suggested Usages
### Startup
Drop the batch file or a shortcut file into `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp`
### Hotkey
Using `AutoHotkey` bind a key with a command such as `KEY::Run, C:\path\to\WallpaperFromVideo.exe "C:\path\to\folder"`
### Timer
Make a task using Windows `Task Scheduler`

## Additional Notes

- The program is portable. As such it must have write permission to its own directory when run.
- The file 'IgnoreVideos.txt' may be created in the same directory as the executable to list any videos that could not be read.
  - Create this file and add lines to it manually if you want some files to be skipped. The complete path to the files must be written, one per line.
- The extracted frame will be saved in the same directory as the executable with the name `wallpaper.png`.
- Only video files with the following extensions will be considered: `.avi`, `.avchd`, `.flv`, `.m4p`, `.m4v`, `.mkv`, `.mp2`, `.mp4`, `.mpeg`, `.mpe`, `.mpg`, `.mpv`, `.mov`, `.qt`, `.webm`, `.wmv`