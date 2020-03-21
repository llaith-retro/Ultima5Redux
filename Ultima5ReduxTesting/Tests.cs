﻿using System;
using System.Diagnostics;
using NUnit.Framework;
using Ultima5Redux;
using Ultima5Redux3D;
using System.Collections.Generic;

namespace Ultima5ReduxTesting
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void AllSmallMapsLoadTest()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            foreach (SmallMapReferences.SingleMapReference smr in world.SmallMapRef.MapReferenceList)
            {
                world.State.TheVirtualMap.LoadSmallMap(
                    world.SmallMapRef.GetSingleMapByLocation(smr.MapLocation, smr.Floor), world.State.CharacterRecords,
                    false);
            }
            
            Assert.True(true);
        }
        
        [Test]
        public void LoadBritishBasement()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            Trace.Write("Starting ");
            //foreach (SmallMapReferences.SingleMapReference smr in world.SmallMapRef.MapReferenceList)
            {
                world.State.TheVirtualMap.LoadSmallMap(
                    world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Skara_Brae, 0), world.State.CharacterRecords,
                    false);
            }
            int i = (24 * (60 / 2));
            while (i > 0)
            {
                world.AdvanceTime(2);
                i--;
            }

            TestContext.Out.Write("Ending ");
            //System.Console.WriteLine("Ending ");//+smr.MapLocation + " on floor " + smr.Floor);
            
            Assert.True(true);
        }
        
        
        [Test]
        public void AllSmallMapsLoadWithOneDayTest()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            foreach (SmallMapReferences.SingleMapReference smr in world.SmallMapRef.MapReferenceList)
            {
                Debug.WriteLine("***** Loading "+smr.MapLocation + " on floor " + smr.Floor);
                world.State.TheVirtualMap.LoadSmallMap(
                    world.SmallMapRef.GetSingleMapByLocation(smr.MapLocation, smr.Floor), world.State.CharacterRecords,
                    false);

                int i = (24 * (60 / 2));
                while (i > 0)
                {
                    world.AdvanceTime(2);
                    i--;
                }
                Debug.WriteLine("***** Ending "+smr.MapLocation + " on floor " + smr.Floor);
            }
            
            Assert.True(true);
        }

        [Test]
        public void Test_TileOverrides()
        {
            TileOverrides to = new TileOverrides();
            
            World world = new World("C:\\games\\ultima_5_late\\britain");

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Lycaeum, 1), world.State.CharacterRecords,
                false);

            world.State.TheVirtualMap.GuessTile(new Point2D(14, 7));
        }


        [Test]
        public void Test_LoadOverworld()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
        }

        [Test]
        public void Test_LoadOverworldOverrideTile()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
            
            world.State.TheVirtualMap.GuessTile(new Point2D(81, 106));
        }

        [Test]
        public void Test_InventoryReferences()
        {
            InventoryReferences invRefs = new InventoryReferences();
            List<InventoryReference> invList = invRefs.GetInventoryReferenceList(InventoryReferences.InventoryReferenceType.Armament);
            foreach (InventoryReference invRef in invList)
            {
                string str = invRef.GetRichTextDescription();
                str = invRefs.HighlightKeywords(str);
            }
        }

        [Test]
        public void Test_PushPull_WontBudge()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Britain, 0), world.State.CharacterRecords,
                false);

            string pushAThing = world.PushAThing(new Point2D(5, 7), VirtualMap.Direction.Down, out bool bWasPushed);
            Assert.False(bWasPushed);
            Debug.WriteLine(pushAThing);
            
            pushAThing = world.PushAThing(new Point2D(22, 2), VirtualMap.Direction.Left, out bWasPushed);
            Assert.True(bWasPushed);
            Debug.WriteLine(pushAThing);
            
            pushAThing = world.PushAThing(new Point2D(2, 8), VirtualMap.Direction.Right, out bWasPushed);
            Assert.True(bWasPushed);
            Debug.WriteLine(pushAThing);
        }

        [Test]
        public void Test_FreeMoveAcrossWorld()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
            
            Point2D startLocation = world.State.TheVirtualMap.CurrentPosition.Copy();
            
            for (int i = 0; i < 256; i++)
            {
                world.TryToMove(VirtualMap.Direction.Up, false, true, out World.TryToMoveResult moveResult);
            }
            Point2D finalLocation = world.State.TheVirtualMap.CurrentPosition.Copy();
            
            Assert.True(finalLocation == startLocation);
        }
        
        [Test]
        public void Test_CheckAlLTilesForMoongates()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);

            Point2D startLocation = world.State.TheVirtualMap.CurrentPosition.Copy();
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    TileReference tileReference = world.State.TheVirtualMap.GetTileReference(x, y);
                }
            }

            Point2D finalLocation = world.State.TheVirtualMap.CurrentPosition.Copy();
            
            Assert.True(finalLocation == startLocation);
        }
        
        [Test]
        public void Test_MoonPhaseReference()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");
            
            MoonPhaseReferences moonPhaseReferences = new MoonPhaseReferences(world.DataOvlRef);

            for (byte nDay = 1; nDay <= 28; nDay++)
            {
                for (byte nHour = 0; nHour < 24; nHour++)
                {
                    TimeOfDay tod = new TimeOfDay(world.State.GetDataChunk(GameState.DataChunkName.CURRENT_YEAR),
                        world.State.GetDataChunk(GameState.DataChunkName.CURRENT_MONTH),
                        world.State.GetDataChunk(GameState.DataChunkName.CURRENT_DAY),
                        world.State.GetDataChunk(GameState.DataChunkName.CURRENT_HOUR),
                        world.State.GetDataChunk(GameState.DataChunkName.CURRENT_MINUTE));

                    tod.Day = nDay;
                    tod.Hour = nHour;

                    MoonPhaseReferences.MoonPhases moonPhase = moonPhaseReferences.GetMoonGateMoonPhase(tod);

                    float fMoonAngle = moonPhaseReferences.GetMoonAngle(tod);
                    Assert.True(fMoonAngle >= 0 && fMoonAngle < 360);
                }
            }
        }
    }
}