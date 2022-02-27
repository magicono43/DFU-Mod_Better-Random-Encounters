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
using DaggerfallConnect.Save;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Items;
using FullSerializer;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Serialization;

namespace BetterRandomEncounters
{
    public class BRELootPileObject : MonoBehaviour
    {
        #region Fields

        string eventName = ""; // Use the OnEnemyDeath method from JewelryAdditions mod to attach this behavior to the newly spawned loot-pile based on the enemy that just died. Do Tomorrow.

        #endregion

        #region Properties

        public string EventName
        {
            get { return eventName; }
            set { eventName = value; }
        }

        #endregion

        #region Public Methods



        #endregion

        #region Private Methods

        private void Update()
        {
            if (SaveLoadManager.Instance.LoadInProgress)
                return;

            if (GameManager.IsGamePaused)
                return;


        }

        #endregion
    }
}