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

namespace DaggerfallWorkshop.Game.Entity
{
    public class BREWork : PlayerEntity
    {
        private bool gameStarted = false;

        #region Constructors

        public BREWork(DaggerfallEntityBehaviour entityBehaviour)
            : base(entityBehaviour)
        {
        }

        #endregion

        public override void Update(DaggerfallEntityBehaviour sender)
        {
            base.Update(sender); // Execute the original parent class update behavior first, then run this modded part afterward.

            if (!gameStarted && !GameManager.Instance.StateManager.GameInProgress)
                return;
            else if (!gameStarted)
                gameStarted = true;

            if (SaveLoadManager.Instance.LoadInProgress)
                return;

            if (CurrentHealth <= 0)
                return;

            uint gameMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
            if (gameMinutes < lastGameMinutes)
            {
                throw new Exception(string.Format("lastGameMinutes {0} greater than gameMinutes: {1}", lastGameMinutes, gameMinutes));
            }

            if (!PreventEnemySpawns)
            {
                for (uint l = 0; l < (gameMinutes - lastGameMinutes); ++l)
                {
                    // Catch up time and break if something spawns. Don't spawn encounters while player is on ship.
                    if (!GameManager.Instance.TransportManager.IsOnShip() &&
                        ModdedEnemySpawn(l + lastGameMinutes + 1))
                        break;

                    // Confirm regionData is available
                    if (regionData == null || regionData.Length == 0)
                        break;
                }
            }

            lastGameMinutes = gameMinutes;

            // Allow enemy spawns again if they have been disabled
            if (PreventEnemySpawns)
                PreventEnemySpawns = false;
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

                        if (UnityEngine.Random.Range(0, 36) == 0) // roll odds for a "good" encounter? Have "good" encounters have a higher chance during the day, and the opposite for night generally.
                        {

                        }
                        else if (UnityEngine.Random.Range(0, 36) == 0) // if good encounter fails, roll for a "bad" counter? If neither succeed than no encounter happens this time around?
                        {

                        }
                    }
                    else
                    {
                        // In a location area at night
                        if (FormulaHelper.RollRandomSpawn_LocationNight() == 0)
                        {
                            GameObjectHelper.CreateFoeSpawner(true, RandomEncounters.ChooseRandomEnemy(false), 1, minLocationDistance);
                            return true;
                        }
                    }
                }
                else
                {
                    if (timeOfDay >= 360 && timeOfDay <= 1080)
                    {
                        // Wilderness during day
                        if (FormulaHelper.RollRandomSpawn_WildernessDay() != 0)
                            return false;
                    }
                    else
                    {
                        // Wilderness at night
                        if (FormulaHelper.RollRandomSpawn_WildernessNight() != 0)
                            return false;
                    }

                    GameObjectHelper.CreateFoeSpawner(true, RandomEncounters.ChooseRandomEnemy(false), 1, minWildernessDistance);
                    return true;
                }
            }

            // Spawns when player is inside
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                // Spawns when player is inside a dungeon
                if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
                {
                    if (IsResting)
                    {
                        if (FormulaHelper.RollRandomSpawn_Dungeon() == 0)
                        {
                            // TODO: Not sure how enemy type is chosen here.
                            GameObjectHelper.CreateFoeSpawner(false, RandomEncounters.ChooseRandomEnemy(false), 1, minDungeonDistance);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
