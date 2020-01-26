﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    public class DamageOverTime : Buff
    {
        private float baseDamage;

        private Scalings scaling;

        private DamageType dmgType;

        private Unit target;

        public const string NAMESUFFIX = " (Damage)";

        public void Initialize(BuffProperties _properties, UnitStats ownerStats, Unit _target)
        {
            base.Initialize(_properties, ownerStats);
            BuffName += NAMESUFFIX;
            target = _target;
        }

        protected override void OnActivated()
        {
            OnTick();
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnTick()
        {
            var damage = new Damage(baseDamage + scaling.GetScalingDamageBonusOnTarget(ownerStatsAtApply, target), dmgType, Instigator, target);
        }
    }
}
