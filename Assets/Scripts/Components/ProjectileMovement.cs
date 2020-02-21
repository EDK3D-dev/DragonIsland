﻿using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    public class ProjectileMovement : Movement
    {
        protected bool stopped = false;

        protected Vector3 lastPos;

        public override void Enable()
        {
            stopped = false;
        }

        public override float GetVelocity()
        {
            return Vector3.Distance(transform.position, lastPos) / Time.deltaTime;
        }

        public override void MoveTo(Vector3 destination)
        {
            if (stopped) return;
            base.MoveTo(destination);
            transform.LookAt(destination);
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        public override void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public override void Disable()
        {
            stopped = true;
        }

        protected override void Update()
        {
            if (Vector3.Distance(lastPos, transform.position) < 0.1f * Time.deltaTime)
            {
                if (GetComponent<Projectile>()?.waitForDestroyRPC == true)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    PhotonView.Get(this)?.RPC("DestroyRPC", RpcTarget.Others);
                    Destroy(gameObject);
                }
            }
            lastPos = transform.position;
        }

        public override void DisableCollision()
        {
            foreach (var collider in GetComponents<Collider>())
            {
                collider.enabled = false;
            }
        }

        public override void EnableCollision()
        {
            foreach (var collider in GetComponents<Collider>())
            {
                collider.enabled = true;
            }
        }

        public override void Stop()
        {
            stopped = true;
        }
    }
}
