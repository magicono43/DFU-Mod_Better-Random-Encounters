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
using DaggerfallWorkshop.Utility;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Items;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.MagicAndEffects;

namespace BetterRandomEncounters
{
    public partial class BREWork
    {
        public static DaggerfallLoot tempShopItemHolder = null;
        public static EnemyEntity eventShopOwner = null;

        public static TextFile.Token[] SmallGroupEncounterTextFinder(string eventName, string enemyName, int enemyID)
        {
            switch (eventName)
            {
                case "Small_Group_Friendly":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "WIP");//GetRandomSmallGroupFriendlyEncounterText(enemyName, enemyID));
                case "Small_Group_Hostile":
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                    TextFile.Formatting.JustifyCenter,
                    "WIP");//GetRandomSmallGroupHostileEncounterText(enemyName, enemyID));
                default:
                    return DaggerfallUnity.Instance.TextProvider.CreateTokens(
                        TextFile.Formatting.JustifyCenter,
                        "Text Token Not Found");
            }
        }

        public void Traveling_Alchemist_Solo()
        {
            GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(playerObj.transform.position + playerObj.transform.forward * 2, (MobileTypes)PickOneOf(128, 131, 132, 133), 1, MobileReactions.Passive);
            mobile[0].AddComponent<BRECustomObject>();
            BRECustomObject bRECustomObject = mobile[0].GetComponent<BRECustomObject>();
            DaggerfallEntityBehaviour behaviour = mobile[0].GetComponent<DaggerfallEntityBehaviour>();
            EnemyEntity alchemist = behaviour.Entity as EnemyEntity;

            bRECustomObject.IsGangLeader = true;
            bRECustomObject.EventName = "Traveling_Alchemist_Solo";
            bRECustomObject.InventoryPopulated = true;
            int n = UnityEngine.Random.Range(3, 8);
            while (n-- >= 0)
                alchemist.Items.AddItem(ItemBuilder.CreateRandomPotion());


            pendingEventInitialTokens = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                "You spot a traveling alchemist. Perhaps they have some potions for sale?");

            bRECustomObject.GreetingText = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                "Hello, nice to see a friendly face! I am an alchemist indeed."); bRECustomObject.HasGreeting = true;

            bRECustomObject.AdditionalText = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                "Oh, you are interested in buying my potions? Alright, I should have a few spare to sell."); bRECustomObject.HasMoreText = true; bRECustomObject.HasAddChoiceBox = true;

            bRECustomObject.AggroText = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                "Take this you thief bastard!"); bRECustomObject.HasAggroText = true;

            pendingEventEnemies.Add(mobile[0]);
        }

        public static void Traveling_Alchemist_Solo_OnYesButton(DaggerfallMessageBox sender)
        {
            BRECustomObject alchObject = null;
            BRECustomObject[] sceneObjects = FindObjectsOfType<BRECustomObject>();
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                if (sceneObjects[i].IsGangLeader && sceneObjects[i].EventName == "Traveling_Alchemist_Solo")
                {
                    alchObject = sceneObjects[i];
                    DaggerfallEntityBehaviour behaviour = sceneObjects[i].GetComponent<DaggerfallEntityBehaviour>();
                    eventShopOwner = behaviour.Entity as EnemyEntity;
                }
            }

            // Generate a new loot-pile
            tempShopItemHolder = GameObjectHelper.CreateLootContainer(LootContainerTypes.CorpseMarker, InventoryContainerImages.Merchant, new Vector3(-10, -10, -10), alchObject.transform, DaggerfallLootDataTables.academicArchive, 0, DaggerfallUnity.NextUID, null, false);

            List<DaggerfallUnityItem> potionList = eventShopOwner.Items.SearchItems(ItemGroups.UselessItems1, 83);
            for (int i = 0; i < potionList.Count; i++)
            {
                if (eventShopOwner.Items.Contains(potionList[i]))
                {
                    tempShopItemHolder.Items.Transfer(potionList[i], eventShopOwner.Items);
                }
            }

            sender.CloseWindow();
            BRETradeWindow tradeWindow = new BRETradeWindow(DaggerfallUI.UIManager, null, BRETradeWindow.WindowModes.Buy, null);
            tradeWindow.MerchantItems = tempShopItemHolder.Items;
            tradeWindow.OnClose += Traveling_Alchemist_OnCloseTrade;
            DaggerfallUI.UIManager.PushWindow(tradeWindow);
        }

        public static void Traveling_Alchemist_OnCloseTrade()
        {
            if (!GameManager.IsGamePaused)
            {
                eventShopOwner.Items.TransferAll(tempShopItemHolder.Items);
                Destroy(tempShopItemHolder);
                tempShopItemHolder = null;
                eventShopOwner = null;
            }
        }

        public static void Traveling_Alchemist_CorpseBagTraps(DaggerfallLoot corpse, EnemyEntity alchemist, BRECustomObject bRECustomObject)
        {
            corpse.gameObject.AddComponent<BRELootPileObject>();
            BRELootPileObject bRELootPileObject = corpse.GetComponent<BRELootPileObject>();

            bRELootPileObject.EventName = bRECustomObject.EventName;
            bRELootPileObject.AttachedEnemy = alchemist;
            bRELootPileObject.HasChoices = true;
            bRELootPileObject.IsTrapped = true;

            if (alchemist.CareerIndex == (int)ClassCareers.Mage || alchemist.CareerIndex == (int)ClassCareers.Sorcerer) // Magic rune that throws some elemental spell or maybe even summon atronach
            {
                bRELootPileObject.ChoiceText = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                "As you go to unlatch the pouch, you notice what looks like a magical rune inscribed onto the clasp. You can only imagine it's a magical trap of some sort",
                "(likely as an attempt to discourage behavior such as this.) ",
                "Do you attempt disarming it to get to the contents of the pouch, at risk of triggering the trap?", "",
                "(Odds Determined By: Thaumaturgy, Destruction, Lockpicking, and of course, Luck)");
            }
            else if (alchemist.CareerIndex == (int)ClassCareers.Healer) // Cursed glyph that spawns a guardian wraith
            {
                bRELootPileObject.ChoiceText = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                "As you go to unlatch the pouch, you notice what looks like a strange glyph engraved onto the clasp. You can only imagine it's a magical trap of some sort",
                "(likely as an attempt to discourage behavior such as this.) ",
                "Do you attempt disarming it to get to the contents of the pouch, at risk of triggering the trap?", "",
                "(Odds Determined By: Mysticism, Restoration, Lockpicking, and of course, Luck)");
            }
            else if (alchemist.CareerIndex == (int)ClassCareers.Nightblade) // Poison Injector or Pressurized Disease Bomb
            {
                bRELootPileObject.ChoiceText = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                "As you go to unlatch the pouch, you notice an abnormal elastic tension when putting any pressure on the clasp. Perhaps a sprint-loaded trap of some kind",
                "(likely as an attempt to discourage behavior such as this.) ",
                "Do you attempt disarming it to get to the contents of the pouch, at risk of triggering the trap?", "",
                "(Odds Determined By: Lockpicking, Agility, Speed, and of course, Luck)");
            }
            else { bRELootPileObject.ChoiceText = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter, ""); }
        }

        public static void Traveling_Alchemist_Inventory_Choice_OnYesButton(DaggerfallMessageBox sender)
        {
            BRELootPileObject alchCorpseObject = null;
            BRELootPileObject[] sceneObjects = FindObjectsOfType<BRELootPileObject>();
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                if (sceneObjects[i].EventName == "Traveling_Alchemist_Solo")
                {
                    alchCorpseObject = sceneObjects[i];
                }
            }

            EnemyEntity alchemist = alchCorpseObject.AttachedEnemy;
            DaggerfallLoot loot = alchCorpseObject.GetComponent<DaggerfallLoot>(); // This should hopefully work to get the loot-pile reference?
            PlayerEntity player = GameManager.Instance.PlayerEntity;
            EntityEffectManager effectManager = player.EntityBehaviour.GetComponent<EntityEffectManager>();
            RaceTemplate raceTemplate = player.GetLiveRaceTemplate();
            int lockP = player.Skills.GetLiveSkillValue(DFCareer.Skills.Lockpicking);
            int myst = player.Skills.GetLiveSkillValue(DFCareer.Skills.Mysticism);
            int thau = player.Skills.GetLiveSkillValue(DFCareer.Skills.Thaumaturgy);
            int dest = player.Skills.GetLiveSkillValue(DFCareer.Skills.Destruction);
            int rest = player.Skills.GetLiveSkillValue(DFCareer.Skills.Restoration);
            int luck = player.Stats.LiveLuck - 50;
            int agil = player.Stats.LiveAgility - 50;
            int sped = player.Stats.LiveSpeed - 50;

            sender.CloseWindow();

            alchCorpseObject.DestroyedItemsText = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                        "You go over what remains of the pack, seems triggering the trap also destroyed anything of use that may have been in it, figures...");

            if (alchemist.CareerIndex == (int)ClassCareers.Mage || alchemist.CareerIndex == (int)ClassCareers.Sorcerer) // Magic rune that throws some elemental spell or maybe even summon atronach
            {
                if (Dice100.SuccessRoll(Mathf.Clamp(-35 + (thau) + (dest / 3) + (lockP / 3) + (luck / 3), 0, 90)))
                {
                    PopRegularText(DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter, // Tomorrow, Increase prices of potions from traveling alchemists more than currently.
                        "Either through skill or dumb luck (or both), you have dispelled the dangerous rune trap.", // Weird bug when multiple alchemist loot objects in one scene, buggy.
                        "You may now plunder the pack's contents freely."));
                }
                else
                {
                    DaggerfallConnect.Save.SpellRecord.SpellRecordData spell;
                    if (GameManager.Instance.EntityEffectBroker.GetClassicSpellRecord(31, out spell)) // ID 31 is apparently for the Lightning spell, will use this for now and see how it goes.
                    {
                        int i = 0;
                        while (i < 2) // Cast the spell multiple times for more destructive effect. (otherwise just cast God's Fire if this is not a good way to do it.)
                        {
                            // Create effect bundle settings from classic spell
                            EffectBundleSettings bundleSettings;
                            if (GameManager.Instance.EntityEffectBroker.ClassicSpellRecordDataToEffectBundleSettings(spell, BundleTypes.Spell, out bundleSettings))
                            {
                                // Spell is fired at player, at strength of player level, from triggering object
                                DaggerfallMissile missile = GameManager.Instance.PlayerEffectManager.InstantiateSpellMissile(bundleSettings.ElementType);
                                missile.Payload = new EntityEffectBundle(bundleSettings, alchemist.EntityBehaviour);
                                Vector3 customAimPosition = loot.transform.position;
                                customAimPosition.y += 40 * MeshReader.GlobalScale;
                                missile.CustomAimPosition = customAimPosition;
                                missile.CustomAimDirection = Vector3.Normalize(GameManager.Instance.PlayerObject.transform.position - loot.transform.position);
                            }
                            i++;
                        }
                    }

                    PopRegularText(DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                        "As you attempt to disarm the rune, suddenly, a massive jolt of electric energy is emitted from the magical rune.",
                        "The force is great enough to throw you backward onto your back, knocking the wind out of your chest."));
                    alchCorpseObject.ItemsDestroyed = true;
                    loot.Items.Clear(); // Destroys items within referenced loot-pile.
                }
            }
            else if (alchemist.CareerIndex == (int)ClassCareers.Healer) // Cursed glyph that spawns a guardian wraith
            {
                if (Dice100.SuccessRoll(Mathf.Clamp(-35 + (myst) + (rest / 3) + (lockP / 3) + (luck / 3), 0, 90)))
                {
                    PopRegularText(DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                        "Either through skill or dumb luck (or both), you have disarmed the glyph trap.",
                        "You may now plunder the pack's contents freely."));
                }
                else
                {
                    GameObject[] wraith = GameObjectHelper.CreateFoeGameObjects(alchCorpseObject.transform.position, MobileTypes.Wraith, 1);
                    wraith[0].SetActive(true);

                    PopRegularText(DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                        "As you attempt to disarm the glyph, suddenly, you are momentarily blinded by a flash of light and thrown back a few steps.",
                        "When your vision clears you realize an enraged apparition is flying straight for you!"));
                    alchCorpseObject.ItemsDestroyed = true;
                    loot.Items.Clear(); // Destroys items within referenced loot-pile.
                }
            }
            else if (alchemist.CareerIndex == (int)ClassCareers.Nightblade) // Poison Injector or Pressurized Disease Bomb
            {
                if (Dice100.SuccessRoll(Mathf.Clamp(-35 + (lockP) + (agil/2) + (sped/4) + (luck/4), 0, 90)))
                {
                    PopRegularText(DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                        "Either through skill or dumb luck (or both), you have disarmed the devious trap.",
                        "You may now plunder the pack's contents freely."));
                }
                else
                {
                    if (Dice100.SuccessRoll(50)) // Poison Injector
                    {
                        if (!((raceTemplate.ImmunityFlags & DFCareer.EffectFlags.Poison) == DFCareer.EffectFlags.Poison) &&
                            FormulaHelper.SavingThrow(DFCareer.Elements.DiseaseOrPoison, DFCareer.EffectFlags.Poison, player, 0) != 0) // If player not immune and fails resist roll, apply poison.
                        {
                            EntityEffectBundle bundle = effectManager.CreatePoison((Poisons)Random.Range(128, 136));
                            effectManager.AssignBundle(bundle, AssignBundleFlags.BypassSavingThrows);
                        }

                        PopRegularText(DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                            "As you fiddle with the mechanism, suddenly, you feel a spring engage and at the same time feel a sharp pain in your wrist.",
                            "You reflexivly retract back, and realize the pain was from a small spring loaded needle of some sort, and it looks",
                            "like it was coated in some strange liquid. That can't be good."));
                        player.DecreaseHealth(1);
                    }
                    else // Disease Bomb
                    {
                        Diseases[] diseaseList = { Diseases.WitchesPox, Diseases.Plague, Diseases.YellowFever, Diseases.StomachRot, Diseases.Consumption, Diseases.BrainFever, Diseases.SwampRot, Diseases.CalironsCurse, Diseases.Cholera, Diseases.Leprosy, Diseases.WoundRot, Diseases.RedDeath, Diseases.BloodRot, Diseases.TyphoidFever, Diseases.Dementia, Diseases.Chrondiasis, Diseases.WizardFever };
                        if (!(FormulaHelper.SavingThrow(DFCareer.Elements.DiseaseOrPoison, DFCareer.EffectFlags.Disease, player, 0) == 0))
                        {
                            int diseaseIndex = UnityEngine.Random.Range(0, diseaseList.Length);
                            Diseases diseaseType = diseaseList[diseaseIndex];
                            EntityEffectBundle bundle = GameManager.Instance.PlayerEffectManager.CreateDisease(diseaseType);
                            GameManager.Instance.PlayerEffectManager.AssignBundle(bundle, AssignBundleFlags.BypassSavingThrows);
                        }

                        PopRegularText(DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter,
                            "As you fiddle with the mechanism, suddenly, you feel a spring engage and hear a violent hissing and rush of invisible gas.",
                            "Before you can react you take a deep breath of the foul smelling substance. As you retract you feel slightly dizzy, but the",
                            "sensation quickly dissipates, but you have a hunch you may not feel so fine once whatever was in that gas-bomb starts to take effect."));
                    }
                    alchCorpseObject.ItemsDestroyed = true;
                    loot.Items.Clear(); // Destroys items within referenced loot-pile.
                }
            }
            alchCorpseObject.IsTrapped = false;
        }

        public static void Traveling_Alchemist_Inventory_Choice_OnNoButton(DaggerfallMessageBox sender)
        {

        }
    }
}