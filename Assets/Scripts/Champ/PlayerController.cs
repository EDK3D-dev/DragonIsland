﻿using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MOBA.Logging;

namespace MOBA
{

    public class PlayerController : MonoBehaviour
    {
        private static PlayerController instance;

        public static PlayerController Instance
        {
            set => instance = value;
            get
            {
                if (!instance)
                {
                    instance = FindObjectOfType<PlayerController>();
                    instance.Initialize();
                }
                return instance;
            }
        }

        [HideInInspector]
        public Unit hovered;

        [SerializeField]
        private ChampCamera cam;


        [SerializeField]
        private Vector3 camOffset;

        [SerializeField]
        private Vector3 camRotation;

        [Space]
        [SerializeField]
        private Champ player;

        private PhotonView playerView;

        [SerializeField]
        private Interface ui;

        public Interface UI => ui;

        public static Champ Player => Instance.player;

        [Space]
        public DefaultColors defaultColors;

        public Shader outline;

        [SerializeField]
        private ParticleSystem moveClickVfx;

        [SerializeField]
        private ParticleSystem atkMoveClickVfx;

        [Header("Settings")]

        [SerializeField, Range(0.1f, 5)]
        private float scrollSpeed = 0.4f;

        [Header("Keybinds")]

        [SerializeField]
        private KeyCode attackMove;


        private void Start()
        {
            if (Instance && Instance != this) Destroy(gameObject);
        }

        public void Initialize()
        {
            if (PhotonNetwork.IsConnected)
            {
                if (PhotonNetwork.LocalPlayer.ActorNumber % 2 == 0)
                {
                    player = PhotonNetwork.Instantiate("TestChamp", Base.InstanceRed.Spawnpoint.position, Quaternion.identity).GetComponent<Champ>();
                    playerView = PhotonView.Get(player);
                    playerView.RPC(nameof(player.SetTeamID), RpcTarget.All, (short)TeamID.red);
                }
                else
                {
                    player = PhotonNetwork.Instantiate("TestChamp", Base.InstanceBlue.Spawnpoint.position, Quaternion.identity).GetComponent<Champ>();
                    playerView = PhotonView.Get(player);
                    playerView.RPC(nameof(player.SetTeamID), RpcTarget.All, (short)TeamID.blue);
                }
            }
            cam.Initialize(player, camOffset, Quaternion.Euler(camRotation));
            ui?.Initialize(player);
        }

        public bool GetMouseWorldPos(out Vector3 mouseWorldPos)
        {
            return cam.GetCursorToGroundPoint(out mouseWorldPos);
        }


        private void OnSelectPressed()
        {
            if (!hovered)
            {
                ui.HideTargetStats();
                return;
            }
            ui.ShowTargetStats(hovered);
        }

        private void OnMovePressed()
        {
            AttackOrMove(true);
        }
        private void OnMoveHeld()
        {
            AttackOrMove();
        }

        private void AttackOrMove(bool spawnClickVFX = false)
        {
            if (!GetMouseWorldPos(out var targetPos)) return;

            if (Minimap.Instance.IsCursorOnMinimap(out var mouseWorldPos))
            {
                targetPos = mouseWorldPos;
            }
            else if (hovered)
            {
                player.OnAttackCommand(hovered.GetViewID());
                GameLogger.Log(player, LogActionType.attack, targetPos, hovered);
                return;
            }
            player.OnMoveCommand(targetPos);
            GameLogger.Log(player, LogActionType.move, targetPos);
            if (spawnClickVFX)
            {
                Instantiate(moveClickVfx, targetPos + Vector3.up * 0.2f, Quaternion.identity);
            }
        }


        private void OnAttackMovePressed()
        {
            if (!GetMouseWorldPos(out var targetPos)) return;
            if (Minimap.Instance.IsCursorOnMinimap(out var mouseWorldPos))
            {
                targetPos = mouseWorldPos;
            }
            else if (hovered)
            {
                if (hovered.Targetable)
                {
                    player.OnAttackCommand(hovered.GetViewID());
                    GameLogger.Log(player, LogActionType.attack, targetPos, hovered);
                }
                return;
            }
            Instantiate(atkMoveClickVfx, targetPos + Vector3.up * 0.2f, Quaternion.identity);
            var targets = player.GetTargetableEnemiesInRange<Unit>(targetPos, 5);
            switch (targets.Count())
            {
                case 0:
                    player.OnMoveCommand(targetPos);
                    GameLogger.Log(player, LogActionType.move, targetPos);
                    break;
                case 1:
                    var target = targets[0];
                    player.OnAttackCommand(targets[0].GetViewID());
                    GameLogger.Log(player, LogActionType.attack, targetPos, target);
                    break;
                default:
                    var closestTarget = targets.GetClosestUnitFrom<Unit>(targetPos);
                    player.OnAttackCommand(closestTarget.GetViewID());
                    GameLogger.Log(player, LogActionType.attack, targetPos, closestTarget);
                    break;
            }
        }

        private void ProcessPlayerInput()
        {
            if (player.IsDead) return;
            if (Input.GetMouseButtonDown(1))
            {
                OnMovePressed();
            }
            else if (Input.GetMouseButton(1))
            {
                OnMoveHeld();
            }

            if (Input.GetMouseButtonDown(0))
            {
                OnSelectPressed();
            }

            if (Input.GetKeyDown(attackMove))
            {
                OnAttackMovePressed();
                player.ToggleRangeIndicator(true);
            }
            else if (Input.GetKeyUp(attackMove))
            {
                player.ToggleRangeIndicator(false);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (GetMouseWorldPos(out var targetPos))
                {
                    player.CastQ(hovered, targetPos);
                    GameLogger.Log(player, LogActionType.Q, targetPos, hovered);
                }
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                if (GetMouseWorldPos(out var targetPos))
                {
                    player.CastW(hovered, targetPos);
                    GameLogger.Log(player, LogActionType.W, targetPos, hovered);
                }
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (GetMouseWorldPos(out var targetPos))
                {
                    player.CastE(hovered, targetPos);
                    GameLogger.Log(player, LogActionType.E, targetPos, hovered);
                }
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (GetMouseWorldPos(out var targetPos))
                {
                    player.CastR(hovered, targetPos);
                    GameLogger.Log(player, LogActionType.R, targetPos, hovered);
                }
            }
        }

        private void ProcessCamInput()
        {
            var scrollAxis = Input.GetAxis("Mouse ScrollWheel");
            if (scrollAxis != 0)
            {
                cam.AddDistanceFactor(-scrollAxis * scrollSpeed);
            }
        }

        private void ProcessDebugInput()
        {
            if (PhotonNetwork.IsConnected)
            {
                if (!PhotonNetwork.IsMasterClient) return;
            }
            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                if (Time.timeScale < 8)
                {
                    Time.timeScale *= 2;
                }
            }
            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                if (Time.timeScale > 0.25f)
                {
                    Time.timeScale /= 2;
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                player.Stats.DebugMode();
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                player.LevelUp();
            }
        }

        private void Update()
        {
            ProcessPlayerInput();
            ProcessCamInput();
            ProcessDebugInput();
        }

        private void OnValidate()
        {
            if (!cam) return;
            cam.transform.position = camOffset;
            cam.transform.rotation = Quaternion.Euler(camRotation);
        }
    }

}


