// Project:         BetterRandomEncounters mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2022 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    1/22/2022, 8:45 PM
// Last Edit:		1/22/2022, 8:45 PM
// Version:			1.00
// Special Thanks:  Hazelnut, Ralzar
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
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallConnect.Arena2;
using BetterRandomEncounters;

namespace BetterRandomEncounters
{
    public class BREWork : MonoBehaviour
    {
        private bool gameStarted = false;
        public int latestEncounterIndex = 0;
        private uint lastGameMinutes = 0;         // Being tracked in order to perform updates based on changes in the current game minute
        PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
        GameObject player = GameManager.Instance.PlayerObject;
        GameObject[] mobile;

        private void Update()
        {
            if (!gameStarted && !GameManager.Instance.StateManager.GameInProgress)
                return;
            else if (!gameStarted)
                gameStarted = true;

            if (SaveLoadManager.Instance.LoadInProgress)
                return;

            if (playerEntity.CurrentHealth <= 0)
                return;

            uint gameMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
            if (gameMinutes < lastGameMinutes)
            {
                throw new Exception(string.Format("lastGameMinutes {0} greater than gameMinutes: {1}", lastGameMinutes, gameMinutes));
            }

            if (!playerEntity.PreventEnemySpawns)
            {
                //Debug.Log((gameMinutes - lastGameMinutes).ToString());
                for (uint l = 0; l < (gameMinutes - lastGameMinutes); ++l)
                {
                    // Catch up time and break if something spawns. Don't spawn encounters while player is on ship.
                    if (!GameManager.Instance.TransportManager.IsOnShip() &&
                        ModdedEnemySpawn(l + lastGameMinutes + 1))
                        break;

                    // Confirm regionData is available
                    if (playerEntity.RegionData == null || playerEntity.RegionData.Length == 0)
                        break;
                }
            }

            lastGameMinutes = gameMinutes;

            // Allow enemy spawns again if they have been disabled
            if (playerEntity.PreventEnemySpawns)
                playerEntity.PreventEnemySpawns = false;
        }

        public bool ModdedEnemySpawn(uint Minutes)
        {
            // Define minimum distance from player based on spawn locations
            const int minDungeonDistance = 8;
            const int minLocationDistance = 10;
            const int minWildernessDistance = 10;

            // Do not allow spawns if not enough time has passed
            bool timeForSpawn = (Minutes % 90) == 0; // Will have to test around with this, kind of strange to me at least how it might actually work in practice.
            if (!timeForSpawn)
                return false;

            // Spawns when player is outside
            if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                uint timeOfDay = Minutes % 1440; // 1440 minutes in a day
                if (GameManager.Instance.PlayerGPS.IsPlayerInLocationRect)
                {
                    if (timeOfDay >= 360 && timeOfDay <= 1080)
                    {
                        // In a location area during day

                        // roll for chances of something happening, possibly also modified by the type of location you are in?
                        // if roll successful than choose the type of encounter that will occur.
                        // if it is some sort of enemy encounter than probably pop some dialogue about it then use "CreateFoeSpawner" in some way.
                        // Etc.

                        //if (UnityEngine.Random.Range(0, 11) == 0) // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        if (1 == 1)
                        {
                            RollGoodLocationDayEncounter();
                        }
                        else if (UnityEngine.Random.Range(0, 26) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {
                            RollBadLocationDayEncounter();
                        }
                    }
                    else
                    {
                        // In a location area at night

                        if (UnityEngine.Random.Range(0, 26) == 0) // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        {
                            RollGoodLocationNightEncounter();
                        }
                        else if (UnityEngine.Random.Range(0, 11) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {
                            RollBadLocationNightEncounter();
                        }
                    }
                }
                else
                {
                    if (timeOfDay >= 360 && timeOfDay <= 1080)
                    {
                        // Wilderness during day

                        if (UnityEngine.Random.Range(0, 11) == 0) // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        {
                            RollGoodWildernessDayEncounter();
                        }
                        else if (UnityEngine.Random.Range(0, 26) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {
                            RollBadWildernessDayEncounter();
                        }
                    }
                    else
                    {
                        // Wilderness at night

                        if (UnityEngine.Random.Range(0, 26) == 0) // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        {
                            RollGoodWildernessNightEncounter();
                        }
                        else if (UnityEngine.Random.Range(0, 11) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {
                            RollBadWildernessNightEncounter();
                        }
                    }
                }
            }

            // Spawns when player is inside
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                // Spawns when player is inside a dungeon
                if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
                {
                    if (playerEntity.IsResting)
                    {

                    }
                }
            }

            return false;
        }

        #region Work Methods

        public void RollGoodLocationDayEncounter()
        {
            //latestEncounterIndex = PickOneOf(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            latestEncounterIndex = PickOneOf(1);
            TextFile.Token[] tokens = null;

            switch (latestEncounterIndex)
            {
                case 1:
                    DaggerfallMessageBox initialEventPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
                    ModManager.Instance.SendModMessage("TravelOptions", "pauseTravel");
                    tokens = BREText.PilgrimageText(1);
                    PopRegularText(tokens); // Later on in development, likely make many of these "safe" encounters skippable with a yes/no prompt.
                    // Figure out if I might be able to somehow make these "foe" objects clickable like in a quest to say something or do something, or if I will have to do that in the quest system.

                    //GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(Vector3.zero, MobileTypes.Healer, 1, MobileReactions.Passive);
                    GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, MobileTypes.Healer, 1, MobileReactions.Passive);
                    mobile[0].AddComponent<BRECustomObject>();
                    BRECustomObject bRECustomObject = mobile[0].GetComponent<BRECustomObject>();
                    bRECustomObject.GreetingText = BREText.PilgrimageText(2); // Place-holder text token
                    bRECustomObject.AdditionalText = BREText.PilgrimageText(3); // Place-holder text token
                    bRECustomObject.HasGreeting = true;
                    bRECustomObject.HasMoreText = true;
                    mobile[0].transform.LookAt(mobile[0].transform.position + (mobile[0].transform.position - player.transform.position));
                    mobile[0].SetActive(true);
                    //GameObject spawner = GameObjectHelper.CreateFoeSpawner(false, MobileTypes.Healer, 1, minDistance: 10, maxDistance: 25, null, true); // May need to change if I just want "passive"
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    break;
                case 8:
                    break;
                case 9:
                    break;
                case 10:
                    break;
                default:
                    break;
            }
        }

        public void RollBadLocationDayEncounter()
        {

        }

        public void RollGoodLocationNightEncounter()
        {

        }

        public void RollBadLocationNightEncounter()
        {

        }

        public void RollGoodWildernessDayEncounter()
        {

        }

        public void RollBadWildernessDayEncounter()
        {

        }

        public void RollGoodWildernessNightEncounter()
        {

        }

        public void RollBadWildernessNightEncounter()
        {

        }

        public static int PickOneOf(params int[] values) // Pango provided assistance in making this much cleaner way of doing the random value choice part, awesome.
        {
            return values[UnityEngine.Random.Range(0, values.Length)];
        }

        public static void PopRegularText(TextFile.Token[] tokens)
        {
            DaggerfallMessageBox textBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            textBox.SetTextTokens(tokens);
            textBox.ClickAnywhereToClose = true;
            textBox.Show();
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
                    // Do the stuff that I want BRECustomObject to do with my mod after the enemy has been created and had this object attached to them, in most cases have them say something, etc.
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
                PopRegularText(custObject.GreetingText);
                custObject.GreetingShown = true;
                return;
            }

            if (custObject.HasMoreText)
            {
                PopRegularText(custObject.AdditionalText);
                custObject.HasMoreText = false;
                return;
            }
        }

        #endregion
    }
}
