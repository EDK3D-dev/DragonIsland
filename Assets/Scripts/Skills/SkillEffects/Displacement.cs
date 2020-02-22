﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    public class Displacement : SkillEffect
    {
        [Space]
        [SerializeField, Range(0.1f, 10)]
        private float duration = 1;

        [SerializeField, Tooltip("Time and value should range from 0 to 1")]
        private AnimationCurve distancePerTime = AnimationCurve.Linear(0, 0, 1, 1);

        [Space]
        [SerializeField, Range(0, 5)]
        private float maxHeight;


        [SerializeField, Tooltip("Time and value should range from 0 to 1")]
        private AnimationCurve heightPerTime = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));

        public override void Activate(Unit _target)
        {
            base.Activate(_target);
        }

        public override void Activate(Vector3 targetPos)
        {
            Debug.LogError("Cannot displace a position! (Source: " + owner.name + ")");
        }

        public override void Activate<T>(UnitList<T> targets)
        {
            foreach (var target in targets)
            {
                Activate(target);
            }
        }

        public override void Tick()
        {
            Debug.LogWarning("Displacements shouldn't tick, repeatedly activate them instead! (Source: " + owner.name + ")");
        }

        protected override void OnDeactivated()
        {
        }
    }
}
