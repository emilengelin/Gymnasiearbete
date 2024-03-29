﻿using ArenaShooter.Data;
using System;
using UnityEngine;

namespace ArenaShooter.Player
{

    static class Profile
    {

        public const string DEFAULT_USERNAME = "Guest User";

        public static string Username    = DEFAULT_USERNAME;
        public static int    TotalKills  = 0;
        public static int    TotalDeaths = 0;
        public static int    TotalShots  = 0;
        public static double TimePlayed  = 0;

        public static Inventory   Inventory;
        public static LoadoutSlot SelectedLoadoutSlot;

        public static void Save()
        {
            ProfileData profileData = new ProfileData(Username,
                                                      TotalKills, 
                                                      TotalDeaths, 
                                                      TotalShots, 
                                                      TimePlayed, 
                                                      new InventoryData(Inventory), 
                                                      Array.IndexOf(Inventory.LoadoutSlots, SelectedLoadoutSlot)
                                                      );

            DataManager.SaveProfile(profileData);
        }

        public static void Load()
        {
            ProfileData profileData = DataManager.LoadProfile();

            // Check if profile data exists:
            if (profileData != null)
            {
                // It does. Load it:
                Username            = profileData.username;
                TotalKills          = profileData.totalKills;
                TotalDeaths         = profileData.totalDeaths;
                TotalShots          = profileData.totalShots;
                TimePlayed          = profileData.timePlayed;
                Inventory           = new Inventory(profileData.inventoryData);
                SelectedLoadoutSlot = Inventory.LoadoutSlots[Mathf.Clamp(profileData.selectedLoadoutSlotIndex, 0, Inventory.LoadoutSlots.Length)];
            }
            else
            {
                // Create a new profile:
                Username            = DEFAULT_USERNAME;
                TotalKills          = 0;
                TotalDeaths         = 0;
                TotalShots          = 0;
                TimePlayed          = 0;
                Inventory           = Inventory.CreateDefault();
                SelectedLoadoutSlot = Inventory.LoadoutSlots[0];
            }
        }

    }

}
