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

        public static TextFile.Token[] EncounterTextFinder(string eventName, string textType = "")
        {
            switch (eventName)
            {
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