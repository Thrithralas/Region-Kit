﻿using System;
using System.Collections.Generic;
using UnityEngine;
using RWCustom;

using static RegionKit.POM.PlacedObjectsManager;

namespace RegionKit.MiscPO
{

    internal class PlacedWaterfallData : ManagedData
    {
        [FloatField("flow", 0f, 20f, 4f)]
        internal float flow;
        [IntegerField("width", 1, 10, 1)]
        internal int width;

        public PlacedWaterfallData(PlacedObject po) : base (po, new ManagedField[] { })
        {

        }
    }

    internal class WormgrassRectData : ManagedData
    {
        internal IntVector2 p2 => GetValue<IntVector2>("p2");

        public WormgrassRectData(PlacedObject po) : base(po, new ManagedField[] 
        {
            new IntVector2Field("p2", new IntVector2(3, 3), IntVector2Field.IntVectorReprType.rect)
        })
        {

        }
    }
}