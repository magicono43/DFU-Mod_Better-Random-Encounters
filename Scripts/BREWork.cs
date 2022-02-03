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
using System.Linq;

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

            DFRegion regionData = GameManager.Instance.PlayerGPS.CurrentRegion;

            // Spawns when player is outside
            if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                uint timeOfDay = Minutes % 1440; // 1440 minutes in a day
                if (GameManager.Instance.PlayerGPS.IsPlayerInLocationRect)
                {
                    DFLocation locationData = GameManager.Instance.PlayerGPS.CurrentLocation;
                    DFRegion.DungeonTypes dungeonType = DFRegion.DungeonTypes.NoDungeon;
                    if (locationData.HasDungeon)
                        dungeonType = regionData.MapTable[locationData.LocationIndex].DungeonType; // Will have to test this later, but should work in giving current location dungeon type, I think.

                    if (timeOfDay >= 360 && timeOfDay <= 1080)
                    {
                        // In a location area during day

                        // roll for chances of something happening, possibly also modified by the type of location you are in?
                        // if roll successful than choose the type of encounter that will occur.
                        // if it is some sort of enemy encounter than probably pop some dialogue about it then use "CreateFoeSpawner" in some way.
                        // Etc.

                        if (locationData.HasDungeon && (dungeonType != DFRegion.DungeonTypes.NoDungeon || regionData.MapTable[locationData.LocationIndex].LocationType != DFRegion.LocationTypes.TownCity))
                        {
                            RollGoodDayDungeonExteriorEncounters(dungeonType);

                            RollBadDayDungeonExteriorEncounters(dungeonType);
                        }

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

                        if (locationData.HasDungeon && (dungeonType != DFRegion.DungeonTypes.NoDungeon || regionData.MapTable[locationData.LocationIndex].LocationType != DFRegion.LocationTypes.TownCity))
                        {
                            RollGoodNightDungeonExteriorEncounters(dungeonType);

                            RollBadNightDungeonExteriorEncounters(dungeonType);
                        }

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
                        //else if (1 == 1)
                        else if (UnityEngine.Random.Range(0, 11) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {
                            if (!RollBadWildernessNightEncounter()) { return false; }
                            else { return true; }
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

        public bool RollGoodDayDungeonExteriorEncounters(DFRegion.DungeonTypes dungeonType)
        {
            ChooseDungeonExteriorEncounter(dungeonType, 0);

            return false;
        }

        public bool RollBadDayDungeonExteriorEncounters(DFRegion.DungeonTypes dungeonType)
        {
            ChooseDungeonExteriorEncounter(dungeonType, 1);

            return false;
        }

        public bool RollGoodNightDungeonExteriorEncounters(DFRegion.DungeonTypes dungeonType)
        {
            ChooseDungeonExteriorEncounter(dungeonType, 2);

            return false;
        }

        public bool RollBadNightDungeonExteriorEncounters(DFRegion.DungeonTypes dungeonType)
        {
            ChooseDungeonExteriorEncounter(dungeonType, 3);

            return false;
        }

        public void ChooseDungeonExteriorEncounter (DFRegion.DungeonTypes dungeonType, int eventType)
        {
            int mainEnemy = -1;

            switch (dungeonType)
            {
                case DFRegion.DungeonTypes.Crypt: mainEnemy = PickOneOf(0, 3, 15, 17, 18, 19, 23, 28, 30, 32, 33, 135, 136, 138, 139); break;
                case DFRegion.DungeonTypes.OrcStronghold: mainEnemy = PickOneOf(0, 7, 12, 21, 24); break;
                case DFRegion.DungeonTypes.HumanStronghold: mainEnemy = PickOneOf(0, 128, 129, 130, 131, 132, 133, 134, 140, 141, 144, 145); break;
                case DFRegion.DungeonTypes.Prison: mainEnemy = PickOneOf(0, 3, 7, 135, 136, 137, 138, 139, 143); break;
                case DFRegion.DungeonTypes.DesecratedTemple: mainEnemy = PickOneOf(0, 1, 3, 18, 22, 23, 25, 26, 27, 29, 31, 132, 140); break;
                case DFRegion.DungeonTypes.Mine: mainEnemy = PickOneOf(0, 3, 4, 5, 6, 7, 16, 20, 28, 36, 135, 136, 138); break;
                case DFRegion.DungeonTypes.NaturalCave: mainEnemy = PickOneOf(0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13, 14, 16, 20, 21, 28, 32, 34, 133, 135, 136, 138, 142, 143); break;
                case DFRegion.DungeonTypes.Coven: mainEnemy = PickOneOf(0, 1, 3, 10, 13, 15, 17, 18, 19, 22, 23, 25, 26, 27, 29, 31, 32, 33, 35, 36, 37, 38, 128, 129, 130, 131, 133); break;
                case DFRegion.DungeonTypes.VampireHaunt: mainEnemy = PickOneOf(0, 3, 15, 17, 18, 19, 23, 28, 30, 32, 33, 133, 139); break;
                case DFRegion.DungeonTypes.Laboratory: mainEnemy = PickOneOf(0, 1, 3, 9, 13, 14, 17, 22, 32, 33, 35, 36, 37, 38, 128, 129, 130, 131, 133); break;
                case DFRegion.DungeonTypes.HarpyNest: mainEnemy = PickOneOf(0, 1, 3, 9, 13, 14, 17, 22, 32, 33, 35, 36, 37, 38, 128, 129, 130, 131, 133); break;
                case DFRegion.DungeonTypes.RuinedCastle: mainEnemy = PickOneOf(0, 3, 6, 7, 8, 9, 12, 14, 15, 16, 17, 19, 20, 21, 22, 24, 28, 30, 32, 33, 34, 135, 136, 138, 139, 141, 144, 145); break;
                case DFRegion.DungeonTypes.SpiderNest: mainEnemy = PickOneOf(0, 1, 2, 3, 6, 15, 20, 32, 33, 135, 136, 138, 139); break;
                case DFRegion.DungeonTypes.GiantStronghold: mainEnemy = PickOneOf(0, 3, 4, 5, 6, 7, 8, 12, 16, 20, 21, 22, 24); break;
                case DFRegion.DungeonTypes.DragonsDen: mainEnemy = PickOneOf(0, 3, 4, 5, 6, 7, 8, 9, 12, 13, 14, 16, 20, 21, 22, 24, 26, 27, 31, 34, 35, 128, 129, 130, 131, 135, 136, 138, 143, 144, 145); break;
                case DFRegion.DungeonTypes.BarbarianStronghold: mainEnemy = PickOneOf(0, 3, 4, 5, 6, 7, 8, 9, 10, 14, 16, 20, 134, 136, 137, 138, 142, 143, 144); break;
                case DFRegion.DungeonTypes.VolcanicCaves: mainEnemy = PickOneOf(1, 15, 22, 26, 27, 29, 31, 32, 33, 34, 35, 36, 128, 131); break;
                case DFRegion.DungeonTypes.ScorpionNest: mainEnemy = PickOneOf(0, 1, 2, 3, 6, 15, 20, 32, 33, 135, 136, 138, 139); break;
                case DFRegion.DungeonTypes.Cemetery: mainEnemy = PickOneOf(0, 3, 6, 15, 17, 18, 19, 23, 28, 30, 32, 33, 135, 136, 138, 139); break;
                default: return;
            }

            if (eventType == 0)
                DoThing((MobileTypes)mainEnemy);
            else if (eventType == 1)
                DoThing2();
            else if (eventType == 2)
                DoThing3();
            else
                DoThing4();
        }

        public void DoThing (MobileTypes enemyType) // Just for testing right now.
        {
            int eventScale = PickOneOf(0, 1, 2, 3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.
            GameObject mobile = null;

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 7, 8, 9, 10, 12, 13, 14, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145 }))
                {
                    SimpleEncounterTextInitiate("Lone_" + enemyType.ToString());
                    mobile = CreateSingleEnemy("Lone_" + enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
            }
            else if (eventScale == 1)
                return;
            else if (eventScale == 2)
                return;
            else if (eventScale == 3)
                return;
        }

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
                    soloMob = (MobileTypes)PickOneOf(28, 30);
                    SimpleEncounterTextInitiate("Lone_Nice_Vampire");
                    mobile = CreateSingleEnemy("Lone_Nice_Vampire", soloMob, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                default:
                    return false;
            }
        }

        public bool RollBadWildernessNightEncounter()
        {
            //latestEncounterIndex = PickOneOf(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            latestEncounterIndex = PickOneOf(1, 2, 3, 4, 5, 6, 7);
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
                case 3: // Encounter a lone atronach, likely the creation of an irresponsible or possibly even malicious wizard or mage set loose onto the world.
                    soloMob = (MobileTypes)PickOneOf(35, 36, 37, 38);
                    SimpleEncounterTextInitiate("Lone_Atronach");
                    mobile = CreateSingleEnemy("", soloMob);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 4: // Encounter a lone "lesser" undead creature, possibly distrubed a shallow grave or from a wayward necromancer/mage.
                    soloMob = (MobileTypes)PickOneOf(15, 17, 19);
                    SimpleEncounterTextInitiate("Lone_Lesser_Undead");
                    mobile = CreateSingleEnemy("", soloMob);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 5: // Encounter a lone wandering/disturbed spirit undead enemy.
                    soloMob = (MobileTypes)PickOneOf(18, 23);
                    SimpleEncounterTextInitiate("Lone_Angry_Spirit");
                    mobile = CreateSingleEnemy("", soloMob);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 6: // Encounter a lone hostile blood-thirsty lesser vampire enemy.
                    soloMob = (MobileTypes)PickOneOf(28);
                    SimpleEncounterTextInitiate("Lone_Lesser_Vampire");
                    mobile = CreateSingleEnemy("", soloMob);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                case 7: // Encounter a lone hostile werebeast enemy.
                    soloMob = (MobileTypes)PickOneOf(9, 14);
                    SimpleEncounterTextInitiate("Lone_Werebeast");
                    mobile = CreateSingleEnemy("", soloMob);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                    return true;
                default:
                    return false;
            }
        }

        public static int PickOneOf(params int[] values) // Pango provided assistance in making this much cleaner way of doing the random value choice part, awesome.
        {
            return values[UnityEngine.Random.Range(0, values.Length)];
        }

        public static bool CheckArrayForValue(int num, int[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (num == values[i])
                    return true;
            }
            return false;
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
