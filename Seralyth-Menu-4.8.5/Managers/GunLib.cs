using ExitGames.Client.Photon;
using GorillaExtensions;
using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using static Seralyth.Menu.Main;

namespace Seralyth.Managers
{
    public class GunLib : MonoBehaviour
    {
        public static GunLibData data = new GunLibData();

        public class GunLibData
        {
            public bool IsGripping { get; set; }
            public bool IsTriggered { get; set; }
            public Vector3 HitPos { get; set; }
            public VRRig LockedRig { get; set; }
            public VRRig LastLockedRig { get; set; }
            public bool GunReady { get; set; }
            public Collider Collider { get; set; }

            public GunLibData(bool gripped = false, bool triggered = false, Vector3 hitpos = default, VRRig player = null, VRRig lastPlr = null, bool gunReady = false, Collider cPoint = null)
            {
                IsGripping = gripped;
                IsTriggered = triggered;
                HitPos = hitpos;
                LockedRig = player;
                LastLockedRig = lastPlr;
                GunReady = gunReady;
                Collider = cPoint;
            }
        }

        public void Start()
        {
            InitpObjs();
            Default = backgroundColor.GetColor(0);
            Selected = buttonColors[1].GetColor(0);
        }

        public static void ResetGL()
        {
            pObj?.SetActive(false);
            HideLockShape();
            if (PhotonNetwork.InRoom)
                PhotonNetwork.RaiseEvent(22, null, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendUnreliable);
        }

        #region ptr declaration & vars
        public static GameObject pObj;
        public static LineRenderer gunLine;
        public static Vector3 determinePos, endPoint;
        public static Material pColor;
        public static TrailRenderer gunTrail;

        public static readonly Dictionary<int, GameObject> GunPtr = new Dictionary<int, GameObject>();
        public static bool rightGunHand;

        public static Color Default;
        public static Color Selected;
        private static Mesh originalSphereMesh;
        private static Mesh cachedCircleMesh;
        private static Mesh cachedSquareMesh;
        private static Mesh cachedTriangleMesh;
        private static Mesh cachedStarMesh;
        private static int cachedShapeIndex = -1;
        private static GameObject lockShape;
        private static Material lockShapeMat;
        private static int lockShapeActiveIndex = -1;
        public static GameObject InitpObjs()
        {
            pObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(pObj.GetComponent<Rigidbody>());
            Destroy(pObj.GetComponent<SphereCollider>());
            pObj.transform.localScale = (Vector3.one * .3f) / 2;
            Renderer pR = pObj.GetComponent<Renderer>();
            pR.material.shader = Shader.Find("GUI/Text Shader");
            pR.material.color = Default;
            pColor = pR.material;
            originalSphereMesh = pObj.GetComponent<MeshFilter>().sharedMesh;
            pObj.SetActive(false);
            gunLine = pObj.GetOrAddComponent<LineRenderer>();
            gunLine.material.shader = Shader.Find("GUI/Text Shader");
            gunLine.startWidth = .006f;
            gunLine.useWorldSpace = true;
            gunLine.material.color = Default;
            gunLine.positionCount = 51;
            gunLine.startColor = Color.white;
            gunLine.endColor = Color.white;
            gunLine.enabled = true;
            gunTrail = pObj.AddComponent<TrailRenderer>();
            gunTrail.time = 1f;
            gunTrail.startWidth = 0.05f;
            gunTrail.endWidth = 0f;
            gunTrail.material = new Material(Shader.Find("Sprites/Default"));
            gunTrail.numCapVertices = 2;
            gunTrail.numCornerVertices = 2;
            Gradient trailGrad = new Gradient();
            trailGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            gunTrail.colorGradient = trailGrad;
            gunTrail.emitting = false;
            return pObj;
        }
        #endregion

        #region colliders
        public static readonly string[] bypassLayers =
        {
            "Gorilla Trigger",
            "Gorilla Boundary",
            "GorillaHand",
            "GorillaObject",
            "Zone",
            "Water",
            "GorillaCosmetics",
            "GorillaParticle",
        };
        public static readonly LayerMask BypassLayers = ~LayerMask.GetMask(bypassLayers);
        #endregion

        #region determine button hand/input
        public static bool DetermineGunHand(bool trigger) => rightGunHand ? (trigger ? rightTriggerPressed : rightGrab) : (trigger ? leftTriggerPressed : leftGrab);
        public static Transform DetermineHand() => rightGunHand ? GTPlayer.Instance.GetControllerTransform(false) : GTPlayer.Instance.GetControllerTransform(true);
        #endregion

        public static void SendGunData()
        {
            if (!PhotonNetwork.InRoom) return;

            Vector3 handPos = Vector3.zero;
            Vector3 endPos = Vector3.zero;
            bool active = false;
            int triggered = 0;

            if (GunActiveThisFrame)
            {
                handPos = GunStartPos;
                endPos = GunEndPos;
                active = true;
                triggered = GetGunInput(true) || gunLocked ? 1 : 0;
            }
            else if (pObj != null && pObj.activeSelf)
            {
                handPos = DetermineHand().position;
                endPos = endPoint;
                active = true;
                triggered = data.IsTriggered ? 1 : 0;
            }

            if (!active) return;

            var args = new object[]
            {
                "seralyth_netmenu_gundata",
                handPos.x, handPos.y, handPos.z,
                endPos.x, endPos.y, endPos.z,
                triggered
            };

            PhotonNetwork.RaiseEvent(NetworkMenuManager.NetworkMenuByte, args, new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others
            }, SendOptions.SendUnreliable);
        }

        public static void HandleRemoteGunData(Player sender, object[] args)
        {
            if (args.Length < 8) return;

            float hx = Convert.ToSingle(args[1]);
            float hy = Convert.ToSingle(args[2]);
            float hz = Convert.ToSingle(args[3]);
            float ex = Convert.ToSingle(args[4]);
            float ey = Convert.ToSingle(args[5]);
            float ez = Convert.ToSingle(args[6]);
            bool triggered = Convert.ToInt32(args[7]) == 1;

            Vector3 handPos = new Vector3(hx, hy, hz);
            Vector3 endPos = new Vector3(ex, ey, ez);

            int actor = sender.ActorNumber;

            if (!remoteGunPointers.TryGetValue(actor, out var ptr))
            {
                ptr = new RemoteGunPointer();
                remoteGunPointers[actor] = ptr;
            }

            ptr.lastUpdateTime = Time.time;
            ptr.player = sender;

            if (ptr.pointer == null)
            {
                ptr.pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(ptr.pointer.GetComponent<Rigidbody>());
                Destroy(ptr.pointer.GetComponent<SphereCollider>());
                ptr.pointer.transform.localScale = (Vector3.one * 0.3f) / 2f;
                Renderer pr = ptr.pointer.GetComponent<Renderer>();
                pr.material.shader = Shader.Find("GUI/Text Shader");
                pr.material.color = Color.white;
                ptr.pointerMat = pr.material;

                GameObject lineObj = new GameObject("RemoteGunLine_" + actor);
                ptr.line = lineObj.AddComponent<LineRenderer>();
                ptr.line.material.shader = Shader.Find("GUI/Text Shader");
                ptr.line.startWidth = 0.006f;
                ptr.line.endWidth = 0.006f;
                ptr.line.useWorldSpace = true;
                ptr.line.positionCount = 51;
                ptr.line.startColor = Color.white;
                ptr.line.endColor = Color.white;
            }

            Color lineColor = buttonColors[0].GetCurrentColor();
            if (triggered)
                lineColor = buttonColors[1].GetCurrentColor();

            ptr.pointerMat.color = lineColor;
            ptr.line.startColor = lineColor;
            ptr.line.endColor = lineColor;
            ptr.targetHandPos = handPos;
            ptr.targetEndPos = endPos;
            ptr.triggered = triggered;
            ptr.active = true;
        }

        public static void UpdateRemoteGunPointers()
        {
            if (!PhotonNetwork.InRoom) return;

            List<int> toRemove = null;
            foreach (var kvp in remoteGunPointers)
            {
                RemoteGunPointer ptr = kvp.Value;
                if (Time.time - ptr.lastUpdateTime > 0.5f)
                {
                    if (toRemove == null) toRemove = new List<int>();
                    toRemove.Add(kvp.Key);
                    continue;
                }

                if (!ptr.active || ptr.pointer == null) continue;

                VRRig rig = ptr.player != null ? GorillaGameManager.StaticFindRigForPlayer(ptr.player) : null;
                if (rig != null)
                {
                    ptr.targetHandPos += rig.transform.position - ptr.lastRigPos;
                    ptr.targetEndPos += rig.transform.position - ptr.lastRigPos;
                    ptr.lastRigPos = rig.transform.position;
                }

                ptr.handPos = Vector3.Lerp(ptr.handPos, ptr.targetHandPos, Time.deltaTime * 15f);
                ptr.endPos = Vector3.Lerp(ptr.endPos, ptr.targetEndPos, Time.deltaTime * 15f);

                ptr.pointer.transform.position = ptr.endPos;

                Vector3 mid = (ptr.handPos + ptr.endPos) * 0.5f;
                for (int i = 0; i < ptr.line.positionCount; i++)
                {
                    float t = i / (float)(ptr.line.positionCount - 1);
                    Vector3 a = Vector3.Lerp(ptr.handPos, mid, t);
                    Vector3 b = Vector3.Lerp(mid, ptr.endPos, t);
                    ptr.line.SetPosition(i, Vector3.Lerp(a, b, t));
                }
            }

            if (toRemove != null)
            {
                foreach (int key in toRemove)
                {
                    if (remoteGunPointers.TryGetValue(key, out var ptr))
                    {
                        if (ptr.pointer != null) Destroy(ptr.pointer);
                        if (ptr.line != null) Destroy(ptr.line.gameObject);
                        remoteGunPointers.Remove(key);
                    }
                }
            }
        }

        public static void ClearRemoteGunPointers()
        {
            foreach (var kvp in remoteGunPointers)
            {
                if (kvp.Value.pointer != null) Destroy(kvp.Value.pointer);
                if (kvp.Value.line != null) Destroy(kvp.Value.line.gameObject);
            }
            remoteGunPointers.Clear();
        }

        public class RemoteGunPointer
        {
            public GameObject pointer;
            public Material pointerMat;
            public LineRenderer line;
            public Vector3 handPos;
            public Vector3 endPos;
            public Vector3 targetHandPos;
            public Vector3 targetEndPos;
            public Vector3 lastRigPos;
            public bool triggered;
            public bool active;
            public float lastUpdateTime;
            public Player player;
        }

        public static readonly Dictionary<int, RemoteGunPointer> remoteGunPointers = new Dictionary<int, RemoteGunPointer>();

        private static Mesh GetCircleMesh()
        {
            if (cachedCircleMesh != null) return cachedCircleMesh;
            Mesh mesh = new Mesh();
            int segments = 32;
            Vector3[] verts = new Vector3[segments + 1];
            int[] tris = new int[segments * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
            }
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = (i + 1) % segments + 1;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            cachedCircleMesh = mesh;
            return mesh;
        }

        private static Mesh GetSquareMesh()
        {
            if (cachedSquareMesh != null) return cachedSquareMesh;
            float s = 0.5f;
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(-s, 0f, -s),
                new Vector3(-s, 0f, s),
                new Vector3(s, 0f, s),
                new Vector3(s, 0f, -s)
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            cachedSquareMesh = mesh;
            return mesh;
        }

        private static Mesh GetTriangleMesh()
        {
            if (cachedTriangleMesh != null) return cachedTriangleMesh;
            float s = 0.5f;
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(0f, 0f, s),
                new Vector3(-s * 0.866f, 0f, -s * 0.5f),
                new Vector3(s * 0.866f, 0f, -s * 0.5f)
            };
            mesh.triangles = new int[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            cachedTriangleMesh = mesh;
            return mesh;
        }

        private static Mesh GetStarMesh()
        {
            if (cachedStarMesh != null) return cachedStarMesh;
            Mesh mesh = new Mesh();
            int points = 5;
            float outer = 0.5f;
            float inner = 0.2f;
            Vector3[] verts = new Vector3[points * 2 + 1];
            int[] tris = new int[points * 6];
            verts[0] = Vector3.zero;
            for (int i = 0; i < points; i++)
            {
                float oa = (float)i / points * Mathf.PI * 2f - Mathf.PI / 2f;
                float ia = oa + Mathf.PI / points;
                verts[i * 2 + 1] = new Vector3(Mathf.Cos(oa) * outer, 0f, Mathf.Sin(oa) * outer);
                verts[i * 2 + 2] = new Vector3(Mathf.Cos(ia) * inner, 0f, Mathf.Sin(ia) * inner);
            }
            for (int i = 0; i < points; i++)
            {
                int b = i * 6;
                int v = i * 2 + 1;
                int vNext2 = (v + 2) % (points * 2 + 1);
                if (vNext2 == 0) vNext2 = 1;
                tris[b] = 0;
                tris[b + 1] = v;
                tris[b + 2] = v + 1;
                tris[b + 3] = 0;
                tris[b + 4] = v + 1;
                tris[b + 5] = vNext2;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            cachedStarMesh = mesh;
            return mesh;
        }


        private static void SetTrailColor(Color c)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            gunTrail.colorGradient = g;
            gunTrail.startColor = c;
            gunTrail.endColor = c;
        }

        private static void ShowLockShape(Vector3 position, int shapeIndex)
        {
            if (shapeIndex <= 0)
            {
                HideLockShape();
                return;
            }

            if (lockShape == null)
            {
                lockShape = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(lockShape.GetComponent<Rigidbody>());
                Destroy(lockShape.GetComponent<SphereCollider>());
                lockShape.transform.localScale = Vector3.one * 0.15f;
                Renderer r = lockShape.GetComponent<Renderer>();
                r.material.shader = Shader.Find("GUI/Text Shader");
                r.material.color = Selected;
                lockShapeMat = r.material;
                lockShape.name = "GunLibLockShape";
            }

            lockShape.SetActive(true);
            lockShape.transform.position = position + Vector3.up * 0.5f;
            lockShape.transform.Rotate(Vector3.up, Time.deltaTime * 90f);

            if (shapeIndex != lockShapeActiveIndex)
            {
                lockShapeActiveIndex = shapeIndex;
                MeshFilter mf = lockShape.GetComponent<MeshFilter>();
                switch (shapeIndex)
                {
                    case 1: mf.sharedMesh = GetCircleMesh(); break;
                    case 2: mf.sharedMesh = GetSquareMesh(); break;
                    case 3: mf.sharedMesh = GetTriangleMesh(); break;
                    case 4: mf.sharedMesh = GetStarMesh(); break;
                }
            }

            if (lockShapeMat != null)
                lockShapeMat.color = Selected;
        }

        private static void HideLockShape()
        {
            if (lockShape != null)
                lockShape.SetActive(false);
            lockShapeActiveIndex = -1;
        }

        public static GunLibData GunInstance(bool lockable = false)
        {
            Vector3 pos = XRSettings.isDeviceActive ? DetermineHand().position - (DetermineHand().up / 4f) : GameObject.Find("Shoulder Camera").GetComponent<Camera>().ScreenPointToRay(UnityInput.mousePosition).origin;
            Vector3 dir = XRSettings.isDeviceActive ? -DetermineHand().up : GameObject.Find("Shoulder Camera").GetComponent<Camera>().ScreenPointToRay(UnityInput.mousePosition).direction;

            data.IsGripping = XRSettings.isDeviceActive ? DetermineGunHand(false) : UnityInput.GetMouseButton(1);
            data.IsTriggered = XRSettings.isDeviceActive ? DetermineGunHand(true) : UnityInput.GetMouseButton(0);

            if (data.IsGripping) //make null ptr pos & null rig (plr left) fallback
            {
                Physics.Raycast(pos, dir, out RaycastHit hit, float.PositiveInfinity, BypassLayers);
                if (lockable)
                {
                    VRRig rig = hit.collider.GetComponentInParent<VRRig>();
                    if (!data.LockedRig)
                    {
                        if (rig && data.IsTriggered)
                            data.LockedRig = rig;
                        determinePos = data.IsTriggered && rig && !rig.isOfflineVRRig ? data.LockedRig.transform.position : hit.point;
                        pColor.color = gunLine.material.color = Default;
                        if (GunLibTrail) SetTrailColor(Default);
                        HideLockShape();
                    }
                    else if (data.IsTriggered && data.LockedRig)
                    {
                        data.GunReady = true;
                        determinePos = data.HitPos = data.LockedRig.transform.position;
                        pColor.color = gunLine.material.color = Selected;
                        if (GunLibTrail) SetTrailColor(Selected);
                        ShowLockShape(data.LockedRig.transform.position, GunLibShape);
                    }
                    else
                    {
                        determinePos = hit.point;
                        data.GunReady = false;
                        data.LastLockedRig = data.LockedRig;
                        data.LockedRig = null;
                        pColor.color = gunLine.material.color = Default;
                        if (GunLibTrail) SetTrailColor(Default);
                        HideLockShape();
                    }
                }
                else
                {
                    data.HitPos = hit.point;
                    determinePos = data.HitPos;
                    pColor.color = gunLine.material.color = Default;
                    if (GunLibTrail) { gunTrail.startColor = Default; gunTrail.endColor = Default; }
                    data.GunReady = data.IsTriggered;
                    data.Collider = hit.collider;
                    HideLockShape();
                }
                endPoint = Vector3.Lerp(endPoint, determinePos, Time.deltaTime * 12);
                if (GunLibLine)
                {
                    gunLine.enabled = true;
                    Vector3 mid = (DetermineHand().position + endPoint) * .5f;
                    for (int i = 0; i < gunLine.positionCount; i++)
                    {
                        float t = i / (float)(gunLine.positionCount - 1);
                        Vector3 a = Vector3.Lerp(DetermineHand().position, mid, t);
                        Vector3 b = Vector3.Lerp(mid, endPoint, t);
                        gunLine.SetPosition(i, Vector3.Lerp(a, b, t));
                    }
                    pObj.transform.position = gunLine.GetPosition(gunLine.positionCount - 1);
                }
                else
                {
                    gunLine.enabled = false;
                    pObj.transform.position = endPoint;
                }
                pObj.SetActive(true);
                if (gunTrail != null) gunTrail.emitting = GunLibTrail;
            }
            else ResetGL();
            return data;
        }
    }
}