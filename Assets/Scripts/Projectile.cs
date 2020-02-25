﻿using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{

    public enum HitMode
    {
        invalid = -1,
        targetOnly = 0,
        enemyChamps = 1,
        enemyUnits = 2,
        alliedChamps = 3,
        alliedUnits = 4,
        monsters = 5,
        anyUnit = 6,
        ownerOnly = 7
    }


    [Serializable]
    public class ProjectileProperties
    {
        public bool isHoming;

        public float speed;

        public float size;

        public float sizePerRank;

        public HitMode hitMode;

        public DamageType dmgType;

        [Tooltip("For regular ranged autoattacks scaling 1:1 with AD, this should be 0.")]
        public float baseDamage;

        public float damagePerRank;

        [Range(-1, 60), Tooltip("Leave at -1 for infinite.")]
        public float lifespan = -1;

        public float lifespanPerRank;

        [Space]
        public bool hitUntargetables = false;

        public bool destroyOnNonTargetHit = false;

        public bool canHitStructures = false;

    }

    //TODO separate instantiating / initializing
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        private ProjectileProperties properties;

        [SerializeField]
        private Movement movement;

        private Unit owner;

        [HideInInspector]
        public bool waitForDestroyRPC;

        /// <summary>
        /// Used to store team id of owner in case owner is destroyed on hit.
        /// </summary>
        private TeamID ownerTeamID;

        private Unit target;

        private Vector3 targetPos;

        private Vector3 targetDir;

        private List<SkillEffect> onHitEffects;

        private float remainingLifetime;

        private Scalings scaling;

        private UnitStats ownerStatsAtSpawn;

        private PhotonView ownerView;

        [SerializeField]
        private ParticleSystem onHitVFX;

        [Tooltip("Let child effects use their scaling or override with own?")]
        public bool overrideChildScalings;

        [SerializeField, Tooltip("Activate effects when projectile dies (not only on hit)?")]
        private bool activateEffectsOnDestroy;

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;
            var unit = other.GetComponent<Unit>();
            if (!unit) return;
            if (unit.IsDead) return;

            if (unit is Structure)
            {
                if (!properties.canHitStructures) return;
            }

            switch (properties.hitMode)
            {
                case HitMode.targetOnly:
                    if (unit == target)
                    {
                        OnHit(unit);
                    }
                    break;
                case HitMode.enemyChamps:
                    if (unit.GetComponent<Champ>())
                    {
                        if (unit.IsEnemy(ownerTeamID))
                        {
                            OnHit(unit);
                        }
                    }
                    break;
                case HitMode.enemyUnits:
                    if (unit.IsEnemy(ownerTeamID))
                    {
                        OnHit(unit);
                    }
                    break;
                case HitMode.alliedChamps:
                    if (unit.GetComponent<Champ>())
                    {
                        if (unit.IsAlly(ownerTeamID))
                        {
                            OnHit(unit);
                        }
                    }
                    break;
                case HitMode.alliedUnits:
                    if (unit.IsAlly(ownerTeamID))
                    {
                        OnHit(unit);
                    }
                    break;
                case HitMode.monsters:
                    if (unit is Monster)
                    {
                        OnHitMonster((Monster)unit);
                    }
                    break;
                case HitMode.anyUnit:
                    if (unit is Monster)
                    {
                        OnHitMonster((Monster)unit);
                    }
                    else
                    {
                        OnHit(unit);
                    }
                    break;
                default:
                    Debug.LogError(owner.name + "spawned projectile with invalid hit mode!");
                    break;
            }
        }

        protected virtual void OnHit(Unit unit)
        {
            if (ownerView)
            {
                if (ownerView.IsMine)
                {
                    if (properties.hitUntargetables || unit.damageable)
                    {
                        var dmg = new Damage(properties.baseDamage + scaling.GetScalingDamageBonusOnTarget(ownerStatsAtSpawn, unit), properties.dmgType, owner, unit);
                        dmg.Inflict();
                        foreach (var effect in onHitEffects)
                        {
                            effect.Activate(transform.position.NullY(), unit);
                        }
                    }
                }
            }

            Instantiate(onHitVFX.gameObject, Vector3.Lerp(transform.position, unit.transform.position, 0.5f), Quaternion.identity);

            if (unit == target)
            {
                OnHitTarget();
            }
            else if (properties.destroyOnNonTargetHit)
            {
                if (waitForDestroyRPC)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    PhotonView.Get(this)?.RPC(nameof(DestroyRPC), RpcTarget.Others);
                    Destroy(gameObject);
                }
            }
        }

        [PunRPC]
        public void DestroyRPC()
        {
            Destroy(gameObject);
        }

        protected virtual void OnHitMonster(Monster monster)
        {
            OnHit(monster);
        }

        private void OnHitTarget()
        {
            if (waitForDestroyRPC)
            {
                gameObject.SetActive(false);
            }
            else
            {
                PhotonView.Get(this)?.RPC(nameof(DestroyRPC), RpcTarget.Others);
                Destroy(gameObject);
            }
        }

        public void Initialize(Unit _owner, ProjectileProperties _properties, Scalings _scaling, TeamID teamID, UnitStats ownerStats)
        {
            properties = _properties;
            if (properties.lifespan > 0)
            {
                remainingLifetime = properties.lifespan;
            }
            movement.SetSpeed(properties.speed);
            transform.localScale *= properties.size;
            ownerStatsAtSpawn = ownerStats;
            if (_owner)
            {
                owner = _owner;
                ownerView = owner.GetComponent<PhotonView>();
                if (ownerView)
                {
                    if (ownerView.IsMine)
                    {
                        onHitEffects = new List<SkillEffect>(GetComponents<SkillEffect>());
                        foreach (var effect in onHitEffects)
                        {
                            effect.Initialize(owner, 0);
                            if (overrideChildScalings)
                            {
                                effect.SetScaling(scaling);
                            }
                            effect.SetStatsAtActivation(ownerStatsAtSpawn);
                        }
                    }
                }
            }
            ownerTeamID = teamID;
            scaling = _scaling;
        }

        /// <summary>
        /// Spawns a stillstanding but initialized projectile.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="_properties"></param>
        /// <returns></returns>
        private Projectile Spawn(Unit _owner, Vector3 position, ProjectileProperties _properties, Scalings _scaling, TeamID teamID, UnitStats ownerStats)
        {
            Projectile instance = Instantiate(gameObject, position, Quaternion.identity).GetComponent<Projectile>();
            instance.Initialize(_owner, _properties, _scaling, teamID, ownerStats);
            return instance;
        }

        /// <summary>
        /// Spawns a projectile flying after a unit or to its position at spawn time with given properties, _properties determine whether it is homing.
        /// </summary>
        /// <param name="_owner"></param>
        /// <param name="_target"></param>
        /// <param name="position"></param>
        /// <param name="_properties"></param>
        /// <returns></returns>
        public Projectile Spawn(Unit _owner, Unit _target, Vector3 position, ProjectileProperties _properties, Scalings _scaling, TeamID teamID, UnitStats ownerStats)
        {
            Projectile instance = Spawn(_owner, position, _properties, _scaling, teamID, ownerStats);
            instance.target = _target;
            instance.targetPos = _owner.transform.position;
            instance.targetDir = (_target.transform.position - position).normalized;
            instance.targetDir.y = 0;
            return instance;
        }

        /// <summary>
        /// Spawns a projectile flying to a position with given properties, cannot be homing.
        /// </summary>
        /// <param name="_owner"></param>
        /// <param name="_targetPos"></param>
        /// <param name="position"></param>
        /// <param name="_properties"></param>
        /// <returns></returns>
        public Projectile SpawnSkillshot(Unit _owner, Vector3 _targetPos, Vector3 position, ProjectileProperties _properties, Scalings _scalings, TeamID teamID, UnitStats ownerStats)
        {
            Projectile instance = Spawn(_owner, position, _properties, _scalings, teamID, ownerStats);
            instance.targetPos = _targetPos;
            instance.targetDir = (_targetPos - position).normalized;
            instance.targetDir.y = 0;
            if (_properties.isHoming)
            {
                instance.properties.isHoming = false;
                Debug.LogWarning(owner.name + " tried to spawn a homing projectile given only a target position instead of a target unit!");
            }
            return instance;
        }


        private void Update()
        {
            Move();

            if (properties.lifespan < 0) return;
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime < 0)
            {
                if (ownerView)
                {
                    if (ownerView.IsMine)
                    {
                        if (activateEffectsOnDestroy)
                        {
                            foreach (var effect in onHitEffects)
                            {
                                effect.Activate(transform.position.NullY());
                            }
                        }
                    }
                }
                Destroy(gameObject);
            }
        }

        private void Move()
        {
            if (properties.isHoming)
            {
                if (!target || target.IsDead)
                {
                    Destroy(gameObject);
                    return;
                }
                if (!properties.hitUntargetables)
                {
                    if (!target.Targetable)
                    {
                        Destroy(gameObject);
                    }
                }
                movement.MoveTo(target.transform.position);
            }
            else
            {
                movement.MoveTo(transform.position + targetDir);
            }
        }

    }
}
