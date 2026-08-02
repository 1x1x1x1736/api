/*
 * Seralyth Menu  Mods/CoolMods.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Seralyth Software
 * https://github.com/Seralyth/Seralyth-Menu
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using GorillaLocomotion;
using GorillaTag;
using Photon.Pun;
using Seralyth.Extensions;
using Seralyth.Managers;
using Seralyth.Menu;
using System.Collections.Generic;
using UnityEngine;
using static Seralyth.Menu.Main;

namespace Seralyth.Mods
{
    public static class CoolMods
    {
        private static float originalTimeScale = 1f;
        private static int timeWarpIndex = 2;
        private static readonly float[] timeWarpSpeeds = { 0.1f, 0.2f, 0.3f, 0.5f, 0.75f };
        private static readonly string[] timeWarpNames = { "10%", "20%", "30%", "50%", "75%" };

        private static float pullRadius = 8f;
        private static float pullForce = 10f;

        private static float shockwaveRadius = 12f;
        private static float shockwaveForce = 20f;

        private static bool grappling;
        private static Vector3 grappleTarget;
        private static float grappleSpeed = 15f;

        private static Rigidbody PlayerRigidbody => GTPlayer.Instance.bodyCollider.attachedRigidbody;

        // ── Time Warp ──

        public static void TimeWarp()
        {
            Time.timeScale = timeWarpSpeeds[timeWarpIndex];
        }

        public static void EnableTimeWarp()
        {
            originalTimeScale = Time.timeScale;
            NotificationManager.SendNotification($"<color=grey>[</color><color=green>TIME WARP</color><color=grey>]</color> Time slowed to {timeWarpNames[timeWarpIndex]}.");
        }

        public static void DisableTimeWarp()
        {
            Time.timeScale = originalTimeScale;
            NotificationManager.SendNotification("<color=grey>[</color><color=green>TIME WARP</color><color=grey>]</color> Time restored.");
        }

        public static void ChangeTimeWarpSpeed(bool positive = true)
        {
            if (positive)
                timeWarpIndex++;
            else
                timeWarpIndex--;

            timeWarpIndex %= timeWarpSpeeds.Length;
            if (timeWarpIndex < 0)
                timeWarpIndex = timeWarpSpeeds.Length - 1;

            Buttons.GetIndex("Time Warp").overlapText = "Time Warp <color=grey>[</color><color=green>" + timeWarpNames[timeWarpIndex] + "</color><color=grey>]</color>";
            Buttons.GetIndex("Change Time Warp Speed").overlapText = "Change Time Warp Speed <color=grey>[</color><color=green>" + timeWarpNames[timeWarpIndex] + "</color><color=grey>]</color>";
        }

        // ── Magnetic Pull ──

        public static void MagneticPull()
        {
            if (!ControllerInputPoller.instance.leftControllerSecondaryButton) return;
            Vector3 origin = GorillaTagger.Instance.rightHandTransform.position;
            Collider[] hits = Physics.OverlapSphere(origin, pullRadius);
            foreach (var hit in hits)
            {
                if (hit.attachedRigidbody && hit.attachedRigidbody != PlayerRigidbody)
                {
                    Vector3 dir = origin - hit.transform.position;
                    hit.attachedRigidbody.AddForce(dir.normalized * pullForce, ForceMode.Acceleration);
                }
            }
        }

        public static void EnableMagneticPull()
        {
            NotificationManager.SendNotification("<color=grey>[</color><color=purple>MAGNETIC PULL</color><color=grey>]</color> Hold <color=green>G</color> to pull objects toward your hand.");
        }

        // ── Shockwave ──

        public static void Shockwave()
        {
            if (!ControllerInputPoller.instance.leftControllerSecondaryButton) return;
            Vector3 origin = GTPlayer.Instance.transform.position;
            Collider[] hits = Physics.OverlapSphere(origin, shockwaveRadius);
            foreach (var hit in hits)
            {
                if (hit.attachedRigidbody && hit.attachedRigidbody != PlayerRigidbody)
                {
                    Vector3 dir = hit.transform.position - origin;
                    hit.attachedRigidbody.AddForce(dir.normalized * shockwaveForce, ForceMode.Impulse);
                }
            }
            NotificationManager.SendNotification("<color=grey>[</color><color=orange>SHOCKWAVE</color><color=grey>]</color> BOOM!");
        }

        public static void EnableShockwave()
        {
            NotificationManager.SendNotification("<color=grey>[</color><color=orange>SHOCKWAVE</color><color=grey>]</color> Press <color=green>G</color> to unleash a shockwave.");
        }

        // ── Grapple Hook ──

        public static void Grapple()
        {
            if (ControllerInputPoller.instance.leftControllerSecondaryButton)
            {
                if (!grappling)
                {
                    Ray ray = new Ray(GorillaTagger.Instance.rightHandTransform.position, GorillaTagger.Instance.rightHandTransform.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, 50f))
                    {
                        grappling = true;
                        grappleTarget = hit.point;
                    }
                }
                if (grappling)
                {
                    Vector3 dir = grappleTarget - GTPlayer.Instance.transform.position;
                    if (dir.magnitude > 1f)
                        PlayerRigidbody.AddForce(dir.normalized * grappleSpeed, ForceMode.Acceleration);
                    else
                        grappling = false;
                }
            }
            else
            {
                grappling = false;
            }
        }

        public static void EnableGrapple()
        {
            grappling = false;
            if (!Application.isPlaying) return;
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.localScale = Vector3.one * 0.05f;
            NotificationManager.SendNotification("<color=grey>[</color><color=lime>GRAPPLE</color><color=grey>]</color> Point and hold <color=green>G</color> to grapple.");
        }

        public static void DisableGrapple()
        {
            grappling = false;
        }

        // ── Auto Juke ──

        private static float autoJukeRange = 8f;
        private static float autoJukeStrength = 14f;

        public static void AutoJuke()
        {
            if (VRRig.LocalRig == null || VRRigCache.ActiveRigs == null) return;
            Transform head = GorillaTagger.Instance.headCollider.transform;
            Vector3 localPos = VRRig.LocalRig.transform.position;
            Vector3 dodgeVelocity = Vector3.zero;

            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal) continue;
                if (rig.IsTagged() == VRRig.LocalRig.IsTagged()) continue;

                Vector3 dirToEnemy = (rig.transform.position - localPos).normalized;
                float dist = Vector3.Distance(rig.transform.position, localPos);
                if (dist > autoJukeRange) continue;

                float forwardDot = Vector3.Dot(head.forward, dirToEnemy);
                float rightDot = Vector3.Dot(head.right, dirToEnemy);

                Vector3 dodgeDir;
                if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot))
                    dodgeDir = -Mathf.Sign(forwardDot) * head.forward;
                else
                    dodgeDir = -Mathf.Sign(rightDot) * head.right;

                PlayerRigidbody.linearVelocity = dodgeDir.normalized * autoJukeStrength;
                return;
            }
        }

        public static void EnableAutoJuke()
        {
        }

        // ── Air Jump ──

        private static bool ajPrevTouching;
        private static bool ajHasCharge;
        private static int airJumpBoostIndex;
        private static readonly int[] airJumpBoosts = { 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 };

        public static void AirJump()
        {
            bool touching = GTPlayer.Instance.IsHandTouching(true) || GTPlayer.Instance.IsHandTouching(false);

            if (touching && !ajPrevTouching)
                ajHasCharge = true;

            ajPrevTouching = touching;

            if (ajHasCharge && ControllerInputPoller.instance.rightControllerPrimaryButton)
            {
                PlayerRigidbody.linearVelocity = GorillaTagger.Instance.headCollider.transform.forward * airJumpBoosts[airJumpBoostIndex];
                ajHasCharge = false;
            }
        }

        public static void EnableAirJump()
        {
            ajPrevTouching = false;
            ajHasCharge = false;
        }

        public static void ChangeAirJumpBoost(bool positive = true)
        {
            if (positive)
                airJumpBoostIndex++;
            else
                airJumpBoostIndex--;

            airJumpBoostIndex %= airJumpBoosts.Length;
            if (airJumpBoostIndex < 0)
                airJumpBoostIndex = airJumpBoosts.Length - 1;

            Buttons.GetIndex("Air Jump").overlapText = "Air Jump <color=grey>[</color><color=green>" + airJumpBoosts[airJumpBoostIndex] + "</color><color=grey>]</color>";
            Buttons.GetIndex("Change Air Jump Boost").overlapText = "Change Air Jump Boost <color=grey>[</color><color=green>" + airJumpBoosts[airJumpBoostIndex] + "</color><color=grey>]</color>";
        }

        // ── Ground Slam ──

        private static float groundSlamCooldown;

        public static void GroundSlam()
        {
            if (!ControllerInputPoller.instance.rightControllerSecondaryButton) return;
            if (Time.time < groundSlamCooldown) return;

            GorillaTagger.Instance.rigidbody.linearVelocity = new Vector3(
                GorillaTagger.Instance.rigidbody.linearVelocity.x,
                -25f,
                GorillaTagger.Instance.rigidbody.linearVelocity.z
            );
            groundSlamCooldown = Time.time + 0.3f;
        }

        public static void EnableGroundSlam()
        {
            groundSlamCooldown = 0f;
        }

    }
}
