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
            GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)PickOneOf(128, 131, 132, 133), 1, MobileReactions.Passive);
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
            //DaggerfallTradeWindow tradeWindow = (DaggerfallTradeWindow)UIWindowFactory.GetInstanceWithArgs(UIWindowType.Trade, new object[] { DaggerfallUI.UIManager, null, DaggerfallTradeWindow.WindowModes.Buy, null });
            BRETradeWindow tradeWindow = (BRETradeWindow)UIWindowFactory.GetInstanceWithArgs(UIWindowType.Trade, new object[] { DaggerfallUI.UIManager, null, BRETradeWindow.WindowModes.Buy, null });
            tradeWindow.MerchantItems = tempShopItemHolder.Items;
            tradeWindow.OnClose += Traveling_Alchemist_OnCloseTrade;
            DaggerfallUI.UIManager.PushWindow(tradeWindow); // May want to create custom trade window here, but will have to see if necessary.
        }

        public static void Traveling_Alchemist_OnCloseTrade()
        {
            if (!GameManager.IsGamePaused)
            {
                eventShopOwner.Items.TransferAll(tempShopItemHolder.Items);
                Destroy(tempShopItemHolder); // Tomorrow, go through this sequence of processes one by one, then test in-game if all seems right, and finish other parts where needed.
                tempShopItemHolder = null;
                eventShopOwner = null;
            }
        }
    }
}