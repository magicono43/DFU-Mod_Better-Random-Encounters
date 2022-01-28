// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2021 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

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

        bool hasGreeting = false;
        bool greetingShown = false;
        bool hasMoreText = false;

        bool wasClicked = false;
        bool wasAttacked = false;

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

        public bool WasClicked
        {
            get { return wasClicked; }
            set { wasClicked = value; }
        }

        public bool WasAttacked
        {
            get { return wasAttacked; }
            set { wasAttacked = value; }
        }

        #endregion

        #region Public Methods

        

        #endregion

        #region Private Methods



        #endregion
    }
}