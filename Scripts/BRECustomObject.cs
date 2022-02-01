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
    /// <summary>
    /// Helper behaviour to pass information between GameObjects and Quest system.
    /// Used to trigger resource events in quest systems like ClickedNpc, InjuredFoe, KilledFoe, etc.
    /// </summary>
    public class BRECustomObject : MonoBehaviour
    {
        #region Fields

        ulong questUID;
        bool isFoeDead = false;

        [NonSerialized] DaggerfallEntityBehaviour enemyEntityBehaviour = null;



        TextFile.Token[] greetingText;
        TextFile.Token[] additionalText;
        TextFile.Token[] aggroText;

        bool hasGreeting = false;
        bool greetingShown = false;
        bool hasMoreText = false;
        bool hasAggroText = false;
        bool aggroTextShown = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets assigned Quest UID.
        /// </summary>
        public ulong QuestUID
        {
            get { return questUID; }
        }

        /// <summary>
        /// Flag stating if this Foe is dead.
        /// </summary>
        public bool IsFoeDead
        {
            get { return isFoeDead; }
        }

        /// <summary>
        /// Gets DaggerfallEntityBehaviour on enemy.
        /// Will be null if not an enemy quest resource.
        /// </summary>
        DaggerfallEntityBehaviour EnemyEntityBehaviour
        {
            get { return enemyEntityBehaviour; }
        }



        public TextFile.Token[] GreetingText
        {
            get { return greetingText; }
            set { greetingText = value; }
        }

        public TextFile.Token[] AdditionalText
        {
            get { return additionalText; }
            set { additionalText = value; }
        }

        public TextFile.Token[] AggroText
        {
            get { return aggroText; }
            set { aggroText = value; }
        }

        public bool HasGreeting
        {
            get { return hasGreeting; }
            set { hasGreeting = value; }
        }

        public bool GreetingShown
        {
            get { return greetingShown; }
            set { greetingShown = value; }
        }

        public bool HasMoreText
        {
            get { return hasMoreText; }
            set { hasMoreText = value; }
        }

        public bool HasAggroText
        {
            get { return hasAggroText; }
            set { hasAggroText = value; }
        }

        public bool AggroTextShown
        {
            get { return aggroTextShown; }
            set { aggroTextShown = value; }
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

            if (hasAggroText)
            {
                if (!aggroTextShown)
                {
                    EnemyMotor motor = GetComponent<EnemyMotor>();
                    if (motor != null && motor.IsHostile)
                    {
                        BREWork.PopRegularText(AggroText);
                        aggroTextShown = true;
                        hasGreeting = false;
                        HasMoreText = false;
                    }
                }
            }
        }

        #endregion
    }
}