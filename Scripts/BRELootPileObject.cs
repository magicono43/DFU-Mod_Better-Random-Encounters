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

        TextFile.Token[] firstOpenText;
        TextFile.Token[] moreOpenText;
        TextFile.Token[] choiceText;

        bool hasFirstOpenText = false;
        bool openTextShown = false;
        bool hasMoreOpenText = false;
        bool hasChoices = false;
        string eventName = "";
        bool isLocked = false;
        bool isTrapped = false;
        bool doesTrapReset = false;

        EnemyEntity attachedEnemy = null;

        #endregion

        #region Properties

        public TextFile.Token[] FirstOpenText
        {
            get { return firstOpenText; }
            set { firstOpenText = value; }
        }

        public TextFile.Token[] MoreOpenText
        {
            get { return moreOpenText; }
            set { moreOpenText = value; }
        }

        public TextFile.Token[] ChoiceText
        {
            get { return choiceText; }
            set { choiceText = value; }
        }

        public bool HasFirstOpenText
        {
            get { return hasFirstOpenText; }
            set { hasFirstOpenText = value; }
        }

        public bool OpenTextShown
        {
            get { return openTextShown; }
            set { openTextShown = value; }
        }

        public bool HasMoreOpenText
        {
            get { return hasMoreOpenText; }
            set { hasMoreOpenText = value; }
        }

        public bool HasChoices
        {
            get { return hasChoices; }
            set { hasChoices = value; }
        }

        public string EventName
        {
            get { return eventName; }
            set { eventName = value; }
        }

        public bool IsLocked
        {
            get { return isLocked; }
            set { isLocked = value; }
        }

        public bool IsTrapped
        {
            get { return isTrapped; }
            set { isTrapped = value; }
        }

        public bool DoesTrapReset
        {
            get { return doesTrapReset; }
            set { doesTrapReset = value; }
        }

        public EnemyEntity AttachedEnemy
        {
            get { return attachedEnemy; }
            set { attachedEnemy = value; }
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