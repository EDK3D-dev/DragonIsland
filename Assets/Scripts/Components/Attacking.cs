﻿using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    [DisallowMultipleComponent]
    public abstract class Attacking : MonoBehaviour
    {

        public Unit target
        {
            get;
            protected set;
        }

        protected Unit owner;

        protected Animator animator;

        protected Coroutine chaseAndAttack;
        public bool IsAttacking()
        {
            if (!target) return false;
            if (target.IsDead) return false;
            return chaseAndAttack != null;
        }

        protected float timeSinceAttack;


        //TODO to be used for autoattackMove, automatically attacks entering units
        [SerializeField]
        private SphereCollider atkTrigger;

        public SphereCollider AtkTrigger => atkTrigger;

        [SerializeField]
        protected Transform rangeIndicator;

        public Transform RangeIndicator => rangeIndicator;

        [SerializeField]
        protected bool lookAtTarget = true;

        public Unit CurrentTarget
        {
            protected set
            {
                target = value;
            }
            get => target;
        }

        protected PhotonView photonView;

        public void StartAttacking(Unit _target)
        {
            if (CurrentTarget == _target) return;
            if (!owner.canAttack) return;
            if (!_target)
            {
                Stop();
                return;
            }
            if (_target.IsDead)
            {
                Stop();
                return;
            }
            if (chaseAndAttack != null)
            {
                StopCoroutine(chaseAndAttack);
            }
            target = _target;
            owner.CanMove = false;
            chaseAndAttack = StartCoroutine(ChaseAndAttack());
        }

        protected IEnumerator ChaseAndAttack()
        {
            while (true)
            {
                if (!target)
                {
                    Stop();
                    break;
                }
                if (target.IsDead)
                {
                    Stop();
                    break;
                }
                if (lookAtTarget)
                {
                    transform.LookAt(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z));
                }
                if (!owner.canAttack)
                {
                    yield return null;
                    continue;
                }
                if (Vector3.Distance(owner.GetGroundPos(), target.GetGroundPos()) > owner.Stats.AtkRange + target.Radius)
                {
                    owner.CanMove = true;
                    owner.MoveTo(target.transform.position);
                    yield return null;
                    continue;
                }
                else owner.CanMove = false;
                if (timeSinceAttack >= 1 / owner.Stats.AtkSpeed)
                {
                    photonView.RPC(nameof(Attack), RpcTarget.All, target.GetViewID());
                    owner.CanMove = false;
                    timeSinceAttack = 0;
                }
                yield return null;
                continue;
            }
        }

        public void Stop()
        {
            if (chaseAndAttack != null)
            {
                StopCoroutine(chaseAndAttack);
                chaseAndAttack = null;
            }
            target = null;
            owner.CanMove = true;
        }

        public abstract void Attack(int targetViewID);

        public virtual void Initialize(Unit _owner)
        {
            owner = _owner;
            photonView = owner.GetComponent<PhotonView>();
            timeSinceAttack = 1 / owner.Stats.AtkSpeed;
            animator = GetComponent<Animator>();
        }

        protected void AttackAnimFinished()
        {
        }

        private void Update()
        {
            timeSinceAttack += Time.deltaTime;
        }
    }

}