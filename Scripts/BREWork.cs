// Project:         BetterRandomEncounters mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2022 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    1/22/2022, 8:45 PM
// Last Edit:		1/22/2022, 8:45 PM
// Version:			1.00
// Special Thanks:  Hazelnut, Ralzar, Badluckburt, Kab the Bird Ranger, JohnDoom
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

            if (GameManager.IsGamePaused)
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
                            if (!RollGoodLocationDayEncounter()) { return false; }
                            else { return true; }
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

                        //if (UnityEngine.Random.Range(0, 11) == 0) // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        if (1 == 1)
                        {
                            if (!RollGoodWildernessDayEncounter()) { return false; }
                            else { return true; }
                        }
                        //else if (1 == 1)
                        else if (UnityEngine.Random.Range(0, 26) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {
                            if (!RollBadWildernessDayEncounter()) { return false; }
                            else { return true; }
                        }
                    }
                    else
                    {
                        // Wilderness at night

                        //if (UnityEngine.Random.Range(0, 26) == 0) // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        if (1 == 1)
                        {
                            if (!RollGoodWildernessNightEncounter()) { return false; }
                            else { return true; }
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

        public bool RollGoodLocationDayEncounter()
        {
            //latestEncounterIndex = PickOneOf(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            latestEncounterIndex = PickOneOf(1);

            switch (latestEncounterIndex)
            {
                case 1:
                    return false;
                case 2:
                    return false;
                case 3:
                    return false;
                case 4:
                    return false;
                case 5:
                    return false;
                case 6:
                    return false;
                case 7:
                    return false;
                case 8:
                    return false;
                case 9:
                    return false;
                case 10:
                    return false;
                default:
                    return false;
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

        public bool RollGoodWildernessDayEncounter()
        {
            //latestEncounterIndex = PickOneOf(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            latestEncounterIndex = PickOneOf(1, 2, 3, 4);
            MobileTypes soloMob = MobileTypes.Rat;
            GameObject mobile = null;

            switch (latestEncounterIndex)
            {
                case 1: // Encounter a lone random class during the daytime that is initially passive to the player, all doing their own different things depending on the class, etc.
                    if (GameManager.Instance.PlayerEntity.IsResting) { return false; } // Basically thinking, don't interrupt resting for most "passive" encounters like this one.
                    soloMob = (MobileTypes)PickOneOf(132, 140, 134, 137, 142, 145);
                    SimpleEncounterTextInitiate("Lone_" + soloMob.ToString());
                    mobile = CreateSingleEnemy("Lone_" + soloMob.ToString(), soloMob, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 2: // Encounter an alchemist that may allow you to buy a selection of potions they have made/have on them at the moment, maybe change variety depending on their class.
                    if (GameManager.Instance.PlayerEntity.IsResting) { return false; }
                    soloMob = (MobileTypes)PickOneOf(128, 131, 132, 133); // Need to make more logic for this encounter, such as creating potions and then being able to buy them, etc.
                    SimpleEncounterTextInitiate("Lone_Alchemist");
                    mobile = CreateSingleEnemy("Lone_Alchemist", soloMob, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 3: // Encounter a lone random class that appears to be doing physical training, most likely in preperation to join the local/regional army/military/guard whatever.
                    if (GameManager.Instance.PlayerEntity.IsResting) { return false; }
                    soloMob = (MobileTypes)PickOneOf(130, 141, 144, 145);
                    SimpleEncounterTextInitiate("Lone_Trainee");
                    mobile = CreateSingleEnemy("Lone_Trainee", soloMob, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 4: // Encounter a lone random mostly magic using class that appears to be casting various spells, likely training/trying to hone their magical abilities.
                    if (GameManager.Instance.PlayerEntity.IsResting) { return false; }
                    soloMob = (MobileTypes)PickOneOf(128, 129, 130, 131); // Need more logic to have casting sounding/visual particles eminating from the NPC, to seem like they are casting spells.
                    SimpleEncounterTextInitiate("Lone_Training_Mage");
                    mobile = CreateSingleEnemy("Lone_Training_Mage", soloMob, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 5:
                    return false;
                case 6:
                    return false;
                case 7:
                    return false;
                case 8:
                    return false;
                case 9:
                    return false;
                case 10:
                    return false;
                default:
                    return false;
            }
        }

        public bool RollBadWildernessDayEncounter()
        {
            //latestEncounterIndex = PickOneOf(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            latestEncounterIndex = PickOneOf(1, 2, 3, 4);
            MobileTypes soloMob = MobileTypes.Rat;
            GameObject mobile = null;

            switch (latestEncounterIndex)
            {
                case 1: // Encounter some lone outdoor animal/creature that is hopefully appropriate to the climate of the surrounding area the player is currently in.
                    soloMob = (MobileTypes)PickOneOf(0, 3, 4, 5, 6, 20); // Possibly take out small vermine like rats and bats and keep those for "multi-spawn" encounters, maybe.
                    SimpleEncounterTextInitiate("Lone_Beast");
                    mobile = CreateSingleEnemy("", soloMob);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 2: // Encounter a creature of nature that has been disturbed by your proximity/intrusion of it's territory/grove whatever.
                    soloMob = (MobileTypes)PickOneOf(2, 10); // Just Spriggan and Nymph for now I guess.
                    SimpleEncounterTextInitiate("Lone_Nature_Guard");
                    mobile = CreateSingleEnemy("", soloMob);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 3: // Encounter a lone humanoid monster type of enemy that would make sense for the outdoor environment to be seen more in the daylight most likely.
                    soloMob = (MobileTypes)PickOneOf(7, 8, 13, 16);
                    SimpleEncounterTextInitiate("Lone_Humanoid_Monster");
                    mobile = CreateSingleEnemy("", soloMob);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 4: // Encounter a lone atronach, likely the creation of an irresponsible or possibly even malicious wizard or mage set loose onto the world.
                    soloMob = (MobileTypes)PickOneOf(35, 36, 37, 38);
                    SimpleEncounterTextInitiate("Lone_Atronach");
                    mobile = CreateSingleEnemy("", soloMob);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 5:
                    return false;
                case 6:
                    return false;
                default:
                    return false;
            }
        }

        public bool RollGoodWildernessNightEncounter()
        {
            //latestEncounterIndex = PickOneOf(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            latestEncounterIndex = PickOneOf(1, 2, 3, 4);
            MobileTypes soloMob = MobileTypes.Rat;
            GameObject mobile = null;

            switch (latestEncounterIndex)
            {
                case 1: // Encounter a lone random class during the night that is initially passive to the player but might become hostile if pushed, all doing their own different things.
                    if (GameManager.Instance.PlayerEntity.IsResting) { return false; }
                    soloMob = (MobileTypes)PickOneOf(133, 135, 136, 138, 139); // Needs more logic for the times where the class will become hostile if talked too multiple times or something else.
                    SimpleEncounterTextInitiate("Lone_" + soloMob.ToString());
                    mobile = CreateSingleEnemy("Lone_" + soloMob.ToString(), soloMob, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 2: // Encounter a lone non-hostile vampire just doing their traveling under the protection of the night, can talk to them.
                    if (GameManager.Instance.PlayerEntity.IsResting) { return false; }
                    soloMob = (MobileTypes)PickOneOf(28, 30); // Need to make more logic for this encounter, such as creating potions and then being able to buy them, etc.
                    SimpleEncounterTextInitiate("Lone_Nice_Vampire");
                    mobile = CreateSingleEnemy("Lone_Nice_Vampire", soloMob, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                default:
                    return false;
            }
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

        public GameObject CreateSingleEnemy(string eventName, MobileTypes enemyType = MobileTypes.Acrobat, MobileReactions enemyReact = MobileReactions.Hostile, bool hasGreet = false, bool hasAdd = false, bool hasAggro = false)
        {
            GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, enemyType, 1, enemyReact);
            mobile[0].AddComponent<BRECustomObject>();
            BRECustomObject bRECustomObject = mobile[0].GetComponent<BRECustomObject>();

            if (eventName != "") // Basically for spawning single non-verbal enemies sort of things, even if the event does have a proper name for the initiation part.
            {
                if (hasGreet) { bRECustomObject.GreetingText = BREText.EncounterTextFinder(eventName, "Greet"); bRECustomObject.HasGreeting = true; }
                if (hasAdd) { bRECustomObject.AdditionalText = BREText.EncounterTextFinder(eventName, "Add"); bRECustomObject.HasMoreText = true; }
                if (hasAggro) { bRECustomObject.AggroText = BREText.EncounterTextFinder(eventName, "Aggro"); bRECustomObject.HasAggroText = true; }
            }
            
            return mobile[0];
        }

        public void SimpleEncounterTextInitiate(string eventName)
        {
            TextFile.Token[] tokens = null;
            DaggerfallMessageBox initialEventPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);

            ModManager.Instance.SendModMessage("TravelOptions", "pauseTravel");
            tokens = BREText.EncounterTextFinder(eventName);

            PopRegularText(tokens); // Later on in development, likely make many of these "safe" encounters skippable with a yes/no prompt.
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
