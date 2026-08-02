/*
 * Seralyth Menu  Patches/Menu/FindFriendsPatch.cs
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
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Realtime;

namespace Seralyth.Patches.Menu
{
    [HarmonyPatch(typeof(LoadBalancingClient), "OnOperationResponse")]
    public static class FindFriendsPatch
    {
        public static void Postfix(LoadBalancingClient __instance, OperationResponse operationResponse)
        {
            if (operationResponse.OperationCode == 222)
                Mods.Safety.HandleFindFriendsResponse(operationResponse);
        }
    }
}
