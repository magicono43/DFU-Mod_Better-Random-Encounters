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
            go.AddComponent<BRELootPileObject>();
        }

        void Awake()
        {
            InitMod();

            mod.IsReady = true;
        }

        private static void InitMod()
        {
            Debug.Log("Begin mod init: BetterRandomEncounters");

            EnemyDeath.OnEnemyDeath += AddBREObject_OnEnemyDeath;

            Debug.Log("Finished mod init: BetterRandomEncounters");
        }

        void Start()
        {
            RegisterJACommands();
            mainCamera = GameManager.Instance.MainCamera;
            playerLayerMask = ~(1 << LayerMask.NameToLayer("Player"));
        }

        public static void AddBREObject_OnEnemyDeath(object sender, EventArgs e) // Adds BRELootPileObject component upon enemy death, if applicable.
        {
            EnemyDeath enemyDeath = sender as EnemyDeath;
            if (enemyDeath != null)
            {
                BRECustomObject bRECustomObject = enemyDeath.GetComponent<BRECustomObject>();
                if (bRECustomObject != null && bRECustomObject.IsGangLeader)
                {
                    DaggerfallEntityBehaviour entityBehaviour = enemyDeath.GetComponent<DaggerfallEntityBehaviour>();
                    if (entityBehaviour != null)
                    {
                        EnemyEntity enemyEntity = entityBehaviour.Entity as EnemyEntity;
                        if (enemyEntity != null)
                        {
                            switch (bRECustomObject.EventName)
                            {
                                case "Traveling_Alchemist_Solo": BREWork.Traveling_Alchemist_CorpseBagTraps(entityBehaviour.CorpseLootContainer, enemyEntity, bRECustomObject); break;
                                default: break;
                            }
                        }
                    }
                }
            }
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
                    // Check for lootable object hit
                    DaggerfallLoot loot;
                    if (LootCheck(hit, out loot))
                    {
                        BRELootPileObject bRELootPileObject;
                        if (BRELootPileCheck(loot, out bRELootPileObject))
                            ExecuteBRELootPile(hit, loot, bRELootPileObject);
                    }

                    // Avoid non-action interactions while a Touch cast is readied
                    if (!touchCastPending)
                    {
                        // Check for mobile enemy hit
                        DaggerfallEntityBehaviour mobileEnemyBehaviour;
                        if (MobileEnemyCheck(hit, out mobileEnemyBehaviour))
                        {
                            BRECustomObject bRECustomObject;
                            if (BRECustObjCheck(mobileEnemyBehaviour, out bRECustomObject))
                                ExecuteBRECustObj(mobileEnemyBehaviour, bRECustomObject);
                        }
                    }
                }
            }
        }

        // Check if raycast hit a lootable object
        private bool LootCheck(RaycastHit hitInfo, out DaggerfallLoot loot)
        {
            loot = hitInfo.transform.GetComponent<DaggerfallLoot>();

            return loot != null;
        }

        // Check if corpse loot pile has BRELootPileObject attached to it
        public static bool BRELootPileCheck(DaggerfallLoot loot, out BRELootPileObject lootPileObj)
        {
            lootPileObj = loot.GetComponent<BRELootPileObject>();

            return lootPileObj != null;
        }

        public static void ExecuteBRELootPile(RaycastHit hit, DaggerfallLoot loot, BRELootPileObject lootPileObj)
        {
            // Check if close enough to activate for all types, besides corpses.
            if (loot.ContainerType != LootContainerTypes.CorpseMarker && hit.distance > PlayerActivate.TreasureActivationDistance)
                return;

            switch (loot.ContainerType)
            {
                // Check if close enough to activate and that corpse has items
                case LootContainerTypes.CorpseMarker:
                    if (hit.distance > PlayerActivate.CorpseActivationDistance) return;
                    else if (loot.Items.Count == 0) return;
                    else if (loot.Items.Count == 1 && loot.Items.Contains(ItemGroups.Weapons, (int)Weapons.Arrow)) return;
                    break;
                default: return;
            }

            BRELootAction(lootPileObj);

            // Open inventory window with activated loot container as remote target (if we fall through to here)
            DaggerfallUI.Instance.InventoryWindow.LootTarget = loot;
            DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenInventoryWindow);
        }

        public static void BRELootAction(BRELootPileObject lootPileObj)
        {
            if (lootPileObj == null)
                return;

            if (!lootPileObj.OpenTextShown && lootPileObj.HasFirstOpenText)
            {
                BREWork.PopRegularText(lootPileObj.FirstOpenText);
                lootPileObj.OpenTextShown = true;
                return;
            }

            if (lootPileObj.HasChoices)
            {
                BREWork.PopTextWithChoice(lootPileObj.ChoiceText, lootPileObj.EventName, true);
                return;
            }

            if (lootPileObj.HasMoreOpenText)
            {
                BREWork.PopRegularText(lootPileObj.MoreOpenText);
                lootPileObj.HasMoreOpenText = false;
                return;
            }
        }

        // Check if raycast hit a mobile enemy
        public static bool MobileEnemyCheck(RaycastHit hitInfo, out DaggerfallEntityBehaviour mobileEnemy)
        {
            mobileEnemy = hitInfo.transform.GetComponent<DaggerfallEntityBehaviour>();

            return mobileEnemy != null;
        }

        // Check if mobile enemy has BRECustomObject attached to it
        public static bool BRECustObjCheck(DaggerfallEntityBehaviour mobileEnemy, out BRECustomObject custObject)
        {
            custObject = mobileEnemy.GetComponent<BRECustomObject>();

            return custObject != null;
        }

        public static void ExecuteBRECustObj(DaggerfallEntityBehaviour mobileEnemyBehaviour, BRECustomObject custObject)
        {
            EnemyEntity enemyEntity = mobileEnemyBehaviour.Entity as EnemyEntity;
            switch (GameManager.Instance.PlayerActivate.CurrentMode)
            {
                case PlayerActivateModes.Info:
                    break;
                case PlayerActivateModes.Grab:
                case PlayerActivateModes.Talk:
                    BRETalk(custObject);
                    break;
                case PlayerActivateModes.Steal:
                    break;
            }
        }

        public static void BRETalk(BRECustomObject custObject)
        {
            if (custObject == null)
                return;

            if (!custObject.GreetingShown && custObject.HasGreeting)
            {
                BREWork.PopRegularText(custObject.GreetingText);
                custObject.GreetingShown = true;
                return;
            }

            if (custObject.HasMoreText)
            {
                if (custObject.HasAddChoiceBox)
                {
                    BREWork.PopTextWithChoice(custObject.AdditionalText, custObject.EventName);
                    return;
                }

                BREWork.PopRegularText(custObject.AdditionalText);
                custObject.HasMoreText = false;
                return;
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
