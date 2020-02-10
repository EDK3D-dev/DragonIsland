﻿using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    public enum DamageType : short
    {
        invalid = -1,
        physical = 0,
        magical = 1,
        piercing = 2,
    }

    public class Damage
    {
        private float baseDmg;

        private DamageType dmgType = DamageType.invalid;

        private Unit instigator;

        private List<Unit> targets;

        private int GetDamageTo(Unit target)
        {
            float dmg = baseDmg * instigator.amplifiers.dealtDmg;
            float defense = 0;
            switch (dmgType)
            {
                case DamageType.physical:
                    defense = target.Stats.Armor * instigator.Stats.percentArmorPen - instigator.Stats.flatArmorPen;
                    break;
                case DamageType.magical:
                    defense = target.Stats.MagicRes * instigator.Stats.percentMagicPen - instigator.Stats.flatMagicPen;
                    break;
                case DamageType.piercing:
                    defense = 0;
                    break;
                default:
                    Debug.LogWarning("Warning: found Damage with invalid type!");
                    break;
            }
            if (defense < 0)
            {
                return (int)(dmg * (2 - 100 / (100 - defense)));
            }
            return (int)(dmg * (100 / 100 + defense));
        }

        public void Inflict()
        {
            foreach (var target in targets)
            {
                InflictTo(target);
            }
        }

        private void InflictTo(Unit target)
        {
            target.GetComponent<PhotonView>().RPC(nameof(target.ReceiveDamage), RpcTarget.All, 
                instigator.GetViewID(), 
                GetDamageTo(target), 
                (short)dmgType
            );
        }

        public Damage(float _baseDmg, DamageType _dmgType, Unit _instigator, Unit _target)
        {
            baseDmg = _baseDmg;
            dmgType = _dmgType;
            instigator = _instigator;
            targets = new List<Unit> { _target };
        }

    }
}
