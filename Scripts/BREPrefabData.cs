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
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace BetterRandomEncounters
{
    [System.Serializable]
    public class StageObject
    {
        public int type = 0; //0 == Mesh, 1 == Billboard, 2 == Editor
        public string name = "";
        public int objectID = 0;
        public string extraData = ""; // Data specific to a given object type and name
        public Vector3 pos = Vector3.zero;
        public Quaternion rot = Quaternion.Euler(0, 0, 0);
        public Vector3 scale = new Vector3(1, 1, 1);
    }

    /// <summary>
    /// Holds data locationPrefab
    /// </summary>
    [System.Serializable]
    public class StagePrefab
    {
        public int height = 8;
        public int width = 8;
        public List<StageObject> obj = new List<StageObject>();
    }

    public enum EditorMarkerTypes
    {
        Enter = 8,
        Start = 10,
        Quest = 11,
        RandomMonster = 15,
        Monster = 16,
        QuestItem = 18,
        RandomTreasure = 19,
        LadderBottom = 21,
        LadderTop = 22,
    }
}