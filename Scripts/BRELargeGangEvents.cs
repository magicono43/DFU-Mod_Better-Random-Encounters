// Project:         BetterRandomEncounters mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2022 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    1/22/2022, 8:45 PM
// Last Edit:		1/22/2022, 8:45 PM
// Version:			1.00
// Special Thanks:  Hazelnut, Ralzar, Badluckburt, Kab the Bird Ranger, JohnDoom, Uncanny Valley
// Modifier:	

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop;
using DaggerfallConnect.Arena2;

namespace BetterRandomEncounters
{
    public class BRELargeGangEvent
    {
        public static TextFile.Token[] LargeGroupEncounterTextFinder(string eventName, string enemyName, int enemyID)
        {
            switch (eventName)
            {
                case "Large_Group_Friendly":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "WIP");//GetRandomSmallGroupFriendlyEncounterText(enemyName, enemyID));
                case "Large_Group_Hostile":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "WIP");//GetRandomSmallGroupHostileEncounterText(enemyName, enemyID));
                default:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Text Token Not Found");
            }
        }
    }
}