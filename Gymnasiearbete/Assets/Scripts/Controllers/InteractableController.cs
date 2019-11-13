﻿using UnityEngine;

namespace ArenaShooter.Controllers
{
    class InteractableController : ServerController<InteractableController>
    {
        #region Editor

        
        [SerializeField] private Transform       medKitContainer;
        [SerializeField] private Transform[]     spawnPointsLargeMedkits;
        [SerializeField] private Transform[]     spawnPointsSmallMedkits;
        [SerializeField] private Transform[]     spawnPointsSmallAmmoboxes;
        [SerializeField] private Transform[]     spawnPointsLargeAmmoBoxes;
        [SerializeField] private RectTransform   container;

        #endregion

        public RectTransform Container
        {
            get
            {
                return container;
            }
        }

        #region Methods

        private void Start()
        {
            
            for (int i = 0; i < spawnPointsLargeMedkits.Length; i++)
            {
                BoltNetwork.Instantiate(BoltPrefabs.LargeMedkitInteractablePrefab, spawnPointsLargeMedkits[i].position, Quaternion.identity);

            }
            for (int i = 0; i < spawnPointsSmallMedkits.Length; i++)
            {
                BoltNetwork.Instantiate(BoltPrefabs.SmallMedkitPrefab, spawnPointsSmallMedkits[i].position, Quaternion.identity);
            }

            for (int i = 0; i < spawnPointsLargeAmmoBoxes.Length; i++)
            {
                BoltNetwork.Instantiate(BoltPrefabs.LargeAmmoBoxPrefab, spawnPointsLargeAmmoBoxes[i].position, Quaternion.identity);
            }
            for (int i = 0; i < spawnPointsSmallAmmoboxes.Length; i++)
            {
                BoltNetwork.Instantiate(BoltPrefabs.SmallAmmoBoxPrefab, spawnPointsSmallAmmoboxes[i].position, Quaternion.identity);
            }
                     
        }

        #endregion
    }
}

