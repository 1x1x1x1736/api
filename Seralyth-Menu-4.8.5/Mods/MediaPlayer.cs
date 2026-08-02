using Seralyth.Menu;
using Seralyth.Managers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using static Seralyth.Menu.Main;
using Object = UnityEngine.Object;

namespace Seralyth.Mods
{
    public static class MediaPlayer
    {
        private static AudioSource audioSrc;
        private static bool isPlaying;
        private static readonly string musicFolder = @"C:\Users\kalew\Music\";
        private static string[] musicFiles;
        private static int currentMusic;
        private static AudioClip musicClip;
        private static GameObject musicPlayerObj;

        public static void PlayMusic()
        {
            if (musicPlayerObj == null)
            {
                musicPlayerObj = new GameObject("Seralyth_MusicPlayer");
                audioSrc = musicPlayerObj.AddComponent<AudioSource>();
                audioSrc.spatialBlend = 0f;
                audioSrc.volume = 0.5f;
            }
            if (ScanMusic())
                PlayMusicFile(currentMusic);
        }

        public static void StopMusic()
        {
            if (audioSrc != null) { audioSrc.Stop(); audioSrc.clip = null; }
            if (musicClip != null) { Object.Destroy(musicClip); musicClip = null; }
            if (musicPlayerObj != null) { Object.Destroy(musicPlayerObj); musicPlayerObj = null; }
            isPlaying = false;
        }

        public static void NextMusic()
        {
            if (musicFiles == null || musicFiles.Length == 0) { ScanMusic(); if (musicFiles.Length == 0) return; }
            PlayMusicFile((currentMusic + 1) % musicFiles.Length);
        }

        public static void PrevMusic()
        {
            if (musicFiles == null || musicFiles.Length == 0) { ScanMusic(); if (musicFiles.Length == 0) return; }
            PlayMusicFile((currentMusic - 1 + musicFiles.Length) % musicFiles.Length);
        }

        private static bool ScanMusic()
        {
            var files = new List<string>();
            string[] exts = { "*.mp3", "*.wav", "*.ogg", "*.flac", "*.aac", "*.m4a" };
            foreach (string ext in exts)
            {
                if (Directory.Exists(musicFolder))
                    files.AddRange(Directory.GetFiles(musicFolder, ext));
            }
            musicFiles = files.ToArray();
            if (musicFiles.Length > 0)
            {
                var btn = Buttons.GetIndex("Music Player File:");
                if (btn != null)
                    btn.overlapText = $"Music Player File: <color=grey>[</color><color=green>{Path.GetFileName(musicFiles[currentMusic])}</color><color=grey>]</color>";
            }
            return musicFiles.Length > 0;
        }

        private static void PlayMusicFile(int index)
        {
            if (musicFiles.Length == 0) return;
            currentMusic = index % musicFiles.Length;
            isPlaying = true;
            CoroutineManager.instance.StartCoroutine(LoadMusicFile(musicFiles[currentMusic]));
        }

        private static IEnumerator LoadMusicFile(string path)
        {
            string url = "file:///" + path.Replace("\\", "/");
            AudioType type = GetAudioType(Path.GetExtension(path));
            using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, type);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                musicClip = DownloadHandlerAudioClip.GetContent(req);
                if (musicClip != null)
                {
                    if (audioSrc == null)
                    {
                        musicPlayerObj = new GameObject("Seralyth_MusicPlayer");
                        audioSrc = musicPlayerObj.AddComponent<AudioSource>();
                        audioSrc.spatialBlend = 0f;
                    }
                    audioSrc.clip = musicClip;
                    audioSrc.volume = 0.5f;
                    audioSrc.Play();
                }
            }

            var btn = Buttons.GetIndex("Music Player File:");
            if (btn != null)
                btn.overlapText = $"Music Player File: <color=grey>[</color><color=green>{Path.GetFileName(musicFiles[currentMusic])}</color><color=grey>]</color>";
        }

        private static AudioType GetAudioType(string ext)
        {
            switch (ext.ToLower())
            {
                case ".mp3": return AudioType.MPEG;
                case ".wav": return AudioType.WAV;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".aac": case ".m4a": return AudioType.MPEG;
                default: return AudioType.MPEG;
            }
        }
    }
}
