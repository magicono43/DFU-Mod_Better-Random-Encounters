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
        public bool tryEventPlacement = false;
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
                        ModdedEventSpawnCheck(l + lastGameMinutes + 1))
                        break;
                }
            }

            if (tryEventPlacement)
            {
                // Do enemy spawning logic.
            }

            lastGameMinutes = gameMinutes;

            // Allow enemy spawns again if they have been disabled
            if (playerEntity.PreventEnemySpawns)
                playerEntity.PreventEnemySpawns = false;
        }

        public bool ModdedEventSpawnCheck(uint Minutes)
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
                            if (UnityEngine.Random.Range(0, 11) == 0)
                                ChooseDungeonExteriorEncounter(dungeonType, climate, 0);

                            if (UnityEngine.Random.Range(0, 26) == 0)
                                ChooseDungeonExteriorEncounter(dungeonType, climate, 1);
                        }

                        //if (UnityEngine.Random.Range(0, 11) == 0) // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        if (1 == 1)
                        {
                            ChooseLocationExteriorEncounter(locationType, climate, 0);
                        }
                        else if (UnityEngine.Random.Range(0, 26) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {
                            ChooseLocationExteriorEncounter(locationType, climate, 1);
                        }
                    }
                    else
                    {
                        // In a location area at night

                        if (locationData.HasDungeon && (dungeonType != DFRegion.DungeonTypes.NoDungeon || regionData.MapTable[locationData.LocationIndex].LocationType != DFRegion.LocationTypes.TownCity))
                        {
                            if (UnityEngine.Random.Range(0, 11) == 0)
                                ChooseDungeonExteriorEncounter(dungeonType, climate, 2);

                            if (UnityEngine.Random.Range(0, 26) == 0)
                                ChooseDungeonExteriorEncounter(dungeonType, climate, 3);
                        }

                        if (UnityEngine.Random.Range(0, 26) == 0) // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        {
                            ChooseLocationExteriorEncounter(locationType, climate, 2);
                        }
                        else if (UnityEngine.Random.Range(0, 11) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {
                            ChooseLocationExteriorEncounter(locationType, climate, 3);
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
                            ChooseWildernessEncounter(climate, 0);
                        }
                        //else if (1 == 1)
                        else if (UnityEngine.Random.Range(0, 26) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {
                            ChooseWildernessEncounter(climate, 1);
                        }
                    }
                    else
                    {
                        // Wilderness at night

                        //if (UnityEngine.Random.Range(0, 26) == 0) // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        if (1 == 1)
                        {
                            ChooseWildernessEncounter(climate, 2);
                        }
                        //else if (1 == 1)
                        else if (UnityEngine.Random.Range(0, 11) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {
                            ChooseWildernessEncounter(climate, 3);
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
                    DFLocation locationData = GameManager.Instance.PlayerGPS.CurrentLocation;
                    DFRegion.DungeonTypes dungeonType = regionData.MapTable[locationData.LocationIndex].DungeonType;

                    if (UnityEngine.Random.Range(0, 26) == 0)
                        ChooseDungeonInteriorEncounter(dungeonType, climate, 0);

                    if (UnityEngine.Random.Range(0, 11) == 0)
                        ChooseDungeonInteriorEncounter(dungeonType, climate, 1);
                }
            }

            return false;
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

            if (ClimateSpawnExceptions(climate, (MobileTypes)mainEnemy))
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

            if (ClimateSpawnExceptions(climate, (MobileTypes)mainEnemy))
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

            if (ClimateSpawnExceptions(climate, (MobileTypes)mainEnemy))
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

            if (ClimateSpawnExceptions(climate, (MobileTypes)mainEnemy))
                return;

            if (eventType == 0)
                CreateGoodInteriorEvent((MobileTypes)mainEnemy);
            else
                CreateBadInteriorEvent((MobileTypes)mainEnemy);
        }

        public bool ClimateSpawnExceptions(MapsFile.Climates climate, MobileTypes enemy)
        {
            switch (climate)
            {
                case MapsFile.Climates.Ocean: if (CheckArrayForValue((int)enemy, new int[] { 0, 2, 4, 5, 6, 20, 26, 35 })) { return true; } else { return false; }
                case MapsFile.Climates.Desert: if (CheckArrayForValue((int)enemy, new int[] { 4, 6, 10, 25, 38 })) { return true; } else { return false; }
                case MapsFile.Climates.Desert2: if (CheckArrayForValue((int)enemy, new int[] { 4, 6, 10, 25, 38 })) { return true; } else { return false; }
                case MapsFile.Climates.Mountain: if (CheckArrayForValue((int)enemy, new int[] { 2, 8, 20 })) { return true; } else { return false; }
                case MapsFile.Climates.Rainforest: if (CheckArrayForValue((int)enemy, new int[] { 4 })) { return true; } else { return false; }
                case MapsFile.Climates.Swamp: if (CheckArrayForValue((int)enemy, new int[] { 4, 5, 20, 26, 35 })) { return true; } else { return false; }
                case MapsFile.Climates.Subtropical: if (CheckArrayForValue((int)enemy, new int[] { 25, 38 })) { return true; } else { return false; }
                case MapsFile.Climates.MountainWoods: if (CheckArrayForValue((int)enemy, new int[] { 8, 20 })) { return true; } else { return false; }
                case MapsFile.Climates.Woodlands: if (CheckArrayForValue((int)enemy, new int[] { 20 })) { return true; } else { return false; }
                case MapsFile.Climates.HauntedWoodlands: if (CheckArrayForValue((int)enemy, new int[] { 20 })) { return true; } else { return false; }
                default: return false;
            }
        }

        public int[] ClimateSpawnExceptions(MapsFile.Climates climate, List<int> checkList)
        {
            List<int> resultEnemies = checkList;
            int[] exceptions;

            switch (climate)
            {
                case MapsFile.Climates.Ocean: exceptions = new int[] { 0, 2, 4, 5, 6, 20, 26, 35 }; break;
                case MapsFile.Climates.Desert: exceptions = new int[] { 4, 6, 10, 25, 38 }; break;
                case MapsFile.Climates.Desert2: exceptions = new int[] { 4, 6, 10, 25, 38 }; break;
                case MapsFile.Climates.Mountain: exceptions = new int[] { 2, 8, 20 }; break;
                case MapsFile.Climates.Rainforest: exceptions = new int[] { 4 }; break;
                case MapsFile.Climates.Swamp: exceptions = new int[] { 4, 5, 20, 26, 35 }; break;
                case MapsFile.Climates.Subtropical: exceptions = new int[] { 25, 38 }; break;
                case MapsFile.Climates.MountainWoods: exceptions = new int[] { 8, 20 }; break;
                case MapsFile.Climates.Woodlands: exceptions = new int[] { 20 }; break;
                case MapsFile.Climates.HauntedWoodlands: exceptions = new int[] { 20 }; break;
                default: exceptions = new int[] {}; break;
            }

            for (int i = 0; i < checkList.Count; i++)
            {
                if (CheckArrayForValue(checkList[i], exceptions))
                {
                    resultEnemies.RemoveAt(i);
                }
            }

            return resultEnemies.ToArray();
        }

        public void CreateGoodDayEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(0, 1, 2, 3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.
            GameObject mobile = null;

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 32, 33, 128, 129, 130, 131, 132, 134, 137, 140, 141, 142, 143, 144, 145 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25, 26, 27, 29, 31, 34, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
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
            else if (eventScale == 2) // Work on adding the "large group" encounters framework, which is likely just going to be the small group but with more for-loop cycles. Also transform change for small groups, don't forget that.
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
            else if (eventScale == 3)
                return;
        }

        public void CreateBadDayEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(0, 1, 2, 3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.
            GameObject mobile = null;

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 128, 129, 130, 131, 132, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 31, 32, 33, 34, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
            }
            else if (eventScale == 1)
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
            else if (eventScale == 2)
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
            else if (eventScale == 3)
                return;
        }

        public void CreateGoodNightEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(0, 1, 2, 3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.
            GameObject mobile = null;

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 28, 30, 32, 33, 133, 134, 135, 136, 138, 139, 142, 143 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 9, 10, 14, 15, 17, 18, 19, 22, 23, 25, 26, 27, 29, 31, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
            }
            else if (eventScale == 1)
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
            else if (eventScale == 2)
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
            else if (eventScale == 3)
                return;
        }

        public void CreateBadNightEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(0, 1, 2, 3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.
            GameObject mobile = null;

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 133, 134, 135, 136, 138, 139, 142, 143 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
                if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 6, 9, 10, 14, 15, 17, 18, 19, 20, 22, 23, 25, 26, 27, 28, 29, 30, 31, 32, 33, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
            }
            else if (eventScale == 1)
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
            else if (eventScale == 2)
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
            else if (eventScale == 3)
                return;
        }

        public void CreateGoodInteriorEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(0, 1, 2, 3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.
            GameObject mobile = null;

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 28, 30, 32, 33, 128, 129, 130, 131, 132, 134, 137, 140, 141, 142, 143, 144, 145 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, true, true);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 1, 2, 7, 8, 10, 12, 13, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25, 26, 27, 29, 31, 34, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Friendly", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Friendly", enemyType.ToString(), enemyType, MobileReactions.Passive, true, false, false);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
            }
            else if (eventScale == 1)
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
            else if (eventScale == 2)
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
            else if (eventScale == 3)
                return;
        }

        public void CreateBadInteriorEvent(MobileTypes enemyType)
        {
            int eventScale = PickOneOf(0, 1, 2, 3); // 0 = Lone Enemy, 1 = Small group of enemies, 2 = Large group of enemies, 3 = Special.
            GameObject mobile = null;

            if (eventScale == 0)
            {
                if (CheckArrayForValue((int)enemyType, new int[] { 128, 129, 130, 131, 132, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
                else if (CheckArrayForValue((int)enemyType, new int[] { 0, 1, 2, 3, 6, 9, 10, 14, 15, 17, 18, 19, 20, 22, 23, 25, 26, 27, 28, 29, 30, 31, 32, 33, 35, 36, 37, 38 }))
                {
                    LoneEncounterTextInitiate("Lone_Hostile", enemyType.ToString(), (int)enemyType);
                    mobile = CreateSingleEnemy("Lone_Hostile", enemyType.ToString(), enemyType, MobileReactions.Hostile, false, false, false);
                    mobile.transform.LookAt(mobile.transform.position + (mobile.transform.position - player.transform.position));
                    mobile.SetActive(true);
                }
            }
            else if (eventScale == 1)
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
            else if (eventScale == 2)
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
            else if (eventScale == 3)
                return;
        }

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
            if (tokens[0].text == "") { return; } // Basically there for the "AggroText" that is shown when an enemy becomes hostile, but for those that should have none but will still pass this method.

            DaggerfallMessageBox textBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            textBox.SetTextTokens(tokens);
            textBox.ClickAnywhereToClose = true;
            textBox.Show();
        }

        public GameObject CreateSingleEnemy(string eventName, string enemyName, MobileTypes enemyType = MobileTypes.Acrobat, MobileReactions enemyReact = MobileReactions.Hostile, bool hasGreet = false, bool hasAdd = false, bool hasAggro = false)
        {
            GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, enemyType, 1, enemyReact);
            mobile[0].AddComponent<BRECustomObject>();
            BRECustomObject bRECustomObject = mobile[0].GetComponent<BRECustomObject>();

            if (eventName != "") // Basically for spawning single non-verbal enemies sort of things, even if the event does have a proper name for the initiation part.
            {
                if (hasGreet) { bRECustomObject.GreetingText = BREText.IndividualNPCTextFinder(eventName, enemyName, "Greet"); bRECustomObject.HasGreeting = true; }
                if (hasAdd) { bRECustomObject.AdditionalText = BREText.IndividualNPCTextFinder(eventName, enemyName, "Add"); bRECustomObject.HasMoreText = true; }
                if (hasAggro) { bRECustomObject.AggroText = BREText.IndividualNPCTextFinder(eventName, enemyName, "Aggro"); bRECustomObject.HasAggroText = true; }
            }
            
            return mobile[0];
        }

        public void LoneEncounterTextInitiate(string eventName, string enemyName, int enemyID)
        {
            TextFile.Token[] tokens = null;
            DaggerfallMessageBox initialEventPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);

            ModManager.Instance.SendModMessage("TravelOptions", "pauseTravel");
            tokens = BREText.LoneEncounterTextFinder(eventName, enemyName, enemyID);

            PopRegularText(tokens); // Later on in development, likely make many of these "safe" encounters skippable with a yes/no prompt.
        }

        public void CreateSmallEnemyGroup(string eventName, string enemyName, MobileTypes enemyType = MobileTypes.Acrobat, MobileReactions enemyReact = MobileReactions.Hostile, bool hasGreet = false, bool hasAdd = false, bool hasAggro = false)
        {
            ulong alliedID = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToSeconds();
            int size = UnityEngine.Random.Range(3, 5);
            GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, enemyType, 1, enemyReact);
            DaggerfallEntityBehaviour behaviour = mobile[0].GetComponent<DaggerfallEntityBehaviour>();
            EnemyEntity entity = behaviour.Entity as EnemyEntity;
            MobileTeams team = entity.Team;
            mobile[0].AddComponent<BRECustomObject>();
            BRECustomObject bRECustomObject = mobile[0].GetComponent<BRECustomObject>();
            bRECustomObject.LinkedAlliesID = alliedID; // Guess try and work on the transform part next for the groups of enemies. Then probably actually test this and see if everything is working at all. 
             // Might have to add another part to the update for event spawns like "trying to position encounter" or something, then uses this time to find a valid position to place things. 
            for (int i = 0; i < size; i++) // Based on how many enemies plan to be spawning in a specific encounter.
            {
                if (i == 0) // For the leader specifically
                {
                    if (eventName != "")
                    {
                        if (hasGreet) { bRECustomObject.GreetingText = BREText.IndividualNPCTextFinder(eventName+"_Leader", enemyName, "Greet"); bRECustomObject.HasGreeting = true; }
                        if (hasAdd) { bRECustomObject.AdditionalText = BREText.IndividualNPCTextFinder(eventName + "_Leader", enemyName, "Add"); bRECustomObject.HasMoreText = true; }
                        if (hasAggro) { bRECustomObject.AggroText = BREText.IndividualNPCTextFinder(eventName + "_Leader", enemyName, "Aggro"); bRECustomObject.HasAggroText = true; }
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
                        if (hasGreet) { bRECustomObject.GreetingText = BREText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Greet"); bRECustomObject.HasGreeting = true; }
                        if (hasAdd) { bRECustomObject.AdditionalText = BREText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Add"); bRECustomObject.HasMoreText = true; }
                        if (hasAggro) { bRECustomObject.AggroText = BREText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Aggro"); bRECustomObject.HasAggroText = true; }
                    }
                }

                mobile[0].transform.LookAt(mobile[0].transform.position + (mobile[0].transform.position - player.transform.position));
                mobile[0].SetActive(true);
            }
        }

        public void SmallGroupEncounterTextInitiate(string eventName, string enemyName, int enemyID)
        {
            TextFile.Token[] tokens = null;
            DaggerfallMessageBox initialEventPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);

            ModManager.Instance.SendModMessage("TravelOptions", "pauseTravel");
            tokens = BREText.SmallGroupEncounterTextFinder(eventName, enemyName, enemyID);

            PopRegularText(tokens); // Later on in development, likely make many of these "safe" encounters skippable with a yes/no prompt.
        }

        public void CreateLargeEnemyGroup(string eventName, string enemyName, MobileTypes enemyType = MobileTypes.Acrobat, MobileReactions enemyReact = MobileReactions.Hostile, bool hasGreet = false, bool hasAdd = false, bool hasAggro = false)
        {
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
                    if (eventName != "")
                    {
                        if (hasGreet) { bRECustomObject.GreetingText = BREText.IndividualNPCTextFinder(eventName + "_Leader", enemyName, "Greet"); bRECustomObject.HasGreeting = true; }
                        if (hasAdd) { bRECustomObject.AdditionalText = BREText.IndividualNPCTextFinder(eventName + "_Leader", enemyName, "Add"); bRECustomObject.HasMoreText = true; }
                        if (hasAggro) { bRECustomObject.AggroText = BREText.IndividualNPCTextFinder(eventName + "_Leader", enemyName, "Aggro"); bRECustomObject.HasAggroText = true; }
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
                        if (hasGreet) { bRECustomObject.GreetingText = BREText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Greet"); bRECustomObject.HasGreeting = true; }
                        if (hasAdd) { bRECustomObject.AdditionalText = BREText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Add"); bRECustomObject.HasMoreText = true; }
                        if (hasAggro) { bRECustomObject.AggroText = BREText.IndividualNPCTextFinder(eventName + "_Follower", enemyName, "Aggro"); bRECustomObject.HasAggroText = true; }
                    }
                }

                mobile[0].transform.LookAt(mobile[0].transform.position + (mobile[0].transform.position - player.transform.position));
                mobile[0].SetActive(true);
            }
        }

        public void LargeGroupEncounterTextInitiate(string eventName, string enemyName, int enemyID)
        {
            TextFile.Token[] tokens = null;
            DaggerfallMessageBox initialEventPopup = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);

            ModManager.Instance.SendModMessage("TravelOptions", "pauseTravel");
            tokens = BREText.LargeGroupEncounterTextFinder(eventName, enemyName, enemyID);

            PopRegularText(tokens); // Later on in development, likely make many of these "safe" encounters skippable with a yes/no prompt.
        }

        public MobileTypes ChooseEnemyFollowers(MobileTypes leader) // Meant for determining encounters where multiple enemies are spawned in a similar group.
        {
            int follower = 0;
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

        // Uses raycasts to find next spawn position just outside of player's field of view
        void PlaceFoeFreely(GameObject[] gameObjects, float minDistance = 5f, float maxDistance = 20f)
        {
            const float overlapSphereRadius = 0.65f;
            const float separationDistance = 1.25f;
            const float maxFloorDistance = 4f;

            // Must have received a valid array
            if (gameObjects == null || gameObjects.Length == 0)
                return;

            // Skip this foe if destroyed (e.g. player left building where pending)
            if (!gameObjects[pendingFoesSpawned])
            {
                pendingFoesSpawned++;
                return;
            }

            // Set parent if none specified already
            if (!gameObjects[pendingFoesSpawned].transform.parent)
                gameObjects[pendingFoesSpawned].transform.parent = GameObjectHelper.GetBestParent();

            // Get roation of spawn ray
            Quaternion rotation;
            if (LineOfSightCheck)
            {
                // Try to spawn outside of player's field of view
                float directionAngle = GameManager.Instance.MainCamera.fieldOfView;
                directionAngle += UnityEngine.Random.Range(0f, 4f);
                if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                    rotation = Quaternion.Euler(0, -directionAngle, 0);
                else
                    rotation = Quaternion.Euler(0, directionAngle, 0);
            }
            else
            {
                // Don't care about player's field of view (e.g. at rest)
                rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
            }

            // Get direction vector and create a new ray
            Vector3 angle = (rotation * Vector3.forward).normalized;
            Vector3 spawnDirection = GameManager.Instance.PlayerObject.transform.TransformDirection(angle).normalized;
            Ray ray = new Ray(GameManager.Instance.PlayerObject.transform.position, spawnDirection);

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
                currentPoint = GameManager.Instance.PlayerObject.transform.position + spawnDirection * UnityEngine.Random.Range(minDistance, maxDistance);
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
            pendingFoeGameObjects[pendingFoesSpawned].transform.position = testPoint;
            FinalizeFoe(pendingFoeGameObjects[pendingFoesSpawned]);
            gameObjects[pendingFoesSpawned].transform.LookAt(GameManager.Instance.PlayerObject.transform.position);

            // Increment count
            pendingFoesSpawned++;
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
