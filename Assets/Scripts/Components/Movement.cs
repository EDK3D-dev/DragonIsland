﻿using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class Movement : MonoBehaviour
{
    public float lastVelocity
    {
        get;
        protected set;
    }

    protected float speed;

    public Vector3 TargetPos { protected set; get; }


    public virtual void MoveTo(Vector3 destination)
    {
        TargetPos = destination;
    }

    public abstract void SetSpeed(float newSpeed);

    public abstract void Disable();

    public abstract void Enable();

    public abstract float GetVelocity();

    public Action OnBeginMoving;
    public Action OnStopMoving;


    public virtual void Initialize(float moveSpeed)
    {
        SetSpeed(moveSpeed);
    }


    public Action OnReachedDestination;

    protected virtual void Update()
    {
        if (lastVelocity <= 0)
        {
            if (GetVelocity() > 0)
            {
                OnBeginMoving?.Invoke();
            }
        }
        else if (GetVelocity() <= 0)
        {
            OnStopMoving?.Invoke();
            if (Vector3.Distance(transform.position, TargetPos) <= 1.2f)
            {
                OnReachedDestination?.Invoke();
            }
        }
        lastVelocity = GetVelocity();
    }


    public abstract void DisableCollision();

    public abstract void EnableCollision();

    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, TargetPos);
    }
}
