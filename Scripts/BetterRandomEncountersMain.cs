// Project:         BetterRandomEncounters mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2022 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    1/22/2022, 8:45 PM
// Last Edit:		1/22/2022, 8:45 PM
// Version:			1.00
// Special Thanks:  Hazelnut, Ralzar, Badluckburt, Kab the Bird Ranger, JohnDoom, Uncanny Valley
// Modifier:			

using System;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Utility;
using Wenzil.Console;
using System.Collections.Generic;
using DaggerfallConnect.FallExe;

namespace BetterRandomEncounters
{
    public class BetterRandomEncountersMain : MonoBehaviour
    {
        static Mod mod;

        Camera mainCamera;
        int playerLayerMask = 0;
        bool castPending = false;
        [NonSerialized] DaggerfallEntityBehaviour enemyEntityBehaviour = null;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<BetterRandomEncountersMain>();


            go.AddComponent<BREWork>();
            go.AddComponent<BRECustomObject>();
        }

        void Awake()
        {
            InitMod();

            mod.IsReady = true;
        }

        private static void InitMod()
        {
            Debug.Log("Begin mod init: BetterRandomEncounters");

            EnemyDeath.OnEnemyDeath += BetterRandomEncountersLoot_OnEnemyDeath;

            Debug.Log("Finished mod init: BetterRandomEncounters");
        }

        void Start()
        {
            RegisterJACommands();
            mainCamera = GameManager.Instance.MainCamera;
            playerLayerMask = ~(1 << LayerMask.NameToLayer("Player"));
        }

        private void Update()
        {
            if (mainCamera == null)
                return;

            if (GameManager.IsGamePaused)
                return;

            // Do nothing further if player has spell ready to cast as activate button is now used to fire spell
            // The exception is a readied touch spell where player can activate doors, etc.
            // Touch spells only fire once a target entity is in range
            bool touchCastPending = false;
            if (GameManager.Instance.PlayerEffectManager)
            {
                // Handle pending spell cast
                if (GameManager.Instance.PlayerEffectManager.HasReadySpell)
                {
                    // Exclude touch spells from this check
                    DaggerfallWorkshop.Game.MagicAndEffects.EntityEffectBundle spell = GameManager.Instance.PlayerEffectManager.ReadySpell;
                    if (spell.Settings.TargetType != DaggerfallWorkshop.Game.MagicAndEffects.TargetTypes.ByTouch)
                    {
                        castPending = true;
                        return;
                    }
                    else
                    {
                        touchCastPending = true;
                    }
                }

                // Prevents last spell cast click from falling through to normal click handling this frame
                if (castPending)
                {
                    castPending = false;
                    return;
                }
            }

            if (InputManager.Instance.ActionComplete(InputManager.Actions.ActivateCenterObject))
            {
                float ActivationDistance = 128 * MeshReader.GlobalScale;

                // Fire ray into scene from active mouse cursor or camera
                Ray ray = new Ray();

                // Ray from camera crosshair position
                ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

                // Test ray against scene
                RaycastHit hit;
                bool hitSomething = Physics.Raycast(ray, out hit, ActivationDistance, playerLayerMask);
                if (hitSomething)
                {
                    // Avoid non-action interactions while a Touch cast is readied
                    if (!touchCastPending)
                    {
                        // Check for mobile enemy hit
                        DaggerfallEntityBehaviour mobileEnemyBehaviour;
                        if (BREWork.MobileEnemyCheck(hit, out mobileEnemyBehaviour))
                        {
                            BRECustomObject bRECustomObject;
                            if (BREWork.BRECustObjCheck(mobileEnemyBehaviour, out bRECustomObject))
                                BREWork.ExecuteBRECustObj(mobileEnemyBehaviour, bRECustomObject);
                        }
                    }
                }
            }
        }

        public static void BetterRandomEncountersLoot_OnEnemyDeath(object sender, EventArgs e) // Populates enemy loot upon their death.
        {
            EnemyDeath enemyDeath = sender as EnemyDeath;
            if (enemyDeath != null)
            {
                DaggerfallEntityBehaviour entityBehaviour = enemyDeath.GetComponent<DaggerfallEntityBehaviour>();
                if (entityBehaviour != null)
                {
                    EnemyEntity enemyEntity = entityBehaviour.Entity as EnemyEntity;
                    if (enemyEntity != null)
                    {
						
                    }
                }
            }
        }

        public static void RegisterJACommands()
        {
            Debug.Log("[BetterRandomEncounters] Trying to register console commands.");
            try
            {
                ConsoleCommandsDatabase.RegisterCommand(AddBREItems.command, AddBREItems.description, AddBREItems.usage, AddBREItems.Execute);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Error Registering BetterRandomEncounters Console commands: {0}", e.Message));
            }
        }

        private static class AddBREItems
        {
            public static readonly string command = "ja_jewelry";
            public static readonly string description = "Adds one of each Jewelry Additions Items to player's inventory (may need to run multiple times to get specific variant.)";
            public static readonly string usage = "ja_jewelry";

            public static string Execute(params string[] args)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                PlayerEntity playerEntity = player.GetComponent<DaggerfallEntityBehaviour>().Entity as PlayerEntity;
                ItemCollection items = playerEntity.Items;
                DaggerfallUnityItem newItem = null;
                int[] allowedJewelry = { 4700, 4701, 4702, 4703, 4704, 4705, 4706, 4707, 0, 1, 2, 3, 4708, 4709, 4710, 4711, 4712, 4713, 4714 };

                for (int i = 0; i < allowedJewelry.Length; i++)
                {
                    if (allowedJewelry[i] >= 4700 && allowedJewelry[i] < 4708)
                    {
                        newItem = ItemBuilder.CreateItem(ItemGroups.Jewellery, allowedJewelry[i]);
                        items.AddItem(newItem);
                    }
                    else
                    {
                        newItem = ItemBuilder.CreateItem(ItemGroups.Gems, allowedJewelry[i]);
                        items.AddItem(newItem);
                    }
                }

                return "BRE Items added";
            }
        }
    }
}
