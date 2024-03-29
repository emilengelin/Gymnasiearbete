﻿using ArenaShooter.Templates.Items;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649

namespace ArenaShooter.Templates.Weapons
{

    enum FiringMode : byte
    {
        Single    = 0,
        Burst     = 5,
        Automatic = 10
    }

    [CreateAssetMenu(menuName = "Templates/Weapons/Body")]
    class BodyTemplate : WeaponPartTemplate
    {

        #region Editor

        [Header("Stats")]
        [SerializeField] private ushort damage         = 10;
        [SerializeField] private short  maxAmmoPerClip = 30;
        [SerializeField] private short  maxAmmoStock   = 150;
        [SerializeField] private short  ammoPerFire    = 1;

        [Space]
        [SerializeField] private float fireCooldown   = 0.1f;
        [SerializeField] private float reloadTime     = 1f;
        [SerializeField] private float fullReloadTime = 1.5f;

        [Space]
        [SerializeField] private float burstFireInterval = 0.05f;
        [SerializeField] private sbyte burstShots        = 3;

        [Header("Logic")]
        [SerializeField] private FiringMode firingMode          = FiringMode.Automatic;
        [SerializeField] private bool       manualAmmoDepletion = false;

        [Header("Prefabs")]
        [Help(@"The prefab to be saved as firePrefab depends on what type of OutputType the weapon has:

* Raycast: Simple particle system or other reusable effect. No scripts needed.
* Projectile: Prefab with rigidbody and script (ProjectileShot) attached needed.
* Electric: Prefab with script (ElectricShot) attached needed.
* Support: Prefab with LineRenderer and script (SupportShot) attached needed."
)]
        [SerializeField] private GameObject firePrefab;

        #endregion

        #region Getters

        public override WeaponPartTemplateType Type
        {
            get
            {
                return WeaponPartTemplateType.Body;
            }
        }

        public ushort Damage
        {
            get
            {
                return damage;
            }
        }

        public short MaxAmmoPerClip
        {
            get
            {
                return maxAmmoPerClip;
            }
        }

        public short MaxAmmoStock
        {
            get
            {
                return maxAmmoStock;
            }
        }

        public short AmmoPerFire
        {
            get
            {
                return ammoPerFire;
            }
        }

        public float FireCooldown
        {
            get
            {
                return fireCooldown;
            }
        }

        public float ReloadTime
        {
            get
            {
                return reloadTime;
            }
        }

        public float FullReloadTime
        {
            get
            {
                return fullReloadTime;
            }
        }

        public float BurstFireInterval
        {
            get
            {
                return burstFireInterval;
            }
        }

        public sbyte BurstShots
        {
            get
            {
                return burstShots;
            }
        }

        public FiringMode FiringMode
        {
            get
            {
                return firingMode;
            }
        }

        public bool ManualAmmoDepletion
        {
            get
            {
                return manualAmmoDepletion;
            }
        }

        public GameObject FirePrefab
        {
            get
            {
                return firePrefab;
            }
        }

        #endregion

        #region Helpers

        public override Dictionary<StatType, float> GetStatTypeValues()
        {
            var statTypeValues = new Dictionary<StatType, float>()
            {
                { StatType.Damage,            damage },
                { StatType.MaxAmmoPerClip,    maxAmmoPerClip },
                { StatType.MaxAmmoStock,      maxAmmoStock },
                { StatType.FireCooldown,      fireCooldown },
                { StatType.ReloadTime,        reloadTime },
                { StatType.FullReloadTime,    fullReloadTime }
            };

            if (firingMode == FiringMode.Burst)
            {
                statTypeValues.Add(StatType.BurstFireInterval, burstFireInterval);
                statTypeValues.Add(StatType.BurstShots, burstShots);
            }

            statTypeValues.Add(StatType.FiringMode, (byte)firingMode);

            return statTypeValues;
        }

        #endregion

    }

}
