﻿using ArenaShooter.AI;
using ArenaShooter.Combat;
using ArenaShooter.Controllers;
using ArenaShooter.Extensions;
using ArenaShooter.Templates.Enemies;
using ArenaShooter.UI;
using Bolt;
using System;
using UnityEngine;

#pragma warning disable 0649

namespace ArenaShooter.Entities
{

    /// <summary>
    /// Enemies only exist on the server (or host).
    /// </summary>
    class Enemy : Entity<IEnemyState>, IWeaponHolder, IAIAgentBehaviour
    {

        #region Editor

        [Header("References")]
        [SerializeField] private AIAgent aiAgent;
        [SerializeField] private Body    body;

        [Space]
        [SerializeField] private Transform renderTransform;

        [Header("Values")]
        [SerializeField] private LayerMask weaponHitLayerMask;

        [Header("Prefabs")]
        [SerializeField] private GameObject uiEnemyGameStatsPrefab;

        #endregion

        #region IEntity

        public override EntityTeam EntityTeam
        {
            get
            {
                return EntityTeam.Enemy;
            }
        }

        public override HealableBy HealableBy
        {
            get
            {
                return HealableBy.Enemy;
            }
        }

        #endregion

        #region IWeaponHolder

        public Vector3 WeaponFirePosition
        {
            get
            {
                return transform.position + Vector3.up;
            }
        }

        public Vector3 WeaponForward
        {
            get
            {
                return body.UpperBodyCurrent;
            }
        }

        public LayerMask WeaponHitLayerMask
        {
            get
            {
                return weaponHitLayerMask;
            }
        }

        public GlobalTargets WeaponTargets
        {
            get
            {
                return GlobalTargets.Everyone;
            }
        }

        public Weapon Weapon
        {
            get
            {
                return weapon;
            }
        }

        #endregion

        #region IAIAgentBehaviour

        public EntityTeam SearchTargetTeam
        {
            get
            {
                return weapon.Stats.TargetEntityTeam;
            }
        }

        public float SearchInterval
        {
            get
            {
                return enemyTemplate.TargetSearchFrequency;
            }
        }

        public float SearchThreshold
        {
            get
            {
                return weapon.Stats.Range * 0.9f;
            }
        }

        public float StoppingDistance
        {
            get
            {
                return weapon.Stats.Range / 2f;
            }
        }

        public float MovementSpeed
        {
            get
            {
                return enemyTemplate.MovementSpeed;
            }
        }

        public float TurnSpeed
        {
            get
            {
                return enemyTemplate.TurnSpeed;
            }
        }

        public Body Body
        {
            get
            {
                return body;
            }
        }

        #endregion

        #region Public properties

        public EnemyTemplate EnemyTemplate
        {
            get
            {
                return enemyTemplate;
            }
        }

        #endregion

        #region Protected variables

        protected EnemyTemplate enemyTemplate;
        protected Weapon        weapon;

        protected UIEnemyGameStats uiEnemyGameStats;

        protected bool isOwner;

        #endregion

        #region Initializing

        /// <summary>
        /// Initializes the enemy which references a template (using <paramref name="enemyTemplateId"/>) and creates the enemy's weapon.
        /// </summary>
        public void Initialize(EnemyTemplate enemyTemplate)
        {
            this.enemyTemplate = enemyTemplate;
            this.weapon        = WeaponController.Singleton.CreateWeapon(enemyTemplate.GetEnemyWeaponTemplate(), transform);

            state.Health           = enemyTemplate.Health;
            state.EnemyTemplateId  = enemyTemplate.TemplateId;
            state.WeaponTemplateId = weapon.Stats.GetEnemyWeaponTemplateId();
            state.Dead             = true;

            aiAgent.Initialize(this);
        }

        protected override void Start()
        {
            base.Start();

            if (!entity.IsOwner)
            {
                this.enemyTemplate = EnemyTemplateController.Singleton.GetEnemyTemplate((ushort)state.EnemyTemplateId);
                this.weapon        = WeaponController.Singleton.CreateWeapon(WeaponController.Singleton.GetEnemyWeaponTemplate((ushort)state.WeaponTemplateId), transform);
            }

            this.weapon.EquipWeapon(this);
        }

        public override void Attached()
        {
            uiEnemyGameStats = Instantiate(uiEnemyGameStatsPrefab, UIGameController.Singleton.EnemyOverlayContainer).GetComponent<UIEnemyGameStats>();
            uiEnemyGameStats.Initialize(this);
            uiEnemyGameStats.transform.position = MainCameraController.MainCamera.WorldToScreenPoint(transform.position);
            uiEnemyGameStats.gameObject.SetActive(false);

            state.SetTransforms(state.Transform, transform, renderTransform);

            if (entity.IsOwner)
            {
                entity.TakeControl();
            }
            else
            {
                Destroy(aiAgent);
            }

            state.AddCallback("Health", uiEnemyGameStats.UpdateUI);

            body.ManualControls = !entity.IsOwner;

            this.isOwner = entity.IsOwner;
        }

        #endregion

        #region Updating

        private void Update()
        {
            uiEnemyGameStats.transform.position = MainCameraController.MainCamera.WorldToScreenPoint(transform.position);

            if (!entity.IsOwner)
            {
                body.UpperBodyCurrent = state.UpperBodyNormal;
                body.LowerBodyCurrent = state.LowerBodyNormal;
            }
        }

        private void FixedUpdate()
        {
            if (entity.IsOwner && !state.Dead)
            {
                CheckForTargets();
            }
        }

        public override void SimulateOwner()
        {
            state.UpperBodyNormal = body.UpperBodyCurrent;
            state.LowerBodyNormal = body.LowerBodyCurrent;
        }

        #endregion

        #region Destroying

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (uiEnemyGameStats != null)
            {
                Destroy(uiEnemyGameStats.gameObject);
            }
        }

        #endregion

        #region Life

        public override void TakeDamage(TakeDamageEvent takeDamageEvent)
        {
            state.SetDynamic("Health", Mathf.Clamp((int)state.GetDynamic("Health") - takeDamageEvent.DamageTaken, 0, int.MaxValue));

            if ((int)state.GetDynamic("Health") <= 0)
            {
                var entityDeathEvent                          = EntityDiedEvent.Create(GlobalTargets.Everyone, ReliabilityModes.ReliableOrdered);
                entityDeathEvent.DeadEntity                   = entity;
                entityDeathEvent.WeaponPartItemTemplateDropId = enemyTemplate.GetWeaponPartItemTemplate()?.Id ?? -1;
                entityDeathEvent.Send();
            }
        }

        public override void Revive(EntityRevivedEvent @event)
        {
            base.Revive(@event);

            if (entity.IsOwner)
            {
                state.Health = enemyTemplate.Health;
                
                // TODO: Reset weapon and reload it instantly.
            }

            uiEnemyGameStats.gameObject.SetActive(true);
            uiEnemyGameStats.UpdateUI();
            Update();

            if (aiAgent != null && isOwner)
            {
                aiAgent.StartAI();
            }
        }

        public override void Die(EntityDiedEvent @event)
        {
            base.Die(@event);

            uiEnemyGameStats.gameObject.SetActive(false);

            if (@event.WeaponPartItemTemplateDropId != -1)
            {
                WeaponPartItemController.Singleton.SpawnWeaponPartItemDrop(BodyOriginPosition, WeaponPartItemController.Singleton.GetWeaponPartItemTemplate(@event.WeaponPartItemTemplateDropId));
            }

            if (WaveController.Singleton != null)
            {
                WaveController.Singleton.DespawnEnemy(this);
            }

            if (aiAgent != null && isOwner)
            {
                aiAgent.StopAI();
            }
        }

        #endregion

        #region Combat

        private void CheckForTargets()
        {
            var hit = Utils.Raycast(new Ray(BodyOriginPosition, body.UpperBodyCurrent), weapon.Stats.Range, Physics.AllLayers, gameObject, QueryTriggerInteraction.Ignore);
            
            if (hit.GameObject != null && hit.GameObject.GetComponent<IEntity>() is IEntity entity)
            {
                if (entity.EntityTeam == weapon.Stats.TargetEntityTeam)
                {
                    weapon.FireWithoutInput();

                    return;
                }
            }

            weapon.StopFiring();
        }

        #endregion

        #region OnEvent

        public override void OnEvent(WeaponFireEffectEvent evnt)
        {
            if (evnt.Shooter == entity)
            {
                weapon?.OnEvent(evnt);
            }
        }

        public override void OnEvent(WeaponSupportBeginFireEffectEvent evnt)
        {
            if (evnt.Shooter == entity)
            {
                weapon?.OnEvent(evnt);
            }
        }

        #endregion

    }

}
