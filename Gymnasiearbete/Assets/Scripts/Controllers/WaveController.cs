﻿using ArenaShooter.Entities;
using ArenaShooter.Extensions;
using ArenaShooter.Templates.Enemies;
using Bolt;
using Bolt.Matchmaking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649

namespace ArenaShooter.Controllers
{

    class WaveController : ServerController<WaveController>
    {

        #region Public constants

        public const float WAVE_COUNTDOWN_TIME         = 3f;
        public const float WAVE_START_WAIT_TIME        = 3f;
        public const float WAVE_END_WAIT_TIME          = 3f;
        public const float WAVE_RESET_NUMBER_WAIT_TIME = 1.5f;

        #endregion

        #region Private constants

        private const int MAX_ENEMIES_ON_MAP = 50;
        private const int MIN_ENEMIES_ON_MAP = 10;
        private const int ENEMIES_IN_POOLS   = 10;

        #endregion

        #region Editor

        [Header("References")]
        [SerializeField] private Transform[] spawnPoints;

        [Space]
        [SerializeField] private EnemyTemplate[] spawnableEnemyTemplates;

        #endregion

        #region Public properties

        /// <summary>
        /// Can more enemies still spawn?
        /// </summary>
        public bool EnemiesCanSpawn
        {
            get
            {
                return currentSpawns != targetSpawns;
            }
        }

        #endregion

        #region Private variables

        private Dictionary<EnemyTemplate, GameObjectPool<Enemy>> enemyPools = new Dictionary<EnemyTemplate, GameObjectPool<Enemy>>();

        private int  currentWave = 0;
        private bool waveIsOngoing;

        /// <summary>
        /// How many enemies are currently spawned and roaming the map?
        /// </summary>
        private int spawnedEnemiesCount;

        /// <summary>
        /// How many enemies can at a maximum be roaming the map at the same time during this wave?
        /// </summary>
        private int spawnedEnemiesLimit;

        /// <summary>
        /// How many enemies have we spawned this wave so far?
        /// </summary>
        private int currentSpawns;

        /// <summary>
        /// How many enemies should we spawn during this wave?
        /// </summary>
        private int targetSpawns;

        /// <summary>
        /// The number of killed entities this wave.
        /// </summary>
        private int killedEnemies;

        #endregion

        public void BeginWaveController()
        {
            SetupEnemyPools();

            StartWave();
        }

        public void StartWave()
        {
            currentWave++;
            waveIsOngoing = true;

            spawnedEnemiesCount = 0;
            spawnedEnemiesLimit = CalculateMaxEnemyCount();
            currentSpawns       = 0;
            targetSpawns        = CalculateEnemySpawnCount();
            killedEnemies       = 0;

            WaveStartEvent waveStartEvent = WaveStartEvent.Create(GlobalTargets.Everyone);
            waveStartEvent.WaveNumber     = currentWave;
            waveStartEvent.EnemyCount     = targetSpawns;
            waveStartEvent.Send();

            WaveProgressEvent waveProgressEvent = WaveProgressEvent.Create(GlobalTargets.Everyone);
            waveProgressEvent.Progress          = 0f;
            waveProgressEvent.Send();

            StartCoroutine("WaveUpdate");
        }

        public void EndWave()
        {
            waveIsOngoing = false;

            WaveEndEvent waveEndEvent = WaveEndEvent.Create(GlobalTargets.Everyone);
            waveEndEvent.WaveNumber   = currentWave;
            waveEndEvent.Send();

            WaveProgressEvent waveProgressEvent = WaveProgressEvent.Create(GlobalTargets.Everyone);
            waveProgressEvent.Progress          = 1f;
            waveProgressEvent.Send();
        }

        public void ResetWaves()
        {
            currentWave = 0;
        }

        #region Wave helpers

        private IEnumerator WaveUpdate()
        {
            yield return new WaitForSecondsRealtime(WAVE_START_WAIT_TIME);

            float waveStartCountdown    = WAVE_COUNTDOWN_TIME;
            int   waveStartCountdownInt = Mathf.CeilToInt(waveStartCountdown);

            WaveCountdownEvent waveCountdownEvent = WaveCountdownEvent.Create(GlobalTargets.Everyone);
            waveCountdownEvent.Time               = waveStartCountdownInt;
            waveCountdownEvent.Send();

            while (waveStartCountdown > 0f)
            {
                waveStartCountdown -= Time.deltaTime;

                int newWaveStartCountdownInt = Mathf.CeilToInt(waveStartCountdown);

                if (waveStartCountdownInt != newWaveStartCountdownInt)
                {
                    waveStartCountdownInt = newWaveStartCountdownInt;

                    waveCountdownEvent      = WaveCountdownEvent.Create(GlobalTargets.Everyone);
                    waveCountdownEvent.Time = waveStartCountdownInt;
                    waveCountdownEvent.Send();
                }

                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSecondsRealtime(WAVE_RESET_NUMBER_WAIT_TIME);

            WaveNumberEvent waveNumberEvent = WaveNumberEvent.Create(GlobalTargets.Everyone);
            waveNumberEvent.Wave            = currentWave;
            waveNumberEvent.Send();

            while (waveIsOngoing && currentSpawns < targetSpawns)
            {
                if (spawnedEnemiesCount < spawnedEnemiesLimit)
                {
                    SpawnEnemy();
                }

                yield return new WaitForSeconds(CalculateEnemySpawnCooldown());
            }

            yield return new WaitUntil(() => spawnedEnemiesCount == 0);

            EndWave();

            yield return new WaitForSecondsRealtime(WAVE_END_WAIT_TIME);

            StartWave();
        }

        private void SpawnEnemy()
        {
            int tries    = 0;
            int index    = Random.Range(0, spawnableEnemyTemplates.Length);
            var template = spawnableEnemyTemplates[index];

            while (tries < spawnableEnemyTemplates.Length && enemyPools[template].PooledItemCount == 0)
            {
                tries++;
                index = (index + 1) % spawnableEnemyTemplates.Length;
                template = spawnableEnemyTemplates[index];
            }

            if (tries < spawnableEnemyTemplates.Length)
            {
                var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

                currentSpawns++;

                var enemy = enemyPools[template].GetItem();
                enemy.transform.position = spawnPoint.position;

                enemy.Revive(null);

                SetEntityActiveEvent setEntityActive = SetEntityActiveEvent.Create(GlobalTargets.AllClients);
                setEntityActive.Entity               = enemy.entity;
                setEntityActive.Active               = true;
                setEntityActive.Position             = spawnPoint.position;
                setEntityActive.Send();

                spawnedEnemiesCount++;
            }
        }

        public void DespawnEnemy(Enemy enemy)
        {
            enemyPools[enemy.EnemyTemplate].PoolItem(enemy);

            spawnedEnemiesCount--;

            killedEnemies++;

            WaveProgressEvent waveProgressEvent = WaveProgressEvent.Create(GlobalTargets.Everyone);
            waveProgressEvent.Progress          = Mathf.Clamp01(killedEnemies / (float)targetSpawns);
            waveProgressEvent.Send();
        }

        #endregion

        #region Helpers

        private void SetupEnemyPools()
        {
            enemyPools.Clear();

            foreach (var template in spawnableEnemyTemplates)
            {
                var pool = new GameObjectPool<Enemy>(null,
                                                     template.EnemyPrefab,
                                                     ENEMIES_IN_POOLS,
                                                     EntitySpawnController.Singleton.SpawnEntityOnServer<Enemy>);

                enemyPools.Add(template, pool);

                foreach (var enemy in pool.PooledItems)
                {
                    enemy.Initialize(template);
                }
            }
        }

        /// <summary>
        /// Calculates the spawn cooldown for the next enemy.
        /// </summary>
        private float CalculateEnemySpawnCooldown()
        {
            // Formula: ( 0.75 / d ) * ( 25 / (p * ( r + 1 ) ) )^0.25

            float difficulty = 1f + (int)ServerUtils.CurrentServerHostInfo.Difficulty * 0.25f; // 1 <= d <= 1.5 with 0.25 per player added
            float players    = BoltMatchmaking.CurrentSession.ConnectionsCurrent + 1;          // 1 <= p <= 4 where p is the current number of players connected

            return 0.75f / difficulty * Mathf.Pow(25 / (players * (currentWave + 1)), 0.25f);
        }

        /// <summary>
        /// Calculates the spawn count of enemies for this wave.
        /// </summary>
        private int CalculateEnemySpawnCount()
        {
            // Formula: d * ( 2p + pr + 6 )

            float difficulty = 1f + (int)ServerUtils.CurrentServerHostInfo.Difficulty * 0.25f; // 1 <= d <= 1.5 with 0.25 per player added
            float players    = BoltMatchmaking.CurrentSession.ConnectionsCurrent + 1;          // 1 <= p <= 4 where p is the current number of players connected

            return Mathf.RoundToInt(difficulty * (2 * players + players * currentWave + 6));
        }

        /// <summary>
        /// Calculates the max amount of enemies present on the map simultaneously.
        /// </summary>
        private int CalculateMaxEnemyCount()
        {
            /// Formula: min ( max ( dp * (r + 1) / 2.5 ), <see cref="MIN_ENEMIES_ON_MAP"/> ), <see cref="MAX_ENEMIES_ON_MAP"/> )

            float difficulty = 1f + (int)ServerUtils.CurrentServerHostInfo.Difficulty * 0.25f; // 1 <= d <= 1.5 with 0.25 per player added
            float players    = BoltMatchmaking.CurrentSession.ConnectionsCurrent + 1;          // 1 <= p <= 4 where p is the current number of players connected

            return (int)Mathf.Clamp(difficulty * players * (currentWave + 1) / 2.5f, MIN_ENEMIES_ON_MAP, MAX_ENEMIES_ON_MAP);
        }

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();

            enemyPools.Clear();
        }

    }

}
