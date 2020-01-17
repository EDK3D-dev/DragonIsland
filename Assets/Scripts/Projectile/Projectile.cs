﻿using System.Collections;
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
        HitModeCount = 7

    }

    [RequireComponent(typeof(Collider))]
    public abstract class Projectile : MonoBehaviour
    {
        [SerializeField]
        protected bool destroyOnNonTargetHit = false;

        [SerializeField]
        protected bool canHitStructures = false;

        [SerializeField]
        protected Movement movement;

        [SerializeField]
        protected float speed;

        [SerializeField]
        protected HitMode hitMode;

        protected Unit target;

        protected Vector3 targetPos;

        protected Unit owner;

        protected float damage;

        protected DamageType dmgType;

        [SerializeField]
        protected GameObject prefab;

        private void OnTriggerEnter(Collider other)
        {
            var unit = other.GetComponent<Unit>();
            if (!unit) return;

            if (unit is Structure)
            {
                if (!canHitStructures) return;
            }

            switch (hitMode)
            {
                case HitMode.invalid:
                    print("encountered invalid projectile hit mode!");
                    break;
                case HitMode.targetOnly:
                    if (unit == target)
                    {
                        OnHit(unit);
                    }
                    break;
                case HitMode.enemyChamps:
                    if (unit.GetComponent<Champ>())
                    {
                        if (owner.IsEnemy(unit))
                        {
                            OnHit(unit);
                        }
                    }
                    break;
                case HitMode.enemyUnits:
                    if (owner.IsEnemy(unit))
                    {
                        OnHit(unit);
                    }
                    break;
                case HitMode.alliedChamps:
                    if (unit.GetComponent<Champ>())
                    {
                        if (owner.IsAlly(unit))
                        {
                            OnHit(unit);
                        }
                    }
                    break;
                case HitMode.alliedUnits:
                    if (owner.IsAlly(unit))
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
                case HitMode.HitModeCount:
                    break;
                default:
                    break;
            }
        }

        protected virtual void OnHit(Unit unit)
        {
            unit.ReceiveDamage(owner, damage, dmgType);

            if (unit == target)
            {
                OnHitTarget();
            }
            else if (destroyOnNonTargetHit)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnHitMonster(Monster monster)
        {
            monster.ReceiveDamage(owner, damage, dmgType);

            if (monster == target)
            {
                OnHitTarget();
            }
            else if (destroyOnNonTargetHit)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnReachedDestination()
        {
            Destroy(gameObject);
        }

        protected virtual void OnHitTarget()
        {
            Destroy(gameObject);
        }

        protected Projectile Spawn(Vector3 position, Unit _owner, float _damage, float _speed, HitMode _hitMode, DamageType _dmgType, bool _destroyOnNonTargetHit = false, bool _canHitStructures = false)
        {
            Projectile instance = Instantiate(prefab, position, Quaternion.identity).GetComponent<Projectile>();
            instance.owner = _owner;
            instance.damage = _damage;
            instance.speed = _speed;
            instance.hitMode = _hitMode;
            instance.dmgType = _dmgType;
            instance.destroyOnNonTargetHit = _destroyOnNonTargetHit;
            instance.canHitStructures = _canHitStructures;
            return instance;
        }

        public void SpawnSkillshot(Unit _target, Vector3 position, Unit _owner, float _damage, float _speed, HitMode _hitMode, DamageType _dmgType, bool _destroyOnNonTargetHit = false, bool _canHitStructures = false)
        {
            Projectile instance = Spawn(position, _owner, _damage, _speed, _hitMode, _dmgType, _destroyOnNonTargetHit, _canHitStructures);
            instance.target = _target;
        }

        public void SpawnHoming(Vector3 _targetPos, Vector3 position, Unit _owner, float _damage, float _speed, HitMode _hitMode, DamageType _dmgType, bool _destroyOnNonTargetHit = false, bool _canHitStructures = false)
        {

        }

        protected virtual void Update()
        {
            if (target)
            {
                movement.MoveTo(target.transform.position);
            }
            else
            {
                movement.MoveTo(targetPos);
                if (Vector3.Distance(transform.position, targetPos) <= 0.01f)
                {
                    OnReachedDestination();
                }
            }
        }


    }
}