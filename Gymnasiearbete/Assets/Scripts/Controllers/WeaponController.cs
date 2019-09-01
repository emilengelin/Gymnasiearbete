﻿using ArenaShooter.Combat;
using ArenaShooter.Templates.Weapons;
using System.Linq;
using UnityEngine;

#pragma warning disable 0649

namespace ArenaShooter.Controllers
{

    class WeaponController : Controller<WeaponController>
    {

        #region Editor

        [Header("References")]
        [SerializeField] private StockTemplate[] stockTemplates;

        [Space]
        [SerializeField] private BodyTemplate[] bodyTemplates;

        [Space]
        [SerializeField] private BarrelTemplate[] barrelTemplates;

        [Space]
        [SerializeField] private Transform projectileContainer;

        #endregion

        #region Public properties

        public Transform ProjectileContainer
        {
            get
            {
                return projectileContainer;
            }
        }

        #endregion

        public Weapon CreateWeapon(StockTemplate stockTemplate, BodyTemplate bodyTemplate, BarrelTemplate barrelTemplate, Transform parent)
        {
            if (stockTemplate.OutputType == bodyTemplate.OutputType && bodyTemplate.OutputType == barrelTemplate.OutputType)
            {
                GameObject weaponGameObject = new GameObject("Weapon");
                weaponGameObject.transform.SetParent(parent);
                Weapon weapon = null;

                switch (stockTemplate.OutputType)
                {
                    case WeaponPartTemplateOutputType.Raycasting:
                        weapon = weaponGameObject.AddComponent<RaycastWeapon>();
                        break;
                    case WeaponPartTemplateOutputType.Projectile:
                        weapon = weaponGameObject.AddComponent<ProjectileWeapon>();
                        break;
                    case WeaponPartTemplateOutputType.Electric:
                        weapon = weaponGameObject.AddComponent<ElectricWeapon>();
                        break;
                    case WeaponPartTemplateOutputType.Support:
                        weapon = weaponGameObject.AddComponent<SupportWeapon>();
                        break;
                    default:
                        Debug.LogWarning("Weapon could not be built with the three given part templates.");
                        return null;
                }

                weapon.Initialize(stockTemplate, bodyTemplate, barrelTemplate);
                return weapon;
            }
            else
            {
                Debug.LogWarning("Weapon could not be built with the three given part templates.");
                return null;
            }
        }

        public StockTemplate GetStockTemplate(ushort id)
        {
            return stockTemplates.FirstOrDefault(t => t.TemplateId == id);
        }

        public BodyTemplate GetBodyTemplate(ushort id)
        {
            return bodyTemplates.FirstOrDefault(t => t.TemplateId == id);
        }

        public BarrelTemplate GetBarrelTemplate(ushort id)
        {
            return barrelTemplates.FirstOrDefault(t => t.TemplateId == id);
        }

    }

}
