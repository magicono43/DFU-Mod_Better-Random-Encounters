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
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallConnect.Arena2;
using BetterRandomEncounters;
using System.Linq;

namespace BetterRandomEncounters
{
    public partial class BREWork : MonoBehaviour
    {
        private bool gameStarted = false;
        public int latestEncounterIndex = 0;
        private uint lastGameMinutes = 0;         // Being tracked in order to perform updates based on changes in the current game minute
        PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
        GameObject player = GameManager.Instance.PlayerObject;
        GameObject[] mobile;

        public static string choiceBoxEventName = "";

        TextFile.Token[] pendingEventInitialTokens = null;
        public bool tryEventPlacement = false;
        List<GameObject> pendingEventEnemies = new List<GameObject>();
        int enemiesSpawnedIndex = 0;

        void Start()
        {
            StartGameBehaviour.OnStartGame += BREWork_OnStartGame;
            SaveLoadManager.OnLoad += BREWork_OnSaveLoad;
        }

        void BREWork_OnStartGame(object sender, EventArgs e)
        {
            lastGameMinutes = 0;
            pendingEventInitialTokens = null;
            tryEventPlacement = false;
            pendingEventEnemies = new List<GameObject>();
            enemiesSpawnedIndex = 0;
        }

        void BREWork_OnSaveLoad(SaveData_v1 saveData)
        {
            lastGameMinutes = 0;
            pendingEventInitialTokens = null;
            tryEventPlacement = false;
            pendingEventEnemies = new List<GameObject>();
            enemiesSpawnedIndex = 0;
        }

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
                if (GameManager.Instance.EntityEffectBroker.SyntheticTimeIncrease) { lastGameMinutes = gameMinutes; }

                //Debug.Log((gameMinutes - lastGameMinutes).ToString());
                for (uint l = 0; l < (gameMinutes - lastGameMinutes); ++l)
                {
                    // Catch up time and break if something spawns. Don't spawn encounters while player is on ship.
                    if (!GameManager.Instance.TransportManager.IsOnShip() &&
                        ModdedEventSpawnCheck(l + lastGameMinutes + 1))
                        break;
                }
            }

            if (tryEventPlacement) // The multi-spawn might have something to do with the "PreventEnemySpawns" maybe? Not sure honestly, will need to do more testing to figure it out later.
            {
                Debug.Log("Try Event Placement Is Trying To Do Something Right Now!");

                if (pendingEventEnemies == null || pendingEventEnemies.Count == 0) { pendingEventInitialTokens = null; tryEventPlacement = false; pendingEventEnemies = new List<GameObject>(); enemiesSpawnedIndex = 0; }
                else { TryPlacingEventObjects(); }

                if (enemiesSpawnedIndex >= pendingEventEnemies.Count)
                {
                    PopRegularText(pendingEventInitialTokens); // Later on in development, likely make many of these "safe" encounters skippable with a yes/no prompt.

                    for (int i = 0; i < pendingEventEnemies.Count; i++)
                    {
                        var bREObject = pendingEventEnemies[i].GetComponent<BRECustomObject>();

                        if (bREObject.ReadyToSpawn)
                            pendingEventEnemies[i].SetActive(true);
                    }

                    pendingEventInitialTokens = null;
                    tryEventPlacement = false;
                    enemiesSpawnedIndex = 0;
                    pendingEventEnemies = new List<GameObject>();
                }
            }

            lastGameMinutes = gameMinutes;

            // Allow enemy spawns again if they have been disabled
            if (playerEntity.PreventEnemySpawns)
                playerEntity.PreventEnemySpawns = false;
        }

        public bool ModdedEventSpawnCheck(uint Minutes)
        {
            // Do not allow spawns if not enough time has passed
            bool timeForSpawn = (Minutes % 90) == 0; // Will have to test around with this, kind of strange to me at least how it might actually work in practice.
            if (!timeForSpawn)
                return false;

            if (GameManager.Instance.EntityEffectBroker.SyntheticTimeIncrease) // Attempts to prevent events from immediatley spawning after fast traveling, will have to see if it works or not.
                return false;

            if (pendingEventEnemies != null && pendingEventEnemies.Count >= 1 && tryEventPlacement) // Attempt to prevent more encounters from being qued while another is already currently trying to spawn.
                return false;

            DFRegion regionData = GameManager.Instance.PlayerGPS.CurrentRegion;
            MapsFile.Climates climate = (MapsFile.Climates)GameManager.Instance.PlayerGPS.CurrentClimateIndex;

            // Spawns when player is outside
            if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                uint timeOfDay = Minutes % 1440; // 1440 minutes in a day
                if (GameManager.Instance.PlayerGPS.IsPlayerInLocationRect)
                {
                    DFLocation locationData = GameManager.Instance.PlayerGPS.CurrentLocation;
                    DFRegion.DungeonTypes dungeonType = DFRegion.DungeonTypes.NoDungeon;
                    DFRegion.LocationTypes locationType = regionData.MapTable[locationData.LocationIndex].LocationType;
                    if (locationData.HasDungeon)
                        dungeonType = regionData.MapTable[locationData.LocationIndex].DungeonType;

                    if (timeOfDay >= 360 && timeOfDay <= 1080)
                    {
                        // In a location area during day

                        if (locationData.HasDungeon && (dungeonType != DFRegion.DungeonTypes.NoDungeon || regionData.MapTable[locationData.LocationIndex].LocationType != DFRegion.LocationTypes.TownCity))
                        {
                            if (UnityEngine.Random.Range(0, 2) == 0) { ChooseDungeonExteriorEncounter(dungeonType, climate, 0); } // (0, 11) == 0)
                            else if (UnityEngine.Random.Range(0, 2) == 0) { ChooseDungeonExteriorEncounter(dungeonType, climate, 1); } // (0, 26) == 0)
                        }
                        else
                        {
                            if (UnityEngine.Random.Range(0, 2) == 0) { ChooseLocationExteriorEncounter(locationType, climate, 0); } // (0, 11) == 0)
                            else if (UnityEngine.Random.Range(0, 2) == 0) { ChooseLocationExteriorEncounter(locationType, climate, 1); } // (0, 26) == 0)
                        }
                    }
                    else
                    {
                        // In a location area at night

                        if (locationData.HasDungeon && (dungeonType != DFRegion.DungeonTypes.NoDungeon || regionData.MapTable[locationData.LocationIndex].LocationType != DFRegion.LocationTypes.TownCity))
                        {
                            if (UnityEngine.Random.Range(0, 2) == 0) { ChooseDungeonExteriorEncounter(dungeonType, climate, 2); } // (0, 26) == 0)
                            else if (UnityEngine.Random.Range(0, 2) == 0) { ChooseDungeonExteriorEncounter(dungeonType, climate, 3); } // (0, 11) == 0)
                        }
                        else
                        {
                            if (UnityEngine.Random.Range(0, 2) == 0) { ChooseLocationExteriorEncounter(locationType, climate, 2); } // (0, 26) == 0)
                            else if (UnityEngine.Random.Range(0, 2) == 0) { ChooseLocationExteriorEncounter(locationType, climate, 3); } // (0, 11) == 0)
                        }
                    }
                }
                else
                {
                    if (timeOfDay >= 360 && timeOfDay <= 1080)
                    {
                        // Wilderness during day

                        // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        if (UnityEngine.Random.Range(0, 2) == 0) { ChooseWildernessEncounter(climate, 0); } // (0, 11) == 0)
                        else if (UnityEngine.Random.Range(0, 2) == 0) { ChooseWildernessEncounter(climate, 1); } // (0, 26) == 0)
                    }
                    else
                    {
                        // Wilderness at night

                        // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        if (UnityEngine.Random.Range(0, 2) == 0) { ChooseWildernessEncounter(climate, 2); } // (0, 26) == 0)
                        else if (UnityEngine.Random.Range(0, 2) == 0) { ChooseWildernessEncounter(climate, 3); } // (0, 11) == 0)
                    }
                }
            }

            // Spawns when player is inside
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                // Spawns when player is inside a dungeon
                if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
                {
                    DFLocation locationData = GameManager.Instance.PlayerGPS.CurrentLocation;
                    DFRegion.DungeonTypes dungeonType = regionData.MapTable[locationData.LocationIndex].DungeonType;

                    if (UnityEngine.Random.Range(0, 2) == 0) { ChooseDungeonInteriorEncounter(dungeonType, climate, 0); } // (0, 26) == 0)
                    else if (UnityEngine.Random.Range(0, 2) == 0) { ChooseDungeonInteriorEncounter(dungeonType, climate, 1); } // (0, 11) == 0)
                }
            }

            if (pendingEventEnemies != null && pendingEventEnemies.Count >= 1) { tryEventPlacement = true; return true; } // If event objects are present, flag to attempt spawning any event future update cycles, also return true.
            else { return true; } // If no event objects are present, return true but don't flag to attempt spawning any event.
        }

        #region Work Methods

        public void ChooseDungeonExteriorEncounter (DFRegion.DungeonTypes dungeonType, MapsFile.Climates climate, int eventType)
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

            if (ClimateSpawnExceptions(climate, (MobileTypes)mainEnemy)) // Possibly change this somehow so something new is selected instead of basically aborting the entire event due to enemy type.
                return;

            if (eventType == 0)
                CreateGoodDayEvent((MobileTypes)mainEnemy);
            else if (eventType == 1)
                CreateBadDayEvent((MobileTypes)mainEnemy);
            else if (eventType == 2)
                CreateGoodNightEvent((MobileTypes)mainEnemy);
            else
                CreateBadNightEvent((MobileTypes)mainEnemy);
        }

        public void ChooseLocationExteriorEncounter(DFRegion.LocationTypes locationType, MapsFile.Climates climate, int eventType)
        {
            int mainEnemy = -1;

            switch (locationType) // Will have to edit these values, but just place-holder for the time being.
            {
                case DFRegion.LocationTypes.TownCity:
                case DFRegion.LocationTypes.TownHamlet:
                case DFRegion.LocationTypes.TownVillage:
                case DFRegion.LocationTypes.HomeFarms:
                case DFRegion.LocationTypes.ReligionTemple:
                case DFRegion.LocationTypes.Tavern:
                case DFRegion.LocationTypes.HomeWealthy:
                case DFRegion.LocationTypes.ReligionCult:
                case DFRegion.LocationTypes.HomePoor:
                case DFRegion.LocationTypes.Coven:
                    mainEnemy = PickOneOf(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145);
                    break;
                default: return;
            }

            if (ClimateSpawnExceptions(climate, (MobileTypes)mainEnemy)) // Possibly change this somehow so something new is selected instead of basically aborting the entire event due to enemy type.
                return;

            if (eventType == 0)
                CreateGoodDayEvent((MobileTypes)mainEnemy);
            else if (eventType == 1)
                CreateBadDayEvent((MobileTypes)mainEnemy);
            else if (eventType == 2)
                CreateGoodNightEvent((MobileTypes)mainEnemy);
            else
                CreateBadNightEvent((MobileTypes)mainEnemy);
        }

        public void ChooseWildernessEncounter(MapsFile.Climates climate, int eventType)
        {
            int mainEnemy = -1;

            mainEnemy = PickOneOf(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145);

            if (ClimateSpawnExceptions(climate, (MobileTypes)mainEnemy)) // Possibly change this somehow so something new is selected instead of basically aborting the entire event due to enemy type.
                return;

            if (eventType == 0)
                CreateGoodDayEvent((MobileTypes)mainEnemy);
            else if (eventType == 1)
                CreateBadDayEvent((MobileTypes)mainEnemy);
            else if (eventType == 2)
                CreateGoodNightEvent((MobileTypes)mainEnemy);
            else
                CreateBadNightEvent((MobileTypes)mainEnemy);
        }

        public void ChooseDungeonInteriorEncounter(DFRegion.DungeonTypes dungeonType, MapsFile.Climates climate, int eventType)
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

            if (ClimateSpawnExceptions(climate, (MobileTypes)mainEnemy)) // Possibly change this somehow so something new is selected instead of basically aborting the entire event due to enemy type.
                return;

            if (eventType == 0)
                CreateGoodInteriorEvent((MobileTypes)mainEnemy);
            else
                CreateBadInteriorEvent((MobileTypes)mainEnemy);
        }

        public bool ClimateSpawnExceptions(MapsFile.Climates climate, MobileTypes enemy)
        {
            bool climateCheck = false;
            bool seasonCheck = false;

            switch (climate)
            {
                case MapsFile.Climates.Ocean: if (CheckArrayForValue((int)enemy, new int[] { 0, 2, 4, 5, 6, 20, 26, 35 })) { climateCheck = true; break; } else { break; }
                case MapsFile.Climates.Desert: if (CheckArrayForValue((int)enemy, new int[] { 4, 6, 10, 25, 38 })) { climateCheck = true; break; } else { break; }
                case MapsFile.Climates.Desert2: if (CheckArrayForValue((int)enemy, new int[] { 4, 6, 10, 25, 38 })) { climateCheck = true; break; } else { break; }
                case MapsFile.Climates.Mountain: if (CheckArrayForValue((int)enemy, new int[] { 2, 8, 20 })) { climateCheck = true; break; } else { break; }
                case MapsFile.Climates.Rainforest: if (CheckArrayForValue((int)enemy, new int[] { 4 })) { climateCheck = true; break; } else { break; }
                case MapsFile.Climates.Swamp: if (CheckArrayForValue((int)enemy, new int[] { 4, 5, 20, 26, 35 })) { climateCheck = true; break; } else { break; }
                case MapsFile.Climates.Subtropical: if (CheckArrayForValue((int)enemy, new int[] { 25, 38 })) { climateCheck = true; break; } else { break; }
                case MapsFile.Climates.MountainWoods: if (CheckArrayForValue((int)enemy, new int[] { 8, 20 })) { climateCheck = true; break; } else { break; }
                case MapsFile.Climates.Woodlands: if (CheckArrayForValue((int)enemy, new int[] { 20 })) { climateCheck = true; break; } else { break; }
                case MapsFile.Climates.HauntedWoodlands: if (CheckArrayForValue((int)enemy, new int[] { 20 })) { climateCheck = true; break; } else { break; }
                default: break;
            }

            if (climateCheck) { return true; }

            switch (DaggerfallUnity.Instance.WorldTime.Now.MonthValue)
            {
                //Spring
                case DaggerfallDateTime.Months.FirstSeed:
                case DaggerfallDateTime.Months.RainsHand:
                case DaggerfallDateTime.Months.SecondSeed:
                    break;
                //Summer
                case DaggerfallDateTime.Months.Midyear:
                case DaggerfallDateTime.Months.SunsHeight:
                case DaggerfallDateTime.Months.LastSeed:
                    if (CheckArrayForValue((int)enemy, new int[] { 25, 38 })) { seasonCheck = true; break; } else { break; }
                //Fall
                case DaggerfallDateTime.Months.Hearthfire:
                case DaggerfallDateTime.Months.Frostfall:
                case DaggerfallDateTime.Months.SunsDusk:
                    break;
                //Winter
                case DaggerfallDateTime.Months.EveningStar:
                case DaggerfallDateTime.Months.MorningStar:
                case DaggerfallDateTime.Months.SunsDawn:
                    if (CheckArrayForValue((int)enemy, new int[] { 26, 35 })) { seasonCheck = true; break; } else { break; }
                default: break;
            }

            if (seasonCheck) { return true; }

            if (GameManager.Instance.WeatherManager.IsRaining && CheckArrayForValue((int)enemy, new int[] { 6, 20, 26, 35 })) { return true; }
            else if (GameManager.Instance.WeatherManager.IsSnowing && CheckArrayForValue((int)enemy, new int[] { 6, 20, 26, 35 })) { return true; }
            else if (GameManager.Instance.WeatherManager.IsStorming && CheckArrayForValue((int)enemy, new int[] { 6, 20, 26, 35 })) { return true; }
            else { return false; }
        }

        public int[] ClimateSpawnExceptions(MapsFile.Climates climate, List<int> checkList)
        {
            List<int> resultEnemies = checkList;
            List<int> exceptions;

            switch (climate)
            {
                case MapsFile.Climates.Ocean: exceptions = new List<int>() { 0, 2, 4, 5, 6, 20, 26, 35 }; break;
                case MapsFile.Climates.Desert: exceptions = new List<int>() { 4, 6, 10, 25, 38 }; break;
                case MapsFile.Climates.Desert2: exceptions = new List<int>() { 4, 6, 10, 25, 38 }; break;
                case MapsFile.Climates.Mountain: exceptions = new List<int>() { 2, 8, 20 }; break;
                case MapsFile.Climates.Rainforest: exceptions = new List<int>() { 4 }; break;
                case MapsFile.Climates.Swamp: exceptions = new List<int>() { 4, 5, 20, 26, 35 }; break;
                case MapsFile.Climates.Subtropical: exceptions = new List<int>() { 25, 38 }; break;
                case MapsFile.Climates.MountainWoods: exceptions = new List<int>() { 8, 20 }; break;
                case MapsFile.Climates.Woodlands: exceptions = new List<int>() { 20 }; break;
                case MapsFile.Climates.HauntedWoodlands: exceptions = new List<int>() { 20 }; break;
                default: exceptions = new List<int>() { }; break;
            }

            switch (DaggerfallUnity.Instance.WorldTime.Now.MonthValue)
            {
                //Spring
                case DaggerfallDateTime.Months.FirstSeed:
                case DaggerfallDateTime.Months.RainsHand:
                case DaggerfallDateTime.Months.SecondSeed:
                    break;
                //Summer
                case DaggerfallDateTime.Months.Midyear:
                case DaggerfallDateTime.Months.SunsHeight:
                case DaggerfallDateTime.Months.LastSeed:
                    exceptions.Add(25); exceptions.Add(38); break;
                //Fall
                case DaggerfallDateTime.Months.Hearthfire:
                case DaggerfallDateTime.Months.Frostfall:
                case DaggerfallDateTime.Months.SunsDusk:
                    break;
                //Winter
                case DaggerfallDateTime.Months.EveningStar:
                case DaggerfallDateTime.Months.MorningStar:
                case DaggerfallDateTime.Months.SunsDawn:
                    exceptions.Add(26); exceptions.Add(35); break;
                default: break;
            }

            if (GameManager.Instance.WeatherManager.IsRaining ||
                GameManager.Instance.WeatherManager.IsSnowing ||
                GameManager.Instance.WeatherManager.IsStorming) { exceptions.Add(6); exceptions.Add(20); exceptions.Add(26); exceptions.Add(35); }

            int[] exceptionsArray = exceptions.ToArray();

            for (int i = 0; i < checkList.Count; i++)
            {
                if (CheckArrayForValue(checkList[i], exceptionsArray))
                {
                    resultEnemies.RemoveAt(i);
                }
            }

            return resultEnemies.ToArray();
        }

        public void CreateGoodDayEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 32, 33, 128, 129, 130, 131, 132, 134, 137, 140, 141, 142, 143, 144, 145 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25, 26, 27, 29, 31, 34, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                }
            }
            else if (eventScale == 1)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 32, 33, 128, 129, 130, 131, 132, 134, 137, 140, 141, 142, 143, 144, 145 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25, 26, 27, 29, 31, 34, 35, 36, 37, 38 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                }
            }
            else if (eventScale == 2)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 32, 33, 128, 129, 130, 131, 132, 134, 137, 140, 141, 142, 143, 144, 145 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25, 26, 27, 29, 31, 34, 35, 36, 37, 38 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                }
            }
            else if (eventScale == 3) // Going to have to do work to see how the final implementation of these will work in terms of event choices, also enemy descrim. or not.
            {
                if (pendingEventEnemies != null && pendingEventEnemies.Count >= 1) { return; }

                Traveling_Alchemist_Solo();
                return;
            }
        }

        public void CreateBadDayEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(0, 1, 2, 3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 128, 129, 130, 131, 132, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 31, 32, 33, 34, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
            }
            else if (eventScale == 1)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 128, 129, 130, 131, 132, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 31, 32, 33, 34, 35, 36, 37, 38 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
            }
            else if (eventScale == 2)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 128, 129, 130, 131, 132, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 31, 32, 33, 34, 35, 36, 37, 38 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
            }
            else if (eventScale == 3)
                return;
        }

        public void CreateGoodNightEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 28, 30, 32, 33, 133, 134, 135, 136, 138, 139, 142, 143 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 9, 10, 14, 15, 17, 18, 19, 22, 23, 25, 26, 27, 29, 31, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                }
            }
            else if (eventScale == 1)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 28, 30, 32, 33, 133, 134, 135, 136, 138, 139, 142, 143 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 9, 10, 14, 15, 17, 18, 19, 22, 23, 25, 26, 27, 29, 31, 35, 36, 37, 38 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                }
            }
            else if (eventScale == 2)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 28, 30, 32, 33, 133, 134, 135, 136, 138, 139, 142, 143 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 9, 10, 14, 15, 17, 18, 19, 22, 23, 25, 26, 27, 29, 31, 35, 36, 37, 38 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                }
            }
            else if (eventScale == 3)
            {
                if (pendingEventEnemies != null && pendingEventEnemies.Count >= 1) { return; }

                Traveling_Alchemist_Solo();
                return;
            }
        }

        public void CreateBadNightEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(0, 1, 2, 3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 133, 134, 135, 136, 138, 139, 142, 143 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
                if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 6, 9, 10, 14, 15, 17, 18, 19, 20, 22, 23, 25, 26, 27, 28, 29, 30, 31, 32, 33, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
            }
            else if (eventScale == 1)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 133, 134, 135, 136, 138, 139, 142, 143 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 6, 9, 10, 14, 15, 17, 18, 19, 20, 22, 23, 25, 26, 27, 28, 29, 30, 31, 32, 33, 35, 36, 37, 38 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
            }
            else if (eventScale == 2)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 133, 134, 135, 136, 138, 139, 142, 143 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 6, 9, 10, 14, 15, 17, 18, 19, 20, 22, 23, 25, 26, 27, 28, 29, 30, 31, 32, 33, 35, 36, 37, 38 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
            }
            else if (eventScale == 3)
                return;
        }

        public void CreateGoodInteriorEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 28, 30, 32, 33, 128, 129, 130, 131, 132, 134, 137, 140, 141, 142, 143, 144, 145 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25, 26, 27, 29, 31, 34, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                }
            }
            else if (eventScale == 1)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 28, 30, 32, 33, 128, 129, 130, 131, 132, 134, 137, 140, 141, 142, 143, 144, 145 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25, 26, 27, 29, 31, 34, 35, 36, 37, 38 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                }
            }
            else if (eventScale == 2)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 28, 30, 32, 33, 128, 129, 130, 131, 132, 134, 137, 140, 141, 142, 143, 144, 145 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25, 26, 27, 29, 31, 34, 35, 36, 37, 38 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Friendly", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                }
            }
            else if (eventScale == 3)
            {
                if (pendingEventEnemies != null && pendingEventEnemies.Count >= 1) { return; }

                Traveling_Alchemist_Solo();
                return;
            }
        }

        public void CreateBadInteriorEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(0, 1, 2, 3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 128, 129, 130, 131, 132, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 6, 9, 10, 14, 15, 17, 18, 19, 20, 22, 23, 25, 26, 27, 28, 29, 30, 31, 32, 33, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
            }
            else if (eventScale == 1)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 128, 129, 130, 131, 132, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 6, 9, 10, 14, 15, 17, 18, 19, 20, 22, 23, 25, 26, 27, 28, 29, 30, 31, 32, 33, 35, 36, 37, 38 }))
                {
                    SmallGroupEncounterTextInitiate("Small_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateSmallEnemyGroup("Small_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
            }
            else if (eventScale == 2)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 128, 129, 130, 131, 132, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 6, 9, 10, 14, 15, 17, 18, 19, 20, 22, 23, 25, 26, 27, 28, 29, 30, 31, 32, 33, 35, 36, 37, 38 }))
                {
                    LargeGroupEncounterTextInitiate("Large_Group_Hostile", enemyType.ToString(), (int)enemyType);
                    CreateLargeEnemyGroup("Large_Group_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                }
            }
            else if (eventScale == 3)
                return;
        }

        public void CreateSingleEnemy(string eventName, string enemyName, MobileTypes enemyType = MobileTypes.Acrobat, MobileReactions enemyReact = MobileReactions.Hostile, bool hasGreet = false, bool hasAdd = false, bool hasAggro = false)
        {
            if (pendingEventEnemies != null && pendingEventEnemies.Count >= 1) { return; }

            GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, enemyType, 1, enemyReact);
            mobile[0].AddComponent<BRECustomObject>();
            BRECustomObject bRECustomObject = mobile[0].GetComponent<BRECustomObject>();

            bRECustomObject.IsGangLeader = true;

            if (eventName != "") // Basically for spawning single non-verbal enemies sort of things, even if the event does have a proper name for the initiation part.
            {
                if (hasGreet) { bRECustomObject.GreetingText = BREGenericText.IndividualNPCTextFinder(eventName, enemyName, "Greet"); bRECustomObject.HasGreeting = true; }
                if (hasAdd) { bRECustomObject.AdditionalText = BREGenericText.IndividualNPCTextFinder(eventName, enemyName, "Add"); bRECustomObject.HasMoreText = true; }
                if (hasAggro) { bRECustomObject.AggroText = BREGenericText.IndividualNPCTextFinder(eventName, enemyName, "Aggro"); bRECustomObject.HasAggroText = true; }
            }
            pendingEventEnemies.Add(mobile[0]);
        }

        public void LoneEncounterTextInitiate(string eventName, string enemyName, int enemyID)
        {
            TextFile.Token[] tokens = null;

            DaggerfallMessageBox initialEventPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            ModManager.Instance.SendModMessage("TravelOptions", "pauseTravel"); // Will need to do more with these parts later on, but for now basically do nothing.

            tokens = BREGenericText.LoneEncounterTextFinder(eventName, enemyName, enemyID); // Possibly just pass enemy gameobject later for easier use of object properties like gender, name, etc.

            pendingEventInitialTokens = tokens;
        }

        public void CreateSmallEnemyGroup(string eventName, string enemyName, MobileTypes enemyType = MobileTypes.Acrobat, MobileReactions enemyReact = MobileReactions.Hostile, bool hasGreet = false, bool hasAdd = false, bool hasAggro = false)
        {
            if (pendingEventEnemies != null && pendingEventEnemies.Count >= 1) { return; }

            ulong alliedID = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToSeconds();
            int size = UnityEngine.Random.Range(3, 5);
            GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, enemyType, 1, enemyReact);
            DaggerfallEntityBehaviour behaviour = mobile[0].GetComponent<DaggerfallEntityBehaviour>();
            EnemyEntity entity = behaviour.Entity as EnemyEntity;
            MobileTeams team = entity.Team;
            mobile[0].AddComponent<BRECustomObject>();
            BRECustomObject bRECustomObject = mobile[0].GetComponent<BRECustomObject>();
            bRECustomObject.LinkedAlliesID = alliedID;

            for (int i = 0; i < size; i++) // Based on how many enemies plan to be spawning in a specific encounter.
            {
                if (i == 0) // For the leader specifically
                {
                    bRECustomObject.IsGangLeader = true;

                    if (eventName != "")
                    {
                        if (hasGreet) { bRECustomObject.GreetingText = BREGenericText.IndividualNPCTextFinder(eventName+"_Leader", enemyName, "Greet"); bRECustomObject.HasGreeting = true; }
                        if (hasAdd) { bRECustomObject.AdditionalText = BREGenericText.IndividualNPCTextFinder(eventName + "_Leader", enemyName, "Add"); bRECustomObject.HasMoreText = true; }
                        if (hasAggro) { bRECustomObject.AggroText = BREGenericText.IndividualNPCTextFinder(eventName + "_Leader", enemyName, "Aggro"); bRECustomObject.HasAggroText = true; }
                    }
                }
                else // For the followers of the leader
                {
                    mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, ChooseEnemyFollowers(enemyType), 1, enemyReact);
                    mobile[0].AddComponent<BRECustomObject>();
                    bRECustomObject = mobile[0].GetComponent<BRECustomObject>();
                    behaviour = mobile[0].GetComponent<DaggerfallEntityBehaviour>();
                    entity = behaviour.Entity as EnemyEntity;
                    entity.Team = team;
                    bRECustomObject.LinkedAlliesID = alliedID;

                    if (eventName != "")
                    {
                        if (hasGreet) { bRECustomObject.GreetingText = BREGenericText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Greet"); bRECustomObject.HasGreeting = true; }
                        if (hasAdd) { bRECustomObject.AdditionalText = BREGenericText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Add"); bRECustomObject.HasMoreText = true; }
                        if (hasAggro) { bRECustomObject.AggroText = BREGenericText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Aggro"); bRECustomObject.HasAggroText = true; }
                    }
                }

                pendingEventEnemies.Add(mobile[0]);
            }
        }

        public void SmallGroupEncounterTextInitiate(string eventName, string enemyName, int enemyID)
        {
            TextFile.Token[] tokens = null;

            DaggerfallMessageBox initialEventPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            ModManager.Instance.SendModMessage("TravelOptions", "pauseTravel"); // Will need to do more with these parts later on, but for now basically do nothing.

            tokens = BREGenericText.SmallGroupEncounterTextFinder(eventName, enemyName, enemyID); // Possibly just pass enemy gameobject later for easier use of object properties like gender, name, etc.

            pendingEventInitialTokens = tokens;
        }

        public void CreateLargeEnemyGroup(string eventName, string enemyName, MobileTypes enemyType = MobileTypes.Acrobat, MobileReactions enemyReact = MobileReactions.Hostile, bool hasGreet = false, bool hasAdd = false, bool hasAggro = false)
        {
            if (pendingEventEnemies != null && pendingEventEnemies.Count >= 1) { return; }

            ulong alliedID = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToSeconds();
            int size = UnityEngine.Random.Range(6, 10);
            GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, enemyType, 1, enemyReact);
            DaggerfallEntityBehaviour behaviour = mobile[0].GetComponent<DaggerfallEntityBehaviour>();
            EnemyEntity entity = behaviour.Entity as EnemyEntity;
            MobileTeams team = entity.Team;
            mobile[0].AddComponent<BRECustomObject>();
            BRECustomObject bRECustomObject = mobile[0].GetComponent<BRECustomObject>();
            bRECustomObject.LinkedAlliesID = alliedID;

            for (int i = 0; i < size; i++) // Based on how many enemies plan to be spawning in a specific encounter.
            {
                if (i == 0) // For the leader specifically
                {
                    bRECustomObject.IsGangLeader = true;

                    if (eventName != "")
                    {
                        if (hasGreet) { bRECustomObject.GreetingText = BREGenericText.IndividualNPCTextFinder(eventName + "_Leader", enemyName, "Greet"); bRECustomObject.HasGreeting = true; }
                        if (hasAdd) { bRECustomObject.AdditionalText = BREGenericText.IndividualNPCTextFinder(eventName + "_Leader", enemyName, "Add"); bRECustomObject.HasMoreText = true; }
                        if (hasAggro) { bRECustomObject.AggroText = BREGenericText.IndividualNPCTextFinder(eventName + "_Leader", enemyName, "Aggro"); bRECustomObject.HasAggroText = true; }
                    }
                }
                else // For the followers of the leader
                {
                    mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, ChooseEnemyFollowers(enemyType), 1, enemyReact);
                    mobile[0].AddComponent<BRECustomObject>();
                    bRECustomObject = mobile[0].GetComponent<BRECustomObject>();
                    behaviour = mobile[0].GetComponent<DaggerfallEntityBehaviour>();
                    entity = behaviour.Entity as EnemyEntity;
                    entity.Team = team;
                    bRECustomObject.LinkedAlliesID = alliedID;

                    if (eventName != "")
                    {
                        if (hasGreet) { bRECustomObject.GreetingText = BREGenericText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Greet"); bRECustomObject.HasGreeting = true; }
                        if (hasAdd) { bRECustomObject.AdditionalText = BREGenericText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Add"); bRECustomObject.HasMoreText = true; }
                        if (hasAggro) { bRECustomObject.AggroText = BREGenericText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Aggro"); bRECustomObject.HasAggroText = true; }
                    }
                }

                pendingEventEnemies.Add(mobile[0]);
            }
        }

        public void LargeGroupEncounterTextInitiate(string eventName, string enemyName, int enemyID)
        {
            TextFile.Token[] tokens = null;

            DaggerfallMessageBox initialEventPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            ModManager.Instance.SendModMessage("TravelOptions", "pauseTravel"); // Will need to do more with these parts later on, but for now basically do nothing.

            tokens = BREGenericText.LargeGroupEncounterTextFinder(eventName, enemyName, enemyID); // Possibly just pass enemy gameobject later for easier use of object properties like gender, name, etc.

            pendingEventInitialTokens = tokens;
        }

        public MobileTypes ChooseEnemyFollowers(MobileTypes leader) // Meant for determining encounters where multiple enemies are spawned in a similar group.
        {
            int follower = 0; // Definitely change this so maybe there is a "credit" based thing so multiple difficult enemies can't spawn in large groups, but will see.
            List<int> enemyList = new List<int>();
            int[] humans = { 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145 };
            MapsFile.Climates climate = (MapsFile.Climates)GameManager.Instance.PlayerGPS.CurrentClimateIndex;

            switch (leader)
            {
                case MobileTypes.Spriggan: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 0, 2, 3, 4, 5, 6, 10, 20 })); return (MobileTypes)follower;
                case MobileTypes.Nymph: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 0, 2, 3, 4, 5, 6, 10, 20 })); return (MobileTypes)follower;
                case MobileTypes.OrcSergeant: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 7 })); return (MobileTypes)follower;
                case MobileTypes.SkeletalWarrior: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 15, 17 })); return (MobileTypes)follower;
                case MobileTypes.Giant: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 4, 16 })); return (MobileTypes)follower;
                case MobileTypes.Ghost: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 15, 17, 18 })); return (MobileTypes)follower;
                case MobileTypes.Mummy: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 15, 17 })); return (MobileTypes)follower;
                case MobileTypes.OrcShaman: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 7, 12 })); return (MobileTypes)follower;
                case MobileTypes.Gargoyle: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 16, 22 })); return (MobileTypes)follower;
                case MobileTypes.Wraith: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 15, 17, 18, 19, 23 })); return (MobileTypes)follower;
                case MobileTypes.OrcWarlord: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 7, 12, 21 })); return (MobileTypes)follower;
                case MobileTypes.FrostDaedra: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 38 })); return (MobileTypes)follower;
                case MobileTypes.FireDaedra: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 35 })); return (MobileTypes)follower;
                case MobileTypes.Daedroth: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 36 })); return (MobileTypes)follower;
                case MobileTypes.Vampire: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 15, 17, 19, 28, 200 })); if (follower == 200) { follower = PickOneOf(humans); } return (MobileTypes)follower;
                case MobileTypes.DaedraSeducer: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 7, 8, 12, 16, 25, 26, 27, 200 })); if (follower == 200) { follower = PickOneOf(humans); } return (MobileTypes)follower;
                case MobileTypes.VampireAncient: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 15, 17, 19, 28, 200 })); if (follower == 200) { follower = PickOneOf(humans); } return (MobileTypes)follower;
                case MobileTypes.DaedraLord: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 25, 26, 27, 29 })); return (MobileTypes)follower;
                case MobileTypes.Lich: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 15, 17, 18, 19, 23, 35, 36, 37, 38 })); return (MobileTypes)follower;
                case MobileTypes.AncientLich: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 15, 17, 18, 19, 23, 35, 36, 37, 38 })); return (MobileTypes)follower;
                case MobileTypes.Mage: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 1, 35, 36, 37, 38, 128, 129, 130, 131, 132, 134, 140, 141, 142, 143, 144, 145 })); return (MobileTypes)follower;
                case MobileTypes.Spellsword: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 128, 129, 130, 131, 132, 134, 140, 141, 142, 143, 144, 145 })); return (MobileTypes)follower;
                case MobileTypes.Battlemage: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 1, 128, 129, 130, 131, 132, 134, 140, 141, 142, 143, 144, 145 })); return (MobileTypes)follower;
                case MobileTypes.Sorcerer: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 1, 35, 36, 37, 38, 128, 129, 130, 131, 132, 134, 140, 141, 142, 143, 144, 145 })); return (MobileTypes)follower;
                case MobileTypes.Healer:
                case MobileTypes.Monk: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 128, 129, 130, 131, 132, 134, 140, 141, 142, 143, 144, 145 })); return (MobileTypes)follower;
                case MobileTypes.Nightblade: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 128, 129, 130, 131, 134, 135, 136, 137, 138, 139, 143 })); return (MobileTypes)follower;
                case MobileTypes.Bard: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 200 })); if (follower == 200) { follower = PickOneOf(humans); } return (MobileTypes)follower;
                case MobileTypes.Burglar:
                case MobileTypes.Rogue:
                case MobileTypes.Acrobat:
                case MobileTypes.Thief:
                case MobileTypes.Assassin:
                case MobileTypes.Barbarian: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 134, 135, 136, 137, 138, 139, 143 })); return (MobileTypes)follower;
                case MobileTypes.Archer:
                case MobileTypes.Warrior:
                case MobileTypes.Knight: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 128, 129, 130, 131, 132, 134, 140, 141, 142, 143, 144, 145 })); return (MobileTypes)follower;
                case MobileTypes.Ranger: follower = PickOneOf(ClimateSpawnExceptions(climate, new List<int> { 0, 3, 4, 5, 6, 20, 128, 129, 130, 131, 132, 134, 140, 141, 142, 143, 144, 145 })); return (MobileTypes)follower;
                default: return leader;
            }
        }

        // Uses raycasts to find next spawn position
        void TryPlacingEventObjects(float minDistance = 3f, float maxDistance = 15f) // Make it so different events have different allowed minDistance and maxDistances, when I start making them.
        {
            // Define minimum distance from player based on spawn locations. Not sure if these are useful in this case.
            //const int minDungeonDistance = 8;
            //const int minLocationDistance = 10;
            //const int minWildernessDistance = 10; // Will definitely want to play around with the spawn distance mins and maxes tomorrow and such.

            const float overlapSphereRadius = 0.65f;
            const float separationDistance = 1.25f;
            const float maxFloorDistance = 5f;

            if (enemiesSpawnedIndex >= pendingEventEnemies.Count)
                return;

            // Must have received a valid list
            if (pendingEventEnemies == null || pendingEventEnemies.Count == 0)
                return;

            // Skip this foe if destroyed (e.g. player left building where pending). Will definitely have to change this later, to basically cancel entire event if player changes scenes.
            if (!pendingEventEnemies[enemiesSpawnedIndex])
            {
                enemiesSpawnedIndex++;
                return;
            }

            GameObject anchor;
            BRECustomObject bREObject = pendingEventEnemies[enemiesSpawnedIndex].GetComponent<BRECustomObject>();
            if (bREObject.IsGangLeader)
                anchor = GameManager.Instance.PlayerObject; // Will initially use player as the anchor for where to place the leader object for an event, then use the leader to place the rest.
            else
                anchor = pendingEventEnemies[0]; // Just assuming for now that the "leader" of the event will always be the first object created or the first index value of this list.

            // Get roation of spawn ray
            Quaternion rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);

            // Get direction vector and create a new ray
            Vector3 angle = (rotation * Vector3.forward).normalized;
            Vector3 spawnDirection = anchor.transform.TransformDirection(angle).normalized;
            Ray ray = new Ray(anchor.transform.position, spawnDirection);

            // Check for a hit
            Vector3 currentPoint;
            RaycastHit initialHit;
            if (Physics.Raycast(ray, out initialHit, maxDistance))
            {
                float cos_normal = Vector3.Dot(-spawnDirection, initialHit.normal.normalized);
                if (cos_normal < 1e-6)
                    return;
                float separationForward = separationDistance / cos_normal;

                // Must be greater than minDistance
                float distanceSlack = initialHit.distance - separationForward - minDistance;
                if (distanceSlack < 0f)
                    return;

                // Separate out from hit point
                float extraDistance = UnityEngine.Random.Range(0f, Mathf.Min(2f, distanceSlack));
                currentPoint = initialHit.point - spawnDirection * (separationForward + extraDistance);
            }
            else
            {
                // Player might be in an open area (e.g. outdoors) pick a random point along spawn direction
                currentPoint = anchor.transform.position + spawnDirection * UnityEngine.Random.Range(minDistance, maxDistance);
            }

            // Must be able to find a surface below
            RaycastHit floorHit;
            ray = new Ray(currentPoint, Vector3.down);
            if (!Physics.Raycast(ray, out floorHit, maxFloorDistance))
                return;

            // Ensure this is open space
            Vector3 testPoint = floorHit.point + Vector3.up * separationDistance;
            Collider[] colliders = Physics.OverlapSphere(testPoint, overlapSphereRadius);
            if (colliders.Length > 0)
                return;

            // This looks like a good spawn position
            pendingEventEnemies[enemiesSpawnedIndex].transform.position = testPoint;
            FinalizeFoe(pendingEventEnemies[enemiesSpawnedIndex]);
            pendingEventEnemies[enemiesSpawnedIndex].transform.LookAt(anchor.transform.position);

            // Increment count
            enemiesSpawnedIndex++;
        }

        // Fine tunes foe position slightly based on mobility and enables GameObject
        void FinalizeFoe(GameObject go)
        {
            var mobileUnit = go.GetComponentInChildren<MobileUnit>();
            if (mobileUnit)
            {
                // Align ground creatures on surface, raise flying creatures slightly into air
                if (mobileUnit.Enemy.Behaviour != MobileBehaviour.Flying)
                    GameObjectHelper.AlignControllerToGround(go.GetComponent<CharacterController>());
                else
                    go.transform.localPosition += Vector3.up * 1.5f;
            }
            else
            {
                // Just align to ground
                GameObjectHelper.AlignControllerToGround(go.GetComponent<CharacterController>());
            }

            var bREObject = go.GetComponent<BRECustomObject>();
            bREObject.ReadyToSpawn = true;
        }

        public static void PopRegularText(TextFile.Token[] tokens)
        {
            if (tokens[0].text == "") { return; } // Basically there for the "AggroText" that is shown when an enemy becomes hostile, but for those that should have none but will still pass this method.

            DaggerfallMessageBox textBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            textBox.SetTextTokens(tokens);
            textBox.ClickAnywhereToClose = true;
            textBox.Show();
        }

        public static void PopTextWithChoice(TextFile.Token[] tokens, string eventName, bool lootPileChoice = false)
        {
            if (tokens[0].text == "") { return; }

            choiceBoxEventName = eventName;

            DaggerfallMessageBox textBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            textBox.SetTextTokens(tokens);
            textBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            textBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
            if (lootPileChoice)
                textBox.OnButtonClick += DoLootPileChoice_OnButtonClick;
            else
                textBox.OnButtonClick += DoEventChoice_OnButtonClick;
            textBox.Show();
        }

        public static void DoEventChoice_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                switch (choiceBoxEventName)
                {
                    case "Traveling_Alchemist_Solo": Traveling_Alchemist_Solo_OnYesButton(sender); break; // For testing, make it so this is the only event that can happen for now.
                    default: break;
                }
            }

            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.No)
            {

            }

            choiceBoxEventName = "";
            sender.CloseWindow();
        }

        public static void DoLootPileChoice_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                switch (choiceBoxEventName)
                {
                    case "Traveling_Alchemist_Solo": Traveling_Alchemist_Inventory_Choice_OnYesButton(sender); break;
                    default: break;
                }
            }

            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.No)
            {
                switch (choiceBoxEventName)
                {
                    case "Traveling_Alchemist_Solo": Traveling_Alchemist_Inventory_Choice_OnNoButton(sender); break;
                    default: break;
                }
            }

            choiceBoxEventName = "";
            sender.CloseWindow();
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

        #endregion

        /*public bool RollGoodWildernessDayEncounter()
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
        }*/
    }
}
