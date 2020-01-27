﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace MOBA
{
    //TODO scaling
    public class ApplyBuff : SkillEffect
    {
        [Space]
        [SerializeField, Tooltip("Buff targets and owner?")]
        private bool alwaysBuffOwner;

        [SerializeField, Tooltip("Buff only owner?")]
        private bool onlyBuffOwner;

        [Space]
        [SerializeField]
        private BuffProperties properties;

        [Space]
        [SerializeField]
        private bool addStatBuff;

        [SerializeField]
        private BuffStats stats;

        [Space]
        [SerializeField]
        private bool addDisable;

        [SerializeField]
        private BuffFlags disableFlags;

        [Space]
        [SerializeField]
        private bool addDamageOverTime;

        [SerializeField]
        private float baseDamage;



        [SerializeField]
        private DamageType dmgType;


        private List<Buff> addedBuffs;

        /// <summary>
        /// Format: ClassName,arg1,arg2,... (Class must derive from CustomBuff and be in MOBA namespace, no spaces after comma)
        /// </summary>
        [Space]
        [SerializeField, Tooltip("Format: ClassName,BuffNameSuffix,arg1,... (Class must derive from CustomBuff and be in MOBA namespace, no spaces after comma)")]
        private List<string> customBuffs;

        public override void Initialize(Unit _owner, int _rank)
        {
            base.Initialize(_owner, _rank);
            addedBuffs = new List<Buff>();
            properties.instigator = owner;
        }

        public override void Activate(Vector3 targetPos, UnitStats ownerStats)
        {
            base.Activate(targetPos, ownerStats);
            Activate(owner, ownerStats);
        }

        public override void Activate(Unit target, UnitStats ownerStats)
        {
            base.Activate(target, ownerStats);
            if (onlyBuffOwner)
            {
                AddBuffs(owner);
                return;
            }
            if (alwaysBuffOwner)
            {
                if (target != owner)
                {
                    AddBuffs(owner);
                }
            }
            AddBuffs(target);
        }

        private void AddBuffs(Unit target)
        {
            if (!target) return;
            if (addStatBuff)
            {
                if (target.HasBuff(properties.buffBaseName + StatBuff.NAMESUFFIX, out var existing))
                {
                    existing.Refresh();
                }
                else
                {
                    var buff = target.AddBuff<StatBuff>();
                    buff.Initialize(properties, ownerStatsAtActivation, stats);
                    addedBuffs.Add(buff);
                }
            }
            if (addDisable)
            {
                if (target.HasBuff(properties.buffBaseName + Disable.NAMESUFFIX, out var existing))
                {
                    existing.Refresh();
                }
                else
                {
                    var buff = target.AddBuff<Disable>();
                    buff.Initialize(properties, ownerStatsAtActivation, disableFlags);
                    addedBuffs.Add(buff);
                }
            }
            if (addDamageOverTime)
            {
                if (target.HasBuff(properties.buffBaseName + DamageOverTime.NAMESUFFIX, out var existing))
                {
                    existing.Refresh();
                }
                else
                {
                    var buff = target.AddBuff<DamageOverTime>();
                    buff.Initialize(properties, ownerStatsAtActivation, target);
                    addedBuffs.Add(buff);
                }
            }
            foreach (var customBuff in customBuffs)
            {
                TryAddCustomBuff(target, customBuff);
            }
        }

        private void TryAddCustomBuff(Unit target, string customBuff)
        {
            var customBuffArgs = new List<string>(customBuff.Split(','));
            var customBuffName = customBuffArgs[0];
            customBuffArgs.RemoveAt(0);
            if (customBuffArgs.Count == 0)
            {
                Debug.LogError(owner.name + " " + gameObject.name + " tried to add custom buff with too few args! Format: ClassName,NameSuffix,arg1,...");
                return;
            }
            if (target.HasBuff(properties.buffBaseName + " (" + customBuffArgs[0] + ")", out var existing))
            {
                existing.Refresh();
                return;
            }
            var buffTypeName = GetType().Namespace + "." + customBuffName;
            var buffType = Type.GetType(buffTypeName);
            if (buffType == null)
            {
                Debug.LogError(owner.name + " " + gameObject.name + " tried to add buff '" + buffTypeName + "', but no such class could be found!");
                return;
            }
            if (!buffType.IsSubclassOf(typeof(CustomBuff)))
            {
                Debug.LogError(owner.name + " " + gameObject.name + " tried to add buff '" + customBuffName + "', which doesn't derive from 'CustomBuff'!");
                return;
            }
            var buff = target.AddCustomBuff(buffType);
            buff.Initialize(properties, ownerStatsAtActivation, customBuffArgs);
            addedBuffs.Add(buff);
        }

        public override void Tick(UnitStats ownerStats)
        {
        }

        protected override void OnDeactivated()
        {
            foreach (Buff buff in addedBuffs)
            {
                Destroy(buff);
            }
        }

        public override void Activate<T>(UnitList<T> targets, UnitStats ownerStats)
        {
            foreach (var target in targets)
            {
                Activate(target, ownerStats);
            }
            target = null;
        }
    }
}
