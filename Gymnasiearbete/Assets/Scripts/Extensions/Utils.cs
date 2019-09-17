﻿using ArenaShooter.Entities;
using Bolt.LagCompensation;
using System.Linq;
using UnityEngine;

namespace ArenaShooter.Extensions
{

    static class Utils
    {

        #region Structs

        internal struct UtilRaycastHit
        {

            public Vector3 HitPoint  { get; private set; }
            public Vector3 HitNormal { get; private set; }

            public RaycastHit?     RaycastHit     { get; private set; }
            public BoltPhysicsHit? BoltPhysicsHit { get; private set; }

            public GameObject GameObject
            {
                get
                {
                    return HitAnything ? (NetworkHit ? Body.gameObject : Collider.gameObject) : null;
                }
            }

            public Collider Collider
            {
                get
                {
                    return RaycastHit.Value.collider;
                }
            }

            public BoltHitbox Hitbox
            {
                get
                {
                    return BoltPhysicsHit.Value.hitbox;
                }
            }

            public BoltHitboxBody Body
            {
                get
                {
                    return BoltPhysicsHit.Value.body;
                }
            }

            public bool NetworkHit
            {
                get
                {
                    return BoltPhysicsHit.HasValue;
                }
            }

            public bool WorldHit
            {
                get
                {
                    return RaycastHit.HasValue;
                }
            }

            public bool HitAnything
            {
                get
                {
                    return RaycastHit.HasValue || BoltPhysicsHit.HasValue;
                }
            }

            public float Distance
            {
                get
                {
                    if (RaycastHit.HasValue)
                    {
                        return RaycastHit.Value.distance;
                    }
                    else if (BoltPhysicsHit.HasValue)
                    {
                        return BoltPhysicsHit.Value.distance;
                    }

                    return 0f;
                }
            }

            public UtilRaycastHit(RaycastHit? raycastHit, Vector3 hitPoint, Vector3 hitNormal)
            {
                this.HitPoint       = hitPoint;
                this.HitNormal      = hitNormal;
                this.RaycastHit     = raycastHit;
                this.BoltPhysicsHit = null;
            }

            public UtilRaycastHit(BoltPhysicsHit? boltPhysicsHit, Vector3 hitPoint, Vector3 hitNormal)
            {
                this.HitPoint       = hitPoint;
                this.HitNormal      = hitNormal;
                this.RaycastHit     = null;
                this.BoltPhysicsHit = boltPhysicsHit;
            }

        }

        #endregion

        #region Layers

        public static bool HasLayer(this LayerMask layerMask, int layer)
        {
            return layerMask == (layerMask | (1 << layer));
        }

        #endregion

        #region Raycasting

        /// <summary>
        /// Performs a raycast operation, first on the geometry, and if no colliders were hit, it performs one on the network.
        /// </summary>
        /// <param name="ray">The ray to cast with.</param>
        /// <param name="maxDistance">Max distance of the raycast.</param>
        /// <param name="hitLayerMask">What the raycast can hit.</param>
        /// <param name="self">The object to be ignored.</param>
        /// <param name="queryTriggerInteraction">Raycast trigger result.</param>
        /// <returns>Returns a <see cref="UtilRaycastHit"/> that contains information about the raycast operation.</returns>
        public static UtilRaycastHit Raycast(Ray ray, float maxDistance, LayerMask hitLayerMask, GameObject self, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            // Search geometry colliders for hits:
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayerMask, queryTriggerInteraction))
            {
                if (hit.collider != null && hit.collider.gameObject != self)
                {
                    return new UtilRaycastHit(hit, hit.point, hit.normal);
                }
            }

            // No geometry was hit, search network hitboxes for hits:
            using (var hitCollection = BoltNetwork.RaycastAll(new Ray(ray.origin, ray.direction * maxDistance), BoltNetwork.ServerFrame))
            {
                BoltPhysicsHit[] boltHits = new BoltPhysicsHit[hitCollection.count];

                for (int i = 0; i < boltHits.Length; i++)
                {
                    boltHits[i] = hitCollection[i];
                }

                // Order the hits by distance from ray.origin.
                boltHits = boltHits.OrderBy(h => h.distance).ToArray();

                // Search network hitboxes for hits:
                foreach (var boltHit in boltHits)
                {
                    var hitbox = boltHit.hitbox;

                    if (hitbox.gameObject != self && hitLayerMask.HasLayer(hitbox.gameObject.layer) && Vector3.Distance(hitbox.transform.position, ray.origin) < maxDistance)
                    {
                        return new UtilRaycastHit(boltHit, ray.origin + ray.direction * boltHit.distance, -ray.direction);
                    }
                }
            }

            return new UtilRaycastHit();
        }

        /// <summary>
        /// Performs a raycast operation, first on the geometry, and if no colliders were hit, it performs one on the network.
        /// </summary>
        /// <typeparam name="T">The type of entity to be hit. <see cref="Entity{T}"/> or <see cref="IEntity"/> can also be passed.</typeparam>
        /// <param name="ray">The ray to cast with.</param>
        /// <param name="maxDistance">Max distance of the raycast.</param>
        /// <param name="hitLayerMask">What the raycast can hit.</param>
        /// <param name="self">The object to be ignored.</param>
        /// <param name="queryTriggerInteraction">Raycast trigger result.</param>
        /// <returns>Returns a <see cref="UtilRaycastHit"/> that contains information about the raycast operation.</returns>
        public static UtilRaycastHit Raycast<T>(Ray ray, float maxDistance, LayerMask hitLayerMask, GameObject self, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) where T : IEntity
        {
            // Search geometry colliders for hits:
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayerMask, queryTriggerInteraction))
            {
                if (hit.collider != null && hit.collider.gameObject != self && hit.collider.GetComponent<T>() != null)
                {
                    return new UtilRaycastHit(hit, hit.point, hit.normal);
                }
            }

            // No geometry was hit, search network hitboxes for hits:
            using (var hitCollection = BoltNetwork.RaycastAll(new Ray(ray.origin, ray.direction * maxDistance), BoltNetwork.ServerFrame))
            {
                BoltPhysicsHit[] boltHits = new BoltPhysicsHit[hitCollection.count];

                for (int i = 0; i < boltHits.Length; i++)
                {
                    boltHits[i] = hitCollection[i];
                }

                // Order the hits by distance from ray.origin.
                boltHits = boltHits.OrderBy(h => h.distance).ToArray();

                // Search network hitboxes for hits:
                foreach (var boltHit in boltHits)
                {
                    var hitbox = boltHit.hitbox;

                    if (hitbox.gameObject != self && hitLayerMask.HasLayer(hitbox.gameObject.layer) && hitbox.GetComponent<T>() != null && Vector3.Distance(hitbox.transform.position, ray.origin) < maxDistance)
                    {
                        return new UtilRaycastHit(boltHit, ray.origin + ray.direction * boltHit.distance, -ray.direction);
                    }
                }
            }

            return new UtilRaycastHit();
        }

        #endregion

        #region IEntity

        public static bool IsNull(this IEntity entity)
        {
            // Taken from http://answers.unity.com/answers/586188/view.html in https://answers.unity.com/questions/586144/destroyed-monobehaviour-not-comparing-to-null.html.
            return entity == null || entity.Equals(null);
        }

        public static bool IsSame(this IEntity entity, IEntity other)
        {
            return !IsNull(entity) ? entity.Equals(other) : entity == other;
        }

        #endregion

    }

}
