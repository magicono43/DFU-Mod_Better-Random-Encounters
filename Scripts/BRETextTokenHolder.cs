// Project:         BetterRandomEncounters mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2022 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    1/22/2022, 8:45 PM
// Last Edit:		1/22/2022, 8:45 PM
// Version:			1.00
// Special Thanks:  Hazelnut, Ralzar
// Modifier:	

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop;
using DaggerfallConnect.Arena2;

namespace BetterRandomEncounters
{
    public class BREText
    {
        public static string GetHonoric()
        {
            int buildQual = GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData.quality;

            if (GameManager.Instance.PlayerEntity.Gender == Genders.Male)
            {
                if (buildQual <= 7)       // 01 - 07
                    return "%ra";
                else if (buildQual <= 17) // 08 - 17
                    return "sir";
                else                      // 18 - 20
                    return "m'lord";
            }
            else
            {
                if (buildQual <= 7)       // 01 - 07
                    return "%ra";
                else if (buildQual <= 17) // 08 - 17
                    return "ma'am";
                else                      // 18 - 20
                    return "madam";
            }
        }

        public static TextFile.Token[] PilgrimageText(int tokenID, int offerAmount = 0)
        {
            switch (tokenID)
            {
                case 1:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "My most sincere apologies " + GetHonoric() + ",",
                        "I have foolishly not stocked",
                        "myself with ample enough coin",
                        "to satisfy your needs. Give",
                        "me fifteen days at most and",
                        "we can continue our transaction.");
                case 2:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        offerAmount + " gold? A modest sum, but i'm certain I can make",
                        "a good profit with that amount, such that you could",
                        "be nothing but satisfied " + GetHonoric() + ", would you agree?");
                case 3:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Back so soon " + GetHonoric() + "? Well I can tell you that your",
                        "investment has been doing very well thus far. As I told you from",
                        "the start, i'm the best in this region, and that has not changed");
                case 21:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        offerAmount + " more you say? Very good " + GetHonoric() + ", i'll",
                        "be sure to take that into consideration when the next trade caravan",
                        "is scheduled to come by. Will that be all for today " + GetHonoric() + "?");
                default:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Text Token Not Found");
            }
        }
    }
}