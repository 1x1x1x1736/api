/*
 * Seralyth Menu  Mods/CopyName.cs
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
using GorillaNetworking;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Realtime;
using Seralyth.Extensions;
using Seralyth.Managers;
using Seralyth.Utilities;
using System.Linq;
using UnityEngine;
using static Seralyth.Menu.Main;
using static Seralyth.Utilities.RigUtilities;

namespace Seralyth.Mods
{
    public static class CopyName
    {
        public static void Self()
        {
            GUIUtility.systemCopyBuffer = PhotonNetwork.LocalPlayer.NickName;
            NotificationManager.SendNotification(
                $"<color=grey>[</color><color=green>COPY NAME</color><color=grey>]</color> Copied your name to clipboard.");
        }

        public static void Gun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        GUIUtility.systemCopyBuffer = CleanPlayerName(GetPlayerFromVRRig(gunTarget).NickName);
                        NotificationManager.SendNotification(
                            $"<color=grey>[</color><color=green>COPY NAME</color><color=grey>]</color> Copied target's name to clipboard.");
                    }
                }
            }
        }

        public static void All()
        {
            string allNames = string.Join(", ", PhotonNetwork.PlayerList.Select(player => player.NickName));
            GUIUtility.systemCopyBuffer = allNames;
            NotificationManager.SendNotification(
                $"<color=grey>[</color><color=green>COPY NAME</color><color=grey>]</color> Copied all names to clipboard.");
        }
    }
}
    

