using GorillaLocomotion;
using Seralyth.Classes.Menu;
using Seralyth.Extensions;
using Seralyth.Managers;
using Seralyth.Menu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Seralyth.Menu.Main;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Seralyth.Mods
{
    public static class Disco
    {
        private static List<GameObject> discoLights;
        private static float discoLightHue;

        public static void DiscoLights()
        {
            if (discoLights == null)
            {
                discoLights = new List<GameObject>();
                for (int i = 0; i < 8; i++)
                {
                    GameObject lightObj = new GameObject("Seralyth_DiscoLight_" + i);
                    Light light = lightObj.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 8f;
                    light.intensity = 2f;
                    light.shadows = LightShadows.None;
                    discoLights.Add(lightObj);
                }
            }

            discoLightHue += Time.deltaTime * 0.05f;
            if (discoLightHue > 1f) discoLightHue -= 1f;

            Vector3 pos = GorillaTagger.Instance.headCollider.transform.position;
            for (int i = 0; i < discoLights.Count; i++)
            {
                float angle = Time.time * (40f + i * 20f) + i * 0.785f;
                float height = Mathf.Sin(Time.time * (0.5f + i * 0.15f) + i) * 1.5f;
                float radius = 2f + i * 0.3f;
                discoLights[i].transform.position = pos + new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                    height + 1f,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius
                );
                discoLights[i].GetComponent<Light>().color = Color.HSVToRGB(
                    (discoLightHue + i * 0.125f) % 1f, 1f, 1f
                );
            }
        }

        public static void FixDiscoLights()
        {
            if (discoLights != null)
            {
                foreach (var l in discoLights)
                    Object.Destroy(l);
                discoLights = null;
            }
        }

        private static List<GameObject> floorTiles;
        private static float floorHue;

        public static void DiscoFloor()
        {
            if (floorTiles == null)
            {
                floorTiles = new List<GameObject>();
                for (int x = -4; x <= 4; x++)
                {
                    for (int z = -4; z <= 4; z++)
                    {
                        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        tile.name = "Seralyth_DiscoFloor";
                        tile.transform.localScale = new Vector3(0.45f, 0.05f, 0.45f);
                        Object.Destroy(tile.GetComponent<Collider>());
                        floorTiles.Add(tile);
                    }
                }
            }

            floorHue += Time.deltaTime * 0.03f;
            if (floorHue > 1f) floorHue -= 1f;

            Vector3 basePos = GorillaTagger.Instance.bodyCollider.transform.position;
            basePos.y = GorillaTagger.Instance.bodyCollider.transform.position.y - 0.1f;

            int idx = 0;
            for (int x = -4; x <= 4; x++)
            {
                for (int z = -4; z <= 4; z++)
                {
                    Vector3 tilePos = basePos + new Vector3(x * 0.5f, 0f, z * 0.5f);
                    floorTiles[idx].transform.position = tilePos;
                    float dist = Mathf.Sqrt(x * x + z * z);
                    Color c = Color.HSVToRGB(
                        (floorHue + dist * 0.1f) % 1f, 1f, 1f
                    );
                    floorTiles[idx].GetComponent<Renderer>().material.color = c;
                    floorTiles[idx].GetComponent<Renderer>().material.shader = Shader.Find("GorillaTag/UberShader");
                    idx++;
                }
            }
        }

        public static void FixDiscoFloor()
        {
            if (floorTiles != null)
            {
                foreach (var t in floorTiles)
                    Object.Destroy(t);
                floorTiles = null;
            }
        }

        private static GameObject discoBall;
        private static Light discoBallSpotlight;

        public static void DiscoBall()
        {
            if (discoBall == null)
            {
                discoBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                discoBall.name = "Seralyth_DiscoBall";
                discoBall.transform.localScale = Vector3.one * 0.4f;
                Object.Destroy(discoBall.GetComponent<Collider>());
                Renderer r = discoBall.GetComponent<Renderer>();
                byte[] imgBytes = File.ReadAllBytes(@"C:\Users\kalew\OneDrive\Pictures\Screenshots\d8cb5dbe-541d-4738-895c-432900a2f706.png");
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(imgBytes);
                tex.wrapMode = TextureWrapMode.Clamp;
                r.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                r.material.SetTexture("_BaseMap", tex);
                r.material.color = Color.white;

                discoBallSpotlight = new GameObject("Seralyth_DiscoSpotlight").AddComponent<Light>();
                discoBallSpotlight.type = LightType.Spot;
                discoBallSpotlight.range = 15f;
                discoBallSpotlight.intensity = 3f;
                discoBallSpotlight.spotAngle = 60f;
                discoBallSpotlight.shadows = LightShadows.None;
            }

            Vector3 headPos = GorillaTagger.Instance.headCollider.transform.position;
            Vector3 ballPos = headPos + Vector3.up * 2.5f;
            discoBall.transform.position = ballPos;
            discoBall.transform.Rotate(Vector3.up, 120f * Time.deltaTime);

            discoBallSpotlight.transform.position = ballPos;
            discoBallSpotlight.transform.rotation = Quaternion.Euler(
                Mathf.Sin(Time.time * 0.5f) * 30f,
                Time.time * 100f,
                0f
            );
            discoBallSpotlight.color = Color.HSVToRGB(
                (Time.time * 0.04f) % 1f, 1f, 1f
            );

        }

        public static void FixDiscoBall()
        {
            if (discoBall != null)
            {
                Object.Destroy(discoBall);
                discoBall = null;
            }
            if (discoBallSpotlight != null)
            {
                Object.Destroy(discoBallSpotlight.gameObject);
                discoBallSpotlight = null;
            }
        }

        public static void PartyMode()
        {
            DiscoLights();
            DiscoFloor();
            DiscoBall();
        }

        public static void FixPartyMode()
        {
            FixDiscoLights();
            FixDiscoFloor();
            FixDiscoBall();
        }
    }
}
