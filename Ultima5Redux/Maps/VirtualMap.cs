﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.External;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;

// ReSharper disable IdentifierTypo

namespace Ultima5Redux.Maps
{
    public class VirtualMap
    {
        #region Private fields
        private NonPlayerCharacterReferences _npcRefs;
        private readonly GameState _state;
        /// <summary>
        /// Reference to towne/keep etc locations on the large map
        /// </summary>
        private LargeMapLocationReferences _largeMapLocationReferenceses;
        /// <summary>
        /// Non player characters on current map
        /// </summary>
        private NonPlayerCharacterReferences _nonPlayerCharacters;
        /// <summary>
        /// All the small maps
        /// </summary>
        private readonly SmallMaps _smallMaps;
        /// <summary>
        /// Both underworld and overworld maps
        /// </summary>
        private readonly Dictionary<LargeMap.Maps, LargeMap> _largeMaps = new Dictionary<LargeMap.Maps, LargeMap>(2);

        /// <summary>
        /// Exposed searched or loot items 
        /// </summary>
        private Queue<InventoryItem>[][] _exposedSearchItems;
        /// <summary>
        /// References to all tiles
        /// </summary>
        private readonly TileReferences _tileReferences;
        /// <summary>
        /// override map is responsible for overriding tiles that would otherwise be static
        /// </summary>
        private int[][] _overrideMap;
        /// <summary>
        /// Current position of player character (avatar)
        /// </summary>
        //private readonly Point2D _currentPosition = new Point2D(0, 0);
        /// <summary>
        /// Current time of day
        /// </summary>
        private readonly TimeOfDay _timeOfDay;

        /// <summary>
        /// Details of where the moongates are
        /// </summary>
        private readonly Moongates _moongates;

        private readonly InventoryReferences _inventoryReferences;

        /// <summary>
        /// All overriden tiles
        /// </summary>
        private TileOverrides _overridenTiles; 
        #endregion

        /// <summary>
        /// 4 way direction
        /// </summary>
        public enum Direction { Up, Down, Left, Right, None };
        internal enum LadderOrStairDirection { Up, Down };


        #region Public Properties

        /// <summary>
        /// Current position of player character (avatar)
        /// </summary>
        // public Point2D CurrentPosition
        // {
        //     get => _currentPosition;
        //     set
        //     {
        //         _currentPosition.X = value.X;
        //         _currentPosition.Y = value.Y;
        //     }
        // }

        // public Point2D CurrentPosition
        // {
        //     get => CurrentPosition.XY;
        //     set => CurrentPosition = new MapUnitPosition(value.X, value.Y, CurrentPosition.Floor);
        // }

        public MapUnitPosition CurrentPosition => TheMapUnits.CurrentAvatarPosition;

        //set => TheMapUnits.CurrentAvatarPosition = value;
        /// <summary>
        /// Number of total columns for current map
        /// </summary>
        public int NumberOfColumnTiles => _overrideMap[0].Length;

        /// <summary>
        /// Number of total rows for current map
        /// </summary>
        public int NumberOfRowTiles => _overrideMap.Length;

        /// <summary>
        /// The current small map (null if on large map)
        /// </summary>
        public SmallMap CurrentSmallMap { get; private set; }
        /// <summary>
        /// Current large map (null if on small map)
        /// </summary>
        public LargeMap CurrentLargeMap { get; private set; }
        /// <summary>
        /// The abstracted Map object for the current map 
        /// Returns large or small depending on what is active
        /// </summary>
        public Map CurrentMap => (IsLargeMap ? (Map)CurrentLargeMap : (Map)CurrentSmallMap);

        /// <summary>
        /// Detailed reference of current small map
        /// </summary>
        public SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; private set; }
        /// <summary>
        /// All small map references
        /// </summary>
        public SmallMapReferences SmallMapRefs { get; private set; }
        /// <summary>
        /// Are we currently on a large map?
        /// </summary>
        public bool IsLargeMap { get; private set; } = false;

        public bool IsBasement => !IsLargeMap && CurrentSingleMapReference.Floor == -1; 

        /// <summary>
        /// If we are on a large map - then are we on overworld or underworld
        /// </summary>
        public LargeMap.Maps LargeMapOverUnder { get; private set; } = (LargeMap.Maps)(-1);

        public MapUnits.MapUnits TheMapUnits { get; private set; }

        #endregion

        #region Constructor, Initializers and Loaders

        /// <summary>
        /// Construct the VirtualMap (requires initialization still)
        /// </summary>
        /// <param name="smallMapReferences"></param>
        /// <param name="smallMaps"></param>
        /// <param name="largeMapLocationReferenceses"></param>
        /// <param name="overworldMap"></param>
        /// <param name="underworldMap"></param>
        /// <param name="nonPlayerCharacters"></param>
        /// <param name="tileReferences"></param>
        /// <param name="state"></param>
        /// <param name="npcRefs"></param>
        /// <param name="timeOfDay"></param>
        /// <param name="moongates"></param>
        /// <param name="inventoryReferences"></param>
        /// <param name="playerCharacterRecords"></param>
        /// <param name="initialMap"></param>
        /// <param name="currentSmallMapReference"></param>
        public VirtualMap(SmallMapReferences smallMapReferences, SmallMaps smallMaps, LargeMapLocationReferences largeMapLocationReferenceses,
            LargeMap overworldMap, LargeMap underworldMap, NonPlayerCharacterReferences nonPlayerCharacters, TileReferences tileReferences,
            GameState state, NonPlayerCharacterReferences npcRefs, TimeOfDay timeOfDay, Moongates moongates, InventoryReferences inventoryReferences,
            PlayerCharacterRecords playerCharacterRecords, LargeMap.Maps initialMap, 
            SmallMapReferences.SingleMapReference currentSmallMapReference)
        {
            // let's make sure they are using the correct combination
            Debug.Assert((initialMap == LargeMap.Maps.Small &&
                          currentSmallMapReference.MapLocation != SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
                         || initialMap != LargeMap.Maps.Small);
            
            SmallMapRefs = smallMapReferences;
            
            _smallMaps = smallMaps;
            _nonPlayerCharacters = nonPlayerCharacters;
            _largeMapLocationReferenceses = largeMapLocationReferenceses;
            _tileReferences = tileReferences;
            _state = state;
            _npcRefs = npcRefs;
            _timeOfDay = timeOfDay;
            _moongates = moongates;
            _inventoryReferences = inventoryReferences;
            _overridenTiles = new TileOverrides();
            
            //this.characterStates = characterStates;
            _largeMaps.Add(LargeMap.Maps.Overworld, overworldMap);
            _largeMaps.Add(LargeMap.Maps.Underworld, underworldMap);

            SmallMapReferences.SingleMapReference.Location mapLocation = currentSmallMapReference?.MapLocation 
                                                                    ?? SmallMapReferences.SingleMapReference.Location.Britannia_Underworld;
            
            // load the characters for the very first time from disk
            // subsequent loads may not have all the data stored on disk and will need to recalculate
            TheMapUnits = new MapUnits.MapUnits(tileReferences, npcRefs,
               state.CharacterAnimationStatesDataChunk, state.OverworldOverlayDataChunks, 
               state.UnderworldOverlayDataChunks, state.CharacterStatesDataChunk,
               state.NonPlayerCharacterMovementLists, state.NonPlayerCharacterMovementOffsets,
               timeOfDay, playerCharacterRecords, initialMap, mapLocation);

            switch (initialMap)
            {
                case LargeMap.Maps.Small:
                    LoadSmallMap(currentSmallMapReference);
                    break;
                case LargeMap.Maps.Overworld:
                case LargeMap.Maps.Underworld:
                    LoadLargeMap(initialMap);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(initialMap), initialMap, null);
            }
        }

        /// <summary>
        /// Loads a small map based on the provided reference
        /// </summary>
        /// <param name="singleMapReference"></param>
        public void LoadSmallMap(SmallMapReferences.SingleMapReference singleMapReference)
        {
            CurrentSingleMapReference = singleMapReference;
            CurrentSmallMap = _smallMaps.GetSmallMap(singleMapReference.MapLocation, singleMapReference.Floor);
            
            _overrideMap = Utils.Init2DArray<int>(CurrentSmallMap.TheMap[0].Length, CurrentSmallMap.TheMap.Length);
            _exposedSearchItems = Utils.Init2DArray<Queue<InventoryItem>>(CurrentSmallMap.TheMap[0].Length, CurrentSmallMap.TheMap.Length);
            
            IsLargeMap = false;
            LargeMapOverUnder = (LargeMap.Maps)(-1);

            TheMapUnits.SetCurrentMapType(singleMapReference.MapLocation, LargeMap.Maps.Small);
        }
      
        /// <summary>
        /// Loads a large map -either overworld or underworld
        /// </summary>
        /// <param name="map"></param>
        public void LoadLargeMap(LargeMap.Maps map)
        {
            CurrentLargeMap = _largeMaps[map];
            int nFloor = map == LargeMap.Maps.Overworld ? 0 : -1;
            switch (map)
            {
                case LargeMap.Maps.Underworld:
                case LargeMap.Maps.Overworld:
                    CurrentSingleMapReference = CurrentLargeMap.CurrentSingleMapReference;
                    break;
                case LargeMap.Maps.Small:
                    throw new Ultima5ReduxException("You can't load a small large map!");
                default:
                    throw new ArgumentOutOfRangeException(nameof(map), map, null);
            }

            _overrideMap = Utils.Init2DArray<int>(CurrentLargeMap.TheMap[0].Length, CurrentLargeMap.TheMap.Length);
            _exposedSearchItems = Utils.Init2DArray<Queue<InventoryItem>>(CurrentLargeMap.TheMap[0].Length, CurrentLargeMap.TheMap.Length);
            
            IsLargeMap = true;
            LargeMapOverUnder = map;

            TheMapUnits.SetCurrentMapType(SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, map);
        }
        #endregion

        #region Tile references and character positioning

        public IEnumerable<InventoryItem> GetExposedInventoryItems(Point2D xy)
        {
            return _exposedSearchItems[xy.X][xy.Y];
        }

        /// <summary>
        /// Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="bIgnoreExposed"></param>
        /// <param name="bIgnoreMoongate"></param>
        /// <returns></returns>
        public TileReference GetTileReference(int x, int y, bool bIgnoreExposed = false, bool bIgnoreMoongate = false)
        {
            // we FIRST check if there is an exposed item to show - this takes precedence over an overriden tile
            if (!bIgnoreExposed)
            {
                if (_exposedSearchItems[x][y] != null)
                {
                    if (_exposedSearchItems[x][y].Count > 0)
                    {
                        // we get the top most exposed item and only show it
                        return _tileReferences.GetTileReference(_exposedSearchItems[x][y].Peek().SpriteNum);
                    }
                }
            }

            // if it's a large map and there should be a moongate and it's nighttime then it's a moongate!
            // bajh: March 22, 2020 - we are going to try to always include the Moongate, and let the game decide what it wants to do with it
            if (!bIgnoreMoongate && IsLargeMap && 
                    _moongates.IsMoonstoneBuried(new Point3D(x, y, LargeMapOverUnder == LargeMap.Maps.Overworld ? 0 : 0xFF)))
            {
                return _tileReferences.GetTileReferenceByName("Moongate");
            }
            
            // we check to see if our override map has something on top of it
            if (_overrideMap[x][y] != 0)
                return _tileReferences.GetTileReference(_overrideMap[x][y]);

            if (IsLargeMap)
            {
                return (_tileReferences.GetTileReference(CurrentLargeMap.TheMap[x][y]));
            }
            else
            {
                return (_tileReferences.GetTileReference(CurrentSmallMap.TheMap[x][y]));
            }
        }

        /// <summary>
        /// Gets a tile reference from the tile the avatar currently resides on
        /// </summary>
        /// <returns></returns>
        public TileReference GetTileReferenceOnCurrentTile()
        {
            return GetTileReference(CurrentPosition.XY);
        }

        /// <summary>
        /// Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public TileReference GetTileReference(Point2D xy)
        {
            return GetTileReference(xy.X, xy.Y);
        }

        internal int SearchAndExposeItems(Point2D xy)
        {
            // check for moonstones
            // moonstone check
            if (IsLargeMap && _moongates.IsMoonstoneBuried(xy, LargeMapOverUnder) && _timeOfDay.IsDayLight)
            {
                InventoryItem invItem =
                    _state.PlayerInventory.TheMoonstones.
                        Items[_moongates.GetMoonPhaseByPosition(xy, LargeMapOverUnder)];
                if (_exposedSearchItems[xy.X][xy.Y] == null)
                {
                    _exposedSearchItems[xy.X][xy.Y] = new Queue<InventoryItem>();
                }
                _exposedSearchItems[xy.X][xy.Y].Enqueue(invItem);
                
                return 1;
            }

            return 0;
        }

        internal bool IsAnyExposedItems(Point2D xy)
        {
            if (_exposedSearchItems[xy.X][xy.Y] == null) return false;
            return (_exposedSearchItems[xy.X][xy.Y].Count > 0);
        }
        
        internal InventoryItem DequeuExposedItem(Point2D xy)
        {
            if (IsAnyExposedItems(xy))
            {
                return _exposedSearchItems[xy.X][xy.Y].Dequeue();
            }
            throw new Ultima5ReduxException("Tried to deque an item at "+xy+" but there is no item on it");
        }
        
        /// <summary>
        /// Gets the Avatar's current position in 3D spaces
        /// </summary>
        /// <returns></returns>
        public Point3D GetCurrent3DPosition()
        {
            if (LargeMapOverUnder == LargeMap.Maps.Small)
            {
                return new Point3D(CurrentPosition.X, 
                    CurrentPosition.Y, CurrentSmallMap.MapFloor);                
            }

            return new Point3D(CurrentPosition.X, 
                CurrentPosition.Y,LargeMapOverUnder == LargeMap.Maps.Overworld ? 0 : 0xFF);
        }
        
        /// <summary>
        /// Sets an override for the current tile which will be favoured over the static map tile
        /// </summary>
        /// <param name="tileReference">the reference (sprite)</param>
        /// <param name="xy"></param>
        public void SetOverridingTileReferece(TileReference tileReference, Point2D xy)
        {
            SetOverridingTileReferece(tileReference, xy.X, xy.Y);
        }

        /// <summary>
        /// Sets an override for the current tile which will be favoured over the static map tile
        /// </summary>
        /// <param name="tileReference"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetOverridingTileReferece(TileReference tileReference, int x, int y)
        {
            _overrideMap[x][y] = tileReference.Index;
        }

        
        /// <summary>
        /// Gets the NPC you want to talk to in the given direction
        /// If you are in front of a table then you can talk over top of it too
        /// </summary>
        /// <param name="direction"></param>
        /// <returns>the NPC or null if non are found</returns>
        public NonPlayerCharacter GetNpcToTalkTo(MapUnitMovement.MovementCommandDirection direction)
        {
            Point2D adjustedPosition = MapUnitMovement.GetAdjustedPos(CurrentPosition.XY, direction, 1);
            
            NonPlayerCharacter npc = TheMapUnits.GetSpecificMapUnitByLocation<NonPlayerCharacter>
                (CurrentSingleMapReference.MapLocation, adjustedPosition, CurrentSingleMapReference.Floor);
            
            if (npc != null) return npc;

            if (!GetTileReference(adjustedPosition).IsTalkOverable)
                return null;
            
            Point2D adjustedPosition2Away = MapUnitMovement.GetAdjustedPos(CurrentPosition.XY, direction, 2);
            return TheMapUnits.GetSpecificMapUnitByLocation<NonPlayerCharacter>
                (CurrentSingleMapReference.MapLocation, adjustedPosition2Away, CurrentSingleMapReference.Floor);
        }
        
        
        /// <summary>
        /// If an NPC is on a tile, then it will get them
        /// assumes it's on the same floor
        /// </summary>
        /// <param name="xy"></param>
        /// <returns>the NPC or null if one does not exist</returns>
        public MapUnit GetMapUnitOnTile(Point2D xy)
        {
            //if (IsLargeMap) return null;
            SmallMapReferences.SingleMapReference.Location location = CurrentSingleMapReference.MapLocation;

            MapUnit mapUnit = TheMapUnits.GetMapUnitByLocation(location, xy, CurrentSingleMapReference.Floor);
            
            return mapUnit;
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        /// Is the particular tile eligible to be moved onto
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="bNoStaircases"></param>
        /// <returns>true if you can move onto the tile</returns>
        internal bool IsTileFreeToTravel(Point2D xy, bool bNoStaircases)
        {
            if (xy.X < 0 || xy.Y < 0) return false;

            bool bIsAvatarTile = CurrentPosition.XY == xy;
            bool bIsNpcTile = IsMapUnitOccupiedTile(xy);
            TileReference tileReference = GetTileReference(xy);
            // if we want to eliminate staircases as an option then we need to make sure it isn't a staircase
            // true indicates that it is walkable
            bool bStaircaseWalkable = bNoStaircases ? !_tileReferences.IsStaircase(tileReference.Index) : true; 
            bool bIsWalkable = tileReference.IsWalking_Passable && bStaircaseWalkable;

            // there is not an NPC on the tile, it is walkable and the Avatar is not currently occupying it
            return (!bIsNpcTile && bIsWalkable && !bIsAvatarTile);
        }

        /// <summary>
        /// Gets possible directions that are accessible from a particular point
        /// </summary>
        /// <param name="characterPosition">the curent position of the character</param>
        /// <param name="scheduledPosition">the place they are supposed to be</param>
        /// <param name="nMaxDistance">max distance they can travel from that position</param>
        /// <param name="bNoStaircases"></param>
        /// <returns></returns>
        private List<MapUnitMovement.MovementCommandDirection> GetPossibleDirectionsList(Point2D characterPosition, Point2D scheduledPosition, 
            int nMaxDistance, bool bNoStaircases)
        {
            List<MapUnitMovement.MovementCommandDirection> directionList = new List<MapUnitMovement.MovementCommandDirection>();

            // gets an adjusted position OR returns null if the position is not valid

            foreach (MapUnitMovement.MovementCommandDirection direction in Enum.GetValues(typeof(MapUnitMovement.MovementCommandDirection)))
            {
                // we may be asked to avoid including .None in the list
                if (direction == MapUnitMovement.MovementCommandDirection.None) continue;
                
                Point2D adjustedPos = GetPositionIfUserCanMove(direction, characterPosition, bNoStaircases, scheduledPosition, nMaxDistance);
                // if adjustedPos == null then the particular direction was not allowed for one reason or another
                if (adjustedPos != null) { directionList.Add(direction); }
            }

            return directionList;
        }

        private Point2D GetPositionIfUserCanMove(MapUnitMovement.MovementCommandDirection direction, Point2D characterPosition, bool bNoStaircases, Point2D scheduledPosition, int nMaxDistance)
        {
            Point2D adjustedPosition = MapUnitMovement.GetAdjustedPos(characterPosition, direction);

            // always include none
            if (direction == MapUnitMovement.MovementCommandDirection.None) return adjustedPosition;

            if (adjustedPosition.X < 0 || adjustedPosition.X >= CurrentMap.TheMap.Length || adjustedPosition.Y < 0 || adjustedPosition.Y >= CurrentMap.TheMap[0].Length) return null;

            // is the tile free to travel to? even if it is, is it within N tiles of the scheduled tile?
            if (IsTileFreeToTravel(adjustedPosition, bNoStaircases) && scheduledPosition.WithinN(adjustedPosition, nMaxDistance))
            {
                return adjustedPosition;
            }

            return null;
        }

        /// <summary>
        /// Gets a suitable random position when wandering 
        /// </summary>
        /// <param name="characterPosition">position of character</param>
        /// <param name="scheduledPosition">scheduled position of the character</param>
        /// <param name="nMaxDistance">max number of tiles the wander can be from the scheduled position</param>
        /// <param name="direction">OUT - the direction that the character should travel</param>
        /// <returns></returns>
        internal Point2D GetWanderCharacterPosition(Point2D characterPosition, Point2D scheduledPosition,
            int nMaxDistance, out MapUnitMovement.MovementCommandDirection direction)
        {
            Random ran = new Random();
            List<MapUnitMovement.MovementCommandDirection> possibleDirections =
                GetPossibleDirectionsList(characterPosition, scheduledPosition, nMaxDistance, true);

            // if no directions are returned then we tell them not to move
            if (possibleDirections.Count == 0)
            {
                direction = MapUnitMovement.MovementCommandDirection.None;
                
                return characterPosition.Copy();
            }

            direction = possibleDirections[ran.Next() % possibleDirections.Count];

            Point2D adjustedPosition = MapUnitMovement.GetAdjustedPos(characterPosition, direction);

            return adjustedPosition;
        }



        private void CalculateNextPath(MapUnit mapUnit, int nMapCurrentFloor)
        {
            Type mapUnitType = mapUnit.GetType();
            if (mapUnitType == typeof(NonPlayerCharacter))
            {
                CalculateNextPath((NonPlayerCharacter)mapUnit, nMapCurrentFloor);
            }
        }
        
        /// <summary>
        /// Gets a list of points for all stairs and ladders  
        /// </summary>
        /// <param name="ladderOrStairDirection">direction of all stairs and ladders</param>
        /// <returns></returns>
        private List<Point2D> GetListOfAllLaddersAndStairs(LadderOrStairDirection ladderOrStairDirection)
        {
            List<Point2D> laddersAndStairs = new List<Point2D>();

            // go through every single tile on the map looking for ladders and stairs
            for (int x = 0; x < SmallMap.XTILES; x++)
            {
                for (int y = 0; y < SmallMap.YTILES; y++)
                {
                    TileReference tileReference = GetTileReference(x, y);
                    if (ladderOrStairDirection == LadderOrStairDirection.Down)
                    {
                        // if this is a ladder or staircase and it's in the right direction, then add it to the list
                        if (_tileReferences.IsLadderDown(tileReference.Index) || IsStairsGoingDown(new Point2D(x, y)))
                        {
                            laddersAndStairs.Add(new Point2D(x, y));
                        }
                    }
                    else // otherwise we know you are going up
                    {   
                        
                        if (_tileReferences.IsLadderUp(tileReference.Index) || (_tileReferences.IsStaircase(tileReference.Index) && IsStairGoingUp(new Point2D(x, y))))
                        {
                            laddersAndStairs.Add(new Point2D(x, y));
                        }
                    }
                } // end y for
            } // end x for
            
            return laddersAndStairs;
        }

        /// <summary>
        /// Gets the shortest path between a list of 
        /// </summary>
        /// <param name="positionList">list of positions</param>
        /// <param name="destinedPosition">the destination position</param>
        /// <returns>an ordered directory list of paths based on the shortest path (straight line path)</returns>
        private SortedDictionary<double, Point2D> GetShortestPaths(List<Point2D> positionList, Point2D destinedPosition)
        {
            SortedDictionary<double, Point2D> sortedPoints = new SortedDictionary<double, Point2D>();

            // get the distances and add to the sorted dictionary
            foreach (Point2D xy in positionList)
            {
                double dDistance = destinedPosition.DistanceBetween(xy);
                // make them negative so they sort backwards
                
                // if the distance is the same then we just add a bit to make sure there is no conflict
                while (sortedPoints.ContainsKey(dDistance))
                {
                    dDistance += 0.0000001;
                }
                sortedPoints.Add(dDistance, xy);
            }

            return sortedPoints;
        }

        /// <summary>
        /// Gets the best possible stair or ladder location
        /// to go to the destinedPosition
        /// Ladder/Stair -> destinedPositon
        /// </summary>
        /// <param name="ladderOrStairDirection">go up or down a ladder/stair</param>
        /// <param name="destinedPosition">the position to go to</param>
        /// <returns></returns>
        internal List<Point2D> GetBestStairsAndLadderLocation(LadderOrStairDirection ladderOrStairDirection,
            Point2D destinedPosition)
        {
            // get all ladder and stairs locations based (only up or down ladders/stairs)
            List<Point2D> allLaddersAndStairList = GetListOfAllLaddersAndStairs(ladderOrStairDirection);

            // get an ordered dictionary of the shortest straight line paths
            SortedDictionary<double, Point2D> sortedPoints = GetShortestPaths(allLaddersAndStairList, destinedPosition);

            // ordered list of the best choice paths (only valid paths) 
            List<Point2D> bestChoiceList = new List<Point2D>(sortedPoints.Count);

            // to make it more familiar, we will transfer to an ordered list
            foreach (Point2D xy in sortedPoints.Values)
            {
                bool bPathBuilt = GetTotalMovesToLocation(destinedPosition, xy) > 0;
                // we first make sure that the path even exists before we add it to the list
                if (bPathBuilt) bestChoiceList.Add(xy);

                continue;
            }

            return bestChoiceList;
        }

        /// <summary>
        /// Gets the best possible stair or ladder locations from the current position to the given ladder/stair direction
        /// currentPosition -> best ladder/stair
        /// </summary>
        /// <param name="ladderOrStairDirection">which direction will we try to get to</param>
        /// <param name="destinedPosition">the position you are trying to get to</param>
        /// <param name="currentPosition">the current position of the character</param>
        /// <returns></returns>
        internal List<Point2D> getBestStairsAndLadderLocationBasedOnCurrentPosition(LadderOrStairDirection ladderOrStairDirection, Point2D destinedPosition, Point2D currentPosition)
        {
            // get all ladder and stairs locations based (only up or down ladders/stairs)
            List<Point2D> allLaddersAndStairList = GetListOfAllLaddersAndStairs(ladderOrStairDirection);

            // get an ordered dictionary of the shortest straight line paths
            SortedDictionary<double, Point2D> sortedPoints = GetShortestPaths(allLaddersAndStairList, destinedPosition);

            // ordered list of the best choice paths (only valid paths) 
            List<Point2D> bestChoiceList = new List<Point2D>(sortedPoints.Count);
            
            // to make it more familiar, we will transfer to an ordered list
            foreach (Point2D xy in sortedPoints.Values)
            {
                bool bPathBuilt = GetTotalMovesToLocation(currentPosition, xy) > 0;
                // we first make sure that the path even exists before we add it to the list
                if (bPathBuilt) bestChoiceList.Add(xy);
                
                continue;
            }

            return bestChoiceList;
        }

        /// <summary>
        /// Returns the total number of moves to the number of moves for the character to reach a point
        /// </summary>
        /// <param name="currentXy"></param>
        /// <param name="targetXy">where the character would move</param>
        /// <returns>the number of moves to the targetXy</returns>
        /// <remarks>This is expensive, and would be wonderful if we had a better way to get this info</remarks>
        internal int GetTotalMovesToLocation(Point2D currentXy, Point2D targetXy)
        {            
            Stack<Node> nodeStack = CurrentMap.AStar.FindPath(new System.Numerics.Vector2(currentXy.X, currentXy.Y),
            new System.Numerics.Vector2(targetXy.X, targetXy.Y));

            return nodeStack?.Count ?? 0;
        }
        
        /// <summary>
        /// Advances each of the NPCs by one movement each
        /// </summary>
        /// <returns></returns>
        internal bool MoveMapUnitsToNextMove()
        {
            // if not on small map - then no NPCs!
            //if (IsLargeMap) return false;

            // go through each of the NPCs on the map
            foreach (MapUnit mapUnit in TheMapUnits.CurrentMapUnits.Where(mapChar => mapChar.IsActive))
            {
                mapUnit.CompleteNextMove(this, _timeOfDay);
            }

            return true;
        }
        #endregion

        #region Public Actions Methods
        // Action methods are things that the Avatar may do that will affect things around him like
        // getting a torch changes the tile underneath, opening a door may set a timer that closes it again
        // in a few turns
        //public void PickUpThing(Point2D xy)
        //{
        //    // todo: will need to actually poccket the thing I picked up
        //    SetOverridingTileReferece(tileReferences.GetTileReferenceByName("BrickFloor"), xy);
        //}


        /// <summary>
        /// Use the stairs and change floors, loading a new map
        /// </summary>
        /// <param name="xy">the position of the stairs, ladder or trapdoor</param>
        /// <param name="bForceDown">force a downward stairs</param>
        public void UseStairs(Point2D xy, bool bForceDown = false)
        {
            bool bStairGoUp = IsStairGoingUp() && !bForceDown;
            CurrentPosition.XY = xy.Copy();
            LoadSmallMap(SmallMapRefs.GetSingleMapByLocation(CurrentSingleMapReference.MapLocation, 
                CurrentSmallMap.MapFloor + (bStairGoUp ? 1 : -1)));
        }

        #endregion

        #region Public Boolean Method
        /// <summary>
        /// Is there food on a table within 1 (4 way) tile
        /// Used for determining if eating animation should be used
        /// </summary>
        /// <param name="characterPos"></param>
        /// <returns>true if food is within a tile</returns>
        public bool IsFoodNearby(Point2D characterPos)
        {
            bool isFoodTable(int nSprite)
            {
                return (nSprite == _tileReferences.GetTileReferenceByName("TableFoodTop").Index
                    || nSprite == _tileReferences.GetTileReferenceByName("TableFoodBottom").Index
                    || nSprite == _tileReferences.GetTileReferenceByName("TableFoodBoth").Index);
            }

            //Todo: use TileReference lookups instead of hard coded values
            if (CurrentSingleMapReference == null) return false;
            // yuck, but if the food is up one tile or down one tile, then food is nearby
            bool bIsFoodNearby = (isFoodTable(GetTileReference(characterPos.X, characterPos.Y - 1).Index)
                                  || isFoodTable(GetTileReference(characterPos.X, characterPos.Y + 1).Index));
            return bIsFoodNearby;

        }

        /// <summary>
        /// Are the stairs at the given position going up?
        /// Be sure to check if they are stairs first
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsStairGoingUp(Point2D xy)
        {
            if (!_tileReferences.IsStaircase(GetTileReference(xy).Index)) return false;

            bool bStairGoUp = _smallMaps.DoStrairsGoUp(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor, xy);
            return bStairGoUp;
        }

        public bool IsAvatarSitting()
        {
            return (_tileReferences.IsChair(GetTileReferenceOnCurrentTile().Index));
        }
        

        /// <summary>
        /// Are the stairs at the given position going down?
        /// Be sure to check if they are stairs first
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsStairsGoingDown(Point2D xy)
        {
            if (!_tileReferences.IsStaircase(GetTileReference(xy).Index)) return false;
            bool bStairGoUp = _smallMaps.DoStairsGoDown(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor, xy);
            return bStairGoUp;
        }

        /// <summary>
        /// Are the stairs at the player characters current position going down?
        /// </summary>
        /// <returns></returns>
        public bool IsStairsGoingDown()
        {
            return IsStairsGoingDown(CurrentPosition.XY);
        }

        /// <summary>
        /// Are the stairs at the player characters current position going up?
        /// </summary>
        /// <returns></returns>
        public bool IsStairGoingUp()
        {
            return IsStairGoingUp(CurrentPosition.XY);
        }

        /// <summary>
        /// When orienting the stairs, which direction should they be drawn 
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public Direction GetStairsDirection(Point2D xy)
        {
            // we are making a BIG assumption at this time that a stair case ONLY ever has a single
            // entrance point, and solid walls on all other sides... hopefully this is true
            if (!GetTileReference(xy.X - 1, xy.Y).IsSolidSprite) return Direction.Left;
            if (!GetTileReference(xy.X + 1, xy.Y).IsSolidSprite) return Direction.Right;
            if (!GetTileReference(xy.X, xy.Y - 1).IsSolidSprite) return Direction.Up;
            if (!GetTileReference(xy.X, xy.Y + 1).IsSolidSprite) return Direction.Down;
            throw new Ultima5ReduxException("Can't get stair direction - something is amiss....");
        }

        /// <summary>
        /// Given the orientation of the stairs, it returns the correct sprite to display
        /// </summary>
        /// <param name="xy">position of stairs</param>
        /// <returns>stair sprite</returns>
        public int GetStairsSprite(Point2D xy)
        {
            bool bGoingUp = IsStairGoingUp(xy);//UltimaGlobal.IsStairGoingUp(voxelPos);
            VirtualMap.Direction direction = GetStairsDirection(xy);
            int nSpriteNum = -1;
            switch (direction)
            {
                case VirtualMap.Direction.Up:
                    nSpriteNum = bGoingUp ? _tileReferences.GetTileReferenceByName("StairsNorth").Index
                        : _tileReferences.GetTileReferenceByName("StairsSouth").Index;
                    break;
                case VirtualMap.Direction.Down:
                    nSpriteNum = bGoingUp ? _tileReferences.GetTileReferenceByName("StairsSouth").Index
                        : _tileReferences.GetTileReferenceByName("StairsNorth").Index;
                    break;
                case VirtualMap.Direction.Left:
                    nSpriteNum = bGoingUp ? _tileReferences.GetTileReferenceByName("StairsWest").Index
                        : _tileReferences.GetTileReferenceByName("StairsEast").Index;
                    break;
                case VirtualMap.Direction.Right:
                    nSpriteNum = bGoingUp ? _tileReferences.GetTileReferenceByName("StairsEast").Index
                        : _tileReferences.GetTileReferenceByName("StairsWest").Index;
                    break;
                case Direction.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return nSpriteNum;
        }

        /// <summary>
        /// Is the door at the specified coordinate horizontal?
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsHorizDoor(Point2D xy)
        {
            if ((GetTileReference(xy.X - 1, xy.Y).IsSolidSpriteButNotDoor
                || GetTileReference(xy.X + 1, xy.Y).IsSolidSpriteButNotDoor))
                return true;
            
            return false;
        }

        /// <summary>
        /// Is there an NPC on the tile specified?
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsMapUnitOccupiedTile(Point2D xy)
        {
            // this method isn't super efficient, may want to optimize in the future
            //if (IsLargeMap) return false;
            return (GetMapUnitOnTile(xy) != null);
        }

        public bool ContainsSearchableThings(Point2D xy)
        {
            // moonstone check
            if (IsLargeMap && _moongates.IsMoonstoneBuried(xy, LargeMapOverUnder))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Once you have confirmed that there is something searchable with ContainsSearchableThings, you
        /// will need to "stir them up", which means they become visible to the user on the map and
        /// made available for "getting" from the virtual map
        /// </summary>
        /// <param name="xy"></param>
        public void StirUpTileSearch(Point2D xy)
        {
            
        }
        

        public void SwapTiles(Point2D tile1Pos, Point2D tile2Pos)
        {
            TileReference tileRef1 = GetTileReference(tile1Pos);
            TileReference tileRef2 = GetTileReference(tile2Pos);
            
            SetOverridingTileReferece(tileRef1, tile2Pos);
            SetOverridingTileReferece(tileRef2, tile1Pos);
        }

        /// <summary>
        /// Attempts to guess the tile underneath a thing that is upright such as a fountain
        /// </summary>
        /// <param name="xy">position of the thing</param>
        /// <returns>tile (sprite) number</returns>
        public int GuessTile(Point2D xy)
        {
            Dictionary<int, int> tileCountDictionary = new Dictionary<int, int>();
            
            // we check our high level tile override
            // todo: technically this is only for 3D worlds, we should consider that
            // this method is much quicker because we only load the data once in the maps 
            if (!IsLargeMap && CurrentMap.IsXYOverride(xy))
            {
                //Debug.WriteLine("Wanted to guess a tile but it was overriden: "+new MapUnitPosition(xy.X,xy.Y,CurrentSingleMapReference.Floor));
                return CurrentMap.GetTileOverride(xy).SpriteNum;
            }
            else if (IsLargeMap && CurrentMap.IsXYOverride(xy))
            {
                return CurrentMap.GetTileOverride(xy).SpriteNum;
            }
            
            // if has exposed search then we evaluate and see if it is actually a normal tile underneath
            int nExposedCount = _exposedSearchItems[xy.X][xy.Y]?.Count ?? 0; 
            if (nExposedCount > 0)
            {
                // there are exposed items on this tile
                TileReference tileRef = GetTileReference(xy.X, xy.Y, bIgnoreExposed: true, bIgnoreMoongate: true);
                if (tileRef.FlatTileSubstitionIndex != -2)
                    return tileRef.Index;
            }

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    // if it is out of bounds then we skips them altogether
                    if (xy.X + i < 0 || xy.X + i >= NumberOfRowTiles || xy.Y + j < 0 || xy.Y + j >= NumberOfColumnTiles)
                        continue;
                    TileReference tileRef = GetTileReference(xy.X + i, xy.Y + j);
                    // only look at non-upright sprites
                    if (tileRef.IsUpright) continue;
                    
                    int nTile = tileRef.Index;
                    if (tileCountDictionary.ContainsKey(nTile)) { tileCountDictionary[nTile] += 1; }
                    else { tileCountDictionary.Add(nTile, 1); }
                }
            }

            int nMostTile = -1;
            int nMostTileTotal = -1;
            // go through each of the tiles we saw and record the tile with the most instances
            foreach (int nTile in tileCountDictionary.Keys)
            {
                int nTotal = tileCountDictionary[nTile];
                if (nMostTile == -1 || nTotal > nMostTileTotal) { nMostTile = nTile; nMostTileTotal = nTotal; }
            }

            // just in case we didn't find a match - just use grass for now
            return nMostTile == -1 ? 5 : nMostTile;
        }
        #endregion
    }
}