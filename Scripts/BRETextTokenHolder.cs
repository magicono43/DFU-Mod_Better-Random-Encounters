// Project:         BetterRandomEncounters mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2022 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    1/22/2022, 8:45 PM
// Last Edit:		1/22/2022, 8:45 PM
// Version:			1.00
// Special Thanks:  Hazelnut, Ralzar, Badluckburt, Kab the Bird Ranger, JohnDoom
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

        public static TextFile.Token[] LoneEncounterTextFinder(string eventName, string enemyName, int enemyID)
        {
            switch (eventName)
            {
                case "Lone_Friendly":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    GetRandomLoneFriendlyEncounterText(enemyName, enemyID));
                case "Lone_Hostile":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    GetRandomLoneHostileEncounterText(enemyName, enemyID));
                default:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Text Token Not Found");
            }
        }

        public static string GetRandomLoneFriendlyEncounterText(string enemyName, int enemyID)
        {
            int choice = BREWork.PickOneOf(1, 2, 3);

            if (enemyID < 128) // None Class NPCs
            {
                switch (choice)
                {
                    case 1:
                        return "You happen upon a seemingly friendly " + enemyName + ".";
                    case 2:
                        return "You and the " + enemyName + " lock eyes for a moment, but they do not attack, perhaps they are friendly?";
                    case 3:
                        return "That " + enemyName + " does not immediately seem hostile, perhaps it's friendly?";
                    default:
                        return "";
                }
            }
            else // Class NPCs
            {
                switch (choice)
                {
                    case 1:
                        return "You happen upon a seemingly friendly " + enemyName + ".";
                    case 2:
                        return "You and the " + enemyName + " lock eyes for a moment, but they do not attack, perhaps they are friendly?";
                    case 3:
                        return "That " + enemyName + " does not immediately seem hostile, perhaps it's friendly?";
                    default:
                        return "";
                }
            }
        }

        public static string GetRandomLoneHostileEncounterText(string enemyName, int enemyID)
        {
            int choice = BREWork.PickOneOf(1, 2, 3);

            if (enemyID < 128) // None Class NPCs
            {
                switch (choice)
                {
                    case 1:
                        return "You happen upon a hostile " + enemyName + ".";
                    case 2:
                        return "You and the " + enemyName + " lock eyes for a moment, and they rush toward you.";
                    case 3:
                        return "That " + enemyName + " is coming right for you!";
                    default:
                        return "";
                }
            }
            else // Class NPCs
            {
                switch (choice)
                {
                    case 1:
                        return "You happen upon a hostile " + enemyName + ".";
                    case 2:
                        return "You and the " + enemyName + " lock eyes for a moment, and they rush toward you.";
                    case 3:
                        return "That " + enemyName + " is coming right for you!";
                    default:
                        return "";
                }
            }
        }

        public static TextFile.Token[] SmallGroupEncounterTextFinder(string eventName, string enemyName, int enemyID)
        {
            switch (eventName)
            {
                case "Small_Group_Friendly":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "");//GetRandomSmallGroupFriendlyEncounterText(enemyName, enemyID));
                case "Small_Group_Hostile":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "");//GetRandomSmallGroupHostileEncounterText(enemyName, enemyID));
                default:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Text Token Not Found");
            }
        }

        public static TextFile.Token[] LargeGroupEncounterTextFinder(string eventName, string enemyName, int enemyID)
        {
            switch (eventName)
            {
                case "Large_Group_Friendly":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "");//GetRandomSmallGroupFriendlyEncounterText(enemyName, enemyID));
                case "Large_Group_Hostile":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "");//GetRandomSmallGroupHostileEncounterText(enemyName, enemyID));
                default:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Text Token Not Found");
            }
        }

        public static TextFile.Token[] IndividualNPCTextFinder(string eventName, string enemyName, string textType = "")
        {
            switch (eventName)
            {
                case "Lone_Friendly":
                case "Lone_Hostile":
                case "Lone_Healer":
                case "Lone_Monk":
                case "Lone_Bard":
                case "Lone_Acrobat":
                case "Lone_Ranger":
                case "Lone_Knight":
                    if (textType == "Greet")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Greetings");
                    else if (textType == "Add")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Hello Again");
                    else if (textType == "Aggro")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Fuck you, Asshole.");
                    else
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "This Encounter Just Spawned");
                case "Lone_Alchemist":
                    if (textType == "Greet")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Greetings");
                    else if (textType == "Add")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Hello Again");
                    else if (textType == "Aggro")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Fuck you, Asshole.");
                    else
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "This Encounter Just Spawned");
                case "Lone_Trainee":
                    if (textType == "Greet")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Greetings");
                    else if (textType == "Add")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Hello Again");
                    else if (textType == "Aggro")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Fuck you, Asshole.");
                    else
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "This Encounter Just Spawned");
                case "Lone_Training_Mage":
                    if (textType == "Greet")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Greetings");
                    else if (textType == "Add")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Hello Again");
                    else if (textType == "Aggro")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Fuck you, Asshole.");
                    else
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "This Encounter Just Spawned");
                case "Lone_Beast":
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "This Encounter Just Spawned");
                case "Lone_Nature_Guard":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "This Encounter Just Spawned");
                case "Lone_Humanoid_Monster":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "This Encounter Just Spawned");
                case "Lone_Atronach":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "This Encounter Just Spawned");
                case "Lone_Nightblade":
                case "Lone_Burglar":
                case "Lone_Rogue":
                case "Lone_Thief":
                case "Lone_Assassin":
                    if (textType == "Greet")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Greetings");
                    else if (textType == "Add")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Hello Again");
                    else if (textType == "Aggro")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Fuck you, Asshole.");
                    else
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "This Encounter Just Spawned");
                case "Lone_Nice_Vampire":
                    if (textType == "Greet")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Greetings");
                    else if (textType == "Add")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Hello Again");
                    else if (textType == "Aggro")
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Fuck you, Asshole.");
                    else
                        return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "This Encounter Just Spawned");
                default:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Text Token Not Found");
            }
        }
    }
}