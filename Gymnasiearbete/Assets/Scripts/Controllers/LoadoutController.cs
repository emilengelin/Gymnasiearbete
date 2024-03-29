﻿using ArenaShooter.Extensions.Attributes;
using ArenaShooter.Player;
using System;

namespace ArenaShooter.Controllers
{

    [Persistent]
    class LoadoutController : Controller<LoadoutController>
    {

        #region Public constants

        public const int MAX_PLAYER_LOADOUT_SLOTS = 5;

        #endregion

        #region Public properties

        public Action OnLoadoutChanged { get; set; }

        #endregion

        protected override void OnAwake()
        {
            Profile.Load();
        }

        private void OnApplicationQuit()
        {
            Profile.Save();
        }

        public void SetSelectedLoadoutSlot(LoadoutSlot loadoutSlot)
        {
            Profile.SelectedLoadoutSlot = loadoutSlot;

            OnLoadoutChanged?.Invoke();
        }

    }

}
