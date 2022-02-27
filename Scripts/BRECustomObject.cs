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
    public class BRECustomObject : MonoBehaviour
    {
        #region Fields

        TextFile.Token[] greetingText;
        TextFile.Token[] additionalText;
        TextFile.Token[] aggroText = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.Nothing, "");

        bool hasGreeting = false;
        bool greetingShown = false;
        bool hasMoreText = false;
        bool hasAddChoiceBox = false;
        bool hasAggroText = false;
        bool aggroTextShown = false;
        ulong linkedAlliesID = 0;
        bool isGangLeader = false;
        string eventName = "";
        bool inventoryPopulated = false;
        bool readyToSpawn = false;

        #endregion

        #region Properties

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

        public bool HasAddChoiceBox
        {
            get { return hasAddChoiceBox; }
            set { hasAddChoiceBox = value; }
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

        public ulong LinkedAlliesID
        {
            get { return linkedAlliesID; }
            set { linkedAlliesID = value; }
        }

        public bool IsGangLeader
        {
            get { return isGangLeader; }
            set { isGangLeader = value; }
        }

        public string EventName
        {
            get { return eventName; }
            set { eventName = value; }
        }

        public bool InventoryPopulated
        {
            get { return inventoryPopulated; }
            set { inventoryPopulated = value; }
        }

        public bool ReadyToSpawn
        {
            get { return readyToSpawn; }
            set { readyToSpawn = value; }
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

            if (!aggroTextShown) // Removed the "HasAggroText" check before this, otherwise non-talking enemies would not all turn hostile when the player attack one of their other gang members.
            {
                EnemyMotor motor = GetComponent<EnemyMotor>();
                if (motor != null && motor.IsHostile)
                {
                    BREWork.PopRegularText(AggroText); // Try and figure out tomorrow why this section of code seems to not be getting ran? Or at least the debug down there is not apparently, weird.
                    AggroTextShown = true;
                    HasGreeting = false;
                    HasMoreText = false;

                    BRECustomObject[] gangMembers = FindObjectsOfType<BRECustomObject>(); // Attempt to turn all event "gang members" hostile once one becomes hostile to the player.
                    Debug.Log("There is this many gang members in the scene: " + gangMembers.Length.ToString());
                    if (gangMembers.Length > 2) // 2 as to factor in the mod object that also has this componenet attached to it in the scene, which I just learned is the case, lol.
                    {
                        for (int i = 0; i < gangMembers.Length; i++)
                        {
                            if (gangMembers[i].LinkedAlliesID == linkedAlliesID)
                            {
                                DaggerfallEntityBehaviour entityBehaviour = gangMembers[i].GetComponent<DaggerfallEntityBehaviour>();
                                if (entityBehaviour != null && (entityBehaviour.EntityType == EntityTypes.EnemyMonster || entityBehaviour.EntityType == EntityTypes.EnemyClass))
                                {
                                    EnemyMotor enemyMotor = entityBehaviour.GetComponent<EnemyMotor>();
                                    if (enemyMotor)
                                    {
                                        enemyMotor.IsHostile = true;
                                        gangMembers[i].AggroTextShown = true;
                                        gangMembers[i].HasGreeting = false;
                                        gangMembers[i].HasMoreText = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}