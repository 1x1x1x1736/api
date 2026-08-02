using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Seralyth.Managers;
using static Seralyth.Menu.Main;
using Object = UnityEngine.Object;

namespace Seralyth.Mods
{
    public static class Boombox
    {
        private static GameObject boomboxObj;
        private static AudioSource audioSource;
        private static AudioClip musicClip;
        private static bool musicLoaded;

        public static void BoomboxUpdate()
        {
            if (boomboxObj == null)
            {
                boomboxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boomboxObj.name = "Seralyth_Boombox";
                boomboxObj.transform.localScale = new Vector3(0.5f, 0.3f, 0.2f);
                Object.Destroy(boomboxObj.GetComponent<Collider>());

                Renderer r = boomboxObj.GetComponent<Renderer>();
                string texPath = @"C:\Users\kalew\OneDrive\Pictures\download.jfif";
                if (File.Exists(texPath))
                {
                    byte[] imgBytes = File.ReadAllBytes(texPath);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(imgBytes);
                    r.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    r.material.SetTexture("_BaseMap", tex);
                }
                else
                {
                    r.material = new Material(Shader.Find("GorillaTag/UberShader"));
                }
                r.material.color = Color.white;

                audioSource = boomboxObj.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.loop = true;
                audioSource.volume = 0.5f;

                if (!musicLoaded)
                {
                    string musicPath = @"C:\Users\kalew\OneDrive\Pictures\music.ogg";
                    if (File.Exists(musicPath))
                        CoroutineManager.instance.StartCoroutine(LoadMusic(musicPath));
                }
            }

            Transform leftHand = GorillaTagger.Instance.leftHandTransform;
            boomboxObj.transform.position = leftHand.position;
            boomboxObj.transform.rotation = leftHand.rotation;
        }

        public static void DisableBoombox()
        {
            if (audioSource != null) { audioSource.Stop(); audioSource = null; }
            if (musicClip != null) { Object.Destroy(musicClip); musicClip = null; }
            if (boomboxObj != null) { Object.Destroy(boomboxObj); boomboxObj = null; }
            musicLoaded = false;
        }

        private static IEnumerator LoadMusic(string path)
        {
            string url = "file:///" + path;
            AudioType type = GetAudioType(Path.GetExtension(path));
            using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, type);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                musicClip = DownloadHandlerAudioClip.GetContent(req);
                if (musicClip != null && audioSource != null)
                {
                    audioSource.clip = musicClip;
                    audioSource.Play();
                    musicLoaded = true;
                }
            }
        }

        private static AudioType GetAudioType(string ext)
        {
            switch (ext.ToLower())
            {
                case ".mp3": return AudioType.MPEG;
                case ".wav": return AudioType.WAV;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".aac":
                case ".m4a": return AudioType.MPEG;
                default: return AudioType.MPEG;
            }
        }
    }
}
