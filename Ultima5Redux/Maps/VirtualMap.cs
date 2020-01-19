﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class VirtualMap
    {
        #region Private fields
        private NonPlayerCharacterReferences npcRefs;
        private GameState state;
        /// <summary>
        /// Reference to towne/keep etc locations on the large map
        /// </summary>
        private LargeMapReference largeMapReferences;
        /// <summary>
        /// Non player characters on current map
        /// </summary>
        private NonPlayerCharacterReferences nonPlayerCharacters;
        /// <summary>
        /// All the small maps
        /// </summary>
        private SmallMaps smallMaps;
        /// <summary>
        /// Both underworld and overworld maps
        /// </summary>
        private Dictionary<LargeMap.Maps, LargeMap> largeMaps = new Dictionary<LargeMap.Maps, LargeMap>(2);
        /// <summary>
        /// References to all tiles
        /// </summary>
        private TileReferences tileReferences;
        /// <summary>
        /// override map is responsible for overriding tiles that would otherwise be static
        /// </summary>
        private int[][] overrideMap;
        /// <summary>
        /// Current position of player character (avatar)
        /// </summary>
        private Point2D _currentPosition = new Point2D(0, 0);
        private TimeOfDay timeOfDay;
        #endregion

        #region Public Properties 
        /// <summary>
        /// 4 way direction
        /// </summary>
        public enum Direction { Up, Down, Left, Right };

        /// <summary>
        /// Current position of player character (avatar)
        /// </summary>
        public Point2D CurrentPosition { 
            get
            {
                return _currentPosition;
            }
            set 
            {
                _currentPosition.X = value.X; 
                _currentPosition.Y = value.Y; 
            } 
        } 

        /// <summary>
        /// Number of total columns for current map
        /// </summary>
        public int NumberOfColumnTiles { get { return overrideMap[0].Length; } }
        /// <summary>
        /// Number of total rows for current map
        /// </summary>
        public int NumberOfRowTiles { get { return overrideMap.Length; } }
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
        public Map CurrentMap { get { return (IsLargeMap ? (Map)CurrentLargeMap : (Map)CurrentSmallMap); } }
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
        /// <summary>
        /// If we are on a large map - then are we on overworld or underworld
        /// </summary>
        public LargeMap.Maps LargeMapOverUnder { get; private set; } = (LargeMap.Maps)(-1);

        public MapCharacters TheMapCharacters { get; private set; }

        #endregion

        #region Constructor, Initializers and Loaders
        /// <summary>
        /// Construct the VirtualMap (requires initalization still)
        /// </summary>
        /// <param name="smallMapReferences"></param>
        /// <param name="smallMaps"></param>
        /// <param name="largeMapReferences"></param>
        /// <param name="overworldMap"></param>
        /// <param name="underworldMap"></param>
        /// <param name="nonPlayerCharacters"></param>
        /// <param name="tileReferences"></param>
        public VirtualMap(SmallMapReferences smallMapReferences, SmallMaps smallMaps, LargeMapReference largeMapReferences, 
            LargeMap overworldMap, LargeMap underworldMap, NonPlayerCharacterReferences nonPlayerCharacters, TileReferences tileReferences,
            GameState state, NonPlayerCharacterReferences npcRefs, TimeOfDay timeOfDay)
        {
            this.SmallMapRefs = smallMapReferences;
            this.smallMaps = smallMaps;
            this.nonPlayerCharacters = nonPlayerCharacters;
            this.largeMapReferences = largeMapReferences;
            this.tileReferences = tileReferences;
            this.state = state;
            this.npcRefs = npcRefs;
            this.timeOfDay = timeOfDay;
            //this.characterStates = characterStates;
            largeMaps.Add(LargeMap.Maps.Overworld, overworldMap);
            largeMaps.Add(LargeMap.Maps.Underworld, underworldMap);

            TheMapCharacters = new MapCharacters(tileReferences, npcRefs, 
               state.CharacterAnimationStatesDataChunk, state.OverworldOverlayDataChunks, state.UnderworldOverlayDataChunks, state.CharacterStatesDataChunk,
               state.NonPlayerCharacterMovementLists, state.NonPlayerCharacterMovementOffsets);
        }

        /// <summary>
        /// Loads a small map based on the provided reference
        /// </summary>
        /// <param name="singleMapReference"></param>
        public void LoadSmallMap(SmallMapReferences.SingleMapReference singleMapReference, PlayerCharacterRecords playerCharacterRecords, bool bLoadFromDisk)
        {
            CurrentSingleMapReference = singleMapReference;
            CurrentSmallMap = smallMaps.GetSmallMap(singleMapReference.MapLocation, singleMapReference.Floor);
            overrideMap = Utils.Init2DArray<int>(CurrentSmallMap.TheMap[0].Length, CurrentSmallMap.TheMap.Length);
            IsLargeMap = false;
            LargeMapOverUnder = (LargeMap.Maps)(-1);

            TheMapCharacters.SetCurrentMapType(singleMapReference, LargeMap.Maps.Small, timeOfDay, playerCharacterRecords, bLoadFromDisk);

            //TheMapCharacters = new MapCharacters(tileReferences, npcRefs, singleMapReference, LargeMap.Maps.Small,
            //    state.CharacterAnimationStatesDataChunk,state.OverworldOverlayDataChunks, state.UnderworldOverlayDataChunks, state.CharacterStatesDataChunk,
            //    state.NonPlayerCharacterMovementLists, state.NonPlayerCharacterMovementOffsets);
        }

        /// <summary>
        /// Loads a large map -either overworld or underworld
        /// </summary>
        /// <param name="map"></param>
        public void LoadLargeMap(LargeMap.Maps map)
        {
            int nFloor = map == LargeMap.Maps.Overworld ? 0 : -1;
            CurrentSingleMapReference = null;
            CurrentLargeMap = largeMaps[map];
            overrideMap = Utils.Init2DArray<int>(CurrentLargeMap.TheMap[0].Length, CurrentLargeMap.TheMap.Length);
            IsLargeMap = true;
            LargeMapOverUnder = map;

            TheMapCharacters.SetCurrentMapType(null, map, timeOfDay, null, true);

        }
        #endregion

        #region Tile references and character positioning
        /// <summary>
        /// Gets a tile reference from the given coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public TileReference GetTileReference(int x, int y)
        {
            // we check to see if our override map has something on top of it
            if (overrideMap[x][y] != 0)
                return tileReferences.GetTileReference(overrideMap[x][y]);

            if (IsLargeMap)
            {
                return (tileReferences.GetTileReference(CurrentLargeMap.TheMap[x][y]));
            }
            else
            {
                return (tileReferences.GetTileReference(CurrentSmallMap.TheMap[x][y]));
            }
        }

        /// <summary>
        /// Gets a tile reference from the tile the avatar currently resides on
        /// </summary>
        /// <returns></returns>
        public TileReference GetTileReferenceOnCurrentTile()
        {
            return GetTileReference(CurrentPosition);
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
            overrideMap[x][y] = tileReference.Index;
        }


        /// <summary>
        /// Moves the player character to the specified coordinate
        /// </summary>
        /// <param name="xy"></param>
        public void SetCharacterPosition(Point2D xy)
        {
            CurrentPosition = xy;
        }

        /// <summary>
        /// If an NPC is on a tile, then it will get them
        /// assumes it's on the same floor
        /// </summary>
        /// <param name="xy"></param>
        /// <returns>the NPC or null if one does not exist</returns>
        public MapCharacter GetNPCOnTile(Point2D xy)
        {
            if (IsLargeMap) return null;
            SmallMapReferences.SingleMapReference.Location location = CurrentSingleMapReference.MapLocation;

            MapCharacter mapCharacter = TheMapCharacters.GetMapCharacterByLocation(location, xy, CurrentSingleMapReference.Floor);

            return mapCharacter;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Is the particular tile eligable to be moved onto
        /// </summary>
        /// <param name="xy"></param>
        /// <returns>true if you can move onto the tile</returns>
        private bool IsTileFreeToTravel(Point2D xy)
        {
            if (xy.X < 0 || xy.Y < 0) return false;

            bool bIsNPCTile = IsNPCTile(xy);
            bool bIsWalkable = GetTileReference(xy).IsWalking_Passable;
            return (!bIsNPCTile && bIsWalkable);
        }

        /// <summary>
        /// Gets possible directions that are accessible from a particular point
        /// </summary>
        /// <param name="characterPosition">the curent position of the character</param>
        /// <param name="scheduledPosition">the place they are supposed to be</param>
        /// <param name="nMaxDistance">max distance they can travel from that position</param>
        /// <returns></returns>
        private List<NonPlayerCharacterMovement.MovementCommandDirection> GetPossibleDirectionsList(Point2D characterPosition, Point2D scheduledPosition, int nMaxDistance)
        {
            List<NonPlayerCharacterMovement.MovementCommandDirection> directionList = new List<NonPlayerCharacterMovement.MovementCommandDirection>();

            // gets an adjusted position OR returns null if the position is not valid
            Point2D getAdjustedPos(NonPlayerCharacterMovement.MovementCommandDirection direction)
            {
                Point2D adjustedPosition = NonPlayerCharacterMovement.GetAdjustedPos(characterPosition, direction);

                // always include none
                if (direction == NonPlayerCharacterMovement.MovementCommandDirection.None) return adjustedPosition;

                // is the tile free to travel to? even if it is, is it within N tiles of the scheduled tile?
                if (IsTileFreeToTravel(adjustedPosition) && scheduledPosition.WithinN(adjustedPosition, nMaxDistance))
                {
                    return adjustedPosition;
                }
                return null;
            }


            foreach (NonPlayerCharacterMovement.MovementCommandDirection direction in Enum.GetValues(typeof(NonPlayerCharacterMovement.MovementCommandDirection)))
            {
                Point2D adjustedPos = getAdjustedPos(direction);
                // if adjustedPos == null then the particular direction was not allowed for one reason or another
                if (adjustedPos != null) { directionList.Add(direction); }
            }

            return directionList;
        }

        private Point2D GetWanderCharacterPosition(Point2D characterPosition, Point2D scheduledPosition, int nMaxDistance, out NonPlayerCharacterMovement.MovementCommandDirection direction)
        {
            Random ran = new Random();
            List<NonPlayerCharacterMovement.MovementCommandDirection> possibleDirections = GetPossibleDirectionsList(characterPosition, scheduledPosition, nMaxDistance);
            
            // we can always go nowhere
            //possibleDirections.Add(NonPlayerCharacterMovement.MovementCommandDirection.None);
            
            direction = possibleDirections[ran.Next() % possibleDirections.Count];

            Point2D adjustedPosition = NonPlayerCharacterMovement.GetAdjustedPos(characterPosition, direction);

            return adjustedPosition;
        }

        /// <summary>
        /// Points a character in a random position within a certain number of tiles to their scheduled position
        /// </summary>
        /// <param name="mapCharacter"></param>
        /// <param name="nMaxDistance"></param>
        /// <returns>the direction they should move</returns>
        private void WanderWithinN(MapCharacter mapCharacter, int nMaxDistance)
        {
            Random ran = new Random();

            // 50% of the time we won't even try to move at all
            int nRan = ran.Next(2);
            if (nRan == 0) return;
            
            CharacterPosition characterPosition = mapCharacter.CurrentCharacterPosition;
            CharacterPosition scheduledPosition = mapCharacter.NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

            //List<NonPlayerCharacterMovement.MovementCommandDirection> possibleDirections = GetPossibleDirectionsList(characterPosition);

            // i could get the size dyanmically, but thats a waste of CPU cycles
            NonPlayerCharacterMovement.MovementCommandDirection direction;// = possibleDirections[ran.Next() % possibleDirections.Count];
            //(NonPlayerCharacterMovement.MovementCommandDirection)(ran.Next()%4);

            Point2D adjustedPosition = GetWanderCharacterPosition(characterPosition.XY, scheduledPosition.XY, nMaxDistance, out direction);

                //NonPlayerCharacterMovement.GetAdjustedPos(characterPosition.XY, direction);
            // if it takes us off the map, then we move on

            // check to see if the random direction is within the correct distance
            if (direction != NonPlayerCharacterMovement.MovementCommandDirection.None && !scheduledPosition.XY.WithinN(adjustedPosition, nMaxDistance))
            {
                throw new Exception("GetWanderCharacterPosition has told us to go outside of our expected maximum area");
            }
            // can we even travel onto the tile?
            if (!IsTileFreeToTravel(adjustedPosition))
            {
                if (direction != NonPlayerCharacterMovement.MovementCommandDirection.None)
                {
                    throw new Exception("Was sent to a tile, but it isn't in free in WanderWithinN");
                }
                // something else is on the tile, so we don't move
                return;
            }

            // add the single instruction to the queue
            mapCharacter.Movement.AddNewMovementInstruction(new NonPlayerCharacterMovement.MovementCommand(direction, 1));
        }

        /// <summary>
        /// calculates and stores new path for NPC
        /// Placed outside into the VirtualMap since it will need information from the active map, VMap and the MapCharacter itself
        /// </summary>
        private void CalculateNextPath(MapCharacter mapChar)
        {
            CharacterPosition npcXy = mapChar.NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay);

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            // the tile the NPC current resides on
            TileReference currentTileRef = GetTileReference(npcXy.XY);

            bool bNPCShouldKlimb = CheckNPCAndKlimb(currentTileRef);
            if (bNPCShouldKlimb)
            {
                // teleport them and return immediately
                return;
            }

            NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType aiType = mapChar.NPCRef.Schedule.GetCharacterAITypeByTime(timeOfDay);

            // is the character is in their prescribed location?
            if (mapChar.CurrentCharacterPosition == npcXy)
            {
                // test all the possibilities, special calculations for all of them
                switch (aiType)
                {
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.Fixed:
                        // do nothing, they are where they are supposed to be 
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.Wander:
                        // choose a tile within N tiles that is not blocked, and build a single path
                        WanderWithinN(mapChar, 2);
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.BigWander:
                        // choose a tile within N tiles that is not blocked, and build a single path
                        WanderWithinN(mapChar, 4);
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.ChildRunAway:
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.MerchantThing:
                        // don't think they move....?
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.ExtortOrAttackOrFollow:
                        // set location of Avatar as way point, but only set the first movement from the list if within N of Avatar
                        break;
                    default:
                        throw new Exception("An unexpected movement AI was encountered: " + aiType.ToString() + " for NPC: " + mapChar.NPCRef.Name);
                }
            }
            else // character not in correct position
            {
                switch (aiType)
                {
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.MerchantThing:
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.Fixed:
                        // move to the correct position
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.Wander:
                        // check to see if wihtin N, if not then plot course, otherwise wander
                        WanderWithinN(mapChar, 2);
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.BigWander:
                        // check to see if wihtin N, if not then plot course, otherwise wander
                        WanderWithinN(mapChar, 4);
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.ChildRunAway:
                        // if the avatar is close by then move away from him, otherwise return to original path, one move at a time
                        break;
                    case NonPlayerCharacterReference.NonPlayerCharacterSchedule.AIType.ExtortOrAttackOrFollow:
                        // set location of Avatar as way point, but only set the first movement from the list if within N of Avatar
                        break;
                    default:
                        throw new Exception("An unexpected movement AI was encountered: " + aiType.ToString() + " for NPC: " + mapChar.NPCRef.Name);
                }

                // build a path
            }
                // calculate the path to get them to exactly their prescribed position
            }

        /// <summary>
        /// Checks if an NPC is on a stair or ladder, and if it goes in the correct direction then it returns true indicating they can teleport
        /// </summary>
        /// <param name="currentTileRef">the tile they are currently on</param>
        /// <param name="bIsOnStairCaseOrLadder"></param>
        /// <returns></returns>
        private bool CheckNPCAndKlimb(TileReference currentTileRef)
        {
            // is player on a ladder or staircase going in the direction they intend to go?
            bool bIsOnStairCaseOrLadder = tileReferences.IsStaircase(currentTileRef.Index) || tileReferences.IsLadder(currentTileRef.Index);

            if (bIsOnStairCaseOrLadder)
            {
                // are they destined to go up or down it?
                if (tileReferences.IsStaircase(currentTileRef.Index))
                {
                    if (IsStairGoingUp())
                    {

                    }
                    else
                    {

                    }
                }
                else // it's a ladder
                {
                    if (tileReferences.IsLadderUp(currentTileRef.Index))
                    {

                    }
                    else
                    {

                    }
                }
            }
            return false;
        }

        internal bool MoveNPCs()
        {
            // if not on small map - then no NPCs!
            if (IsLargeMap) return false;

            // go through each of the NPCs on the map
            foreach (MapCharacter mapChar in TheMapCharacters.Characters)
            {
                if (!mapChar.IsActive) continue;

                // if there is no next available movement then we gotta recalculate and see if they should move
                if (!mapChar.Movement.IsNextCommandAvailable())
                {
                    CalculateNextPath(mapChar);
                }

                // this NPC has a command in the buffer, so let's execute!
                // it's possible that CalculateNextPath came up empty for a variety of reasons, and that's okay
                if (mapChar.Movement.IsNextCommandAvailable())
                {
                    // peek and see what we have before we pop it off
                    NonPlayerCharacterMovement.MovementCommandDirection direction = mapChar.Movement.GetNextMovementCommandDirection(true);
                    Point2D adjustedPos = NonPlayerCharacterMovement.GetAdjustedPos(mapChar.CurrentCharacterPosition.XY, direction);
                    // need to evaluate if I can even move to the next tile before actually popping out of the queue
                    bool bIsNPCOnSpace = IsNPCTile(adjustedPos);
                    TileReference adjustedTile = GetTileReference(adjustedPos);
                    if (GetTileReference(adjustedPos).IsNPCCapableSpace && !bIsNPCOnSpace)
                    {
                        // pop the direction from the queue
                        direction = mapChar.Movement.GetNextMovementCommandDirection(false);
                        mapChar.Move(adjustedPos, mapChar.CurrentCharacterPosition.Floor);
                    }
                }
            }

            return true;
        }
        #endregion

        #region Public Actions Methods
        // Action methods are things that the Avatar may do that will affect things around him like
        // getting a torch changes the tile underneath, openning a door may set a timer that closes it again
        // in a few turns
        //public void PickUpThing(Point2D xy)
        //{
        //    // todo: will need to actually poccket the thing I picked up
        //    SetOverridingTileReferece(tileReferences.GetTileReferenceByName("BrickFloor"), xy);
        //}


        public void UseStairs(Point2D xy)
        {
            bool bStairGoUp = IsStairGoingUp();

            LoadSmallMap(SmallMapRefs.GetSingleMapByLocation(CurrentSingleMapReference.MapLocation, CurrentSmallMap.MapFloor + (bStairGoUp ? 1 : -1)), state.CharacterRecords, false);
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
            //Todo: use TileReference lookups instead of hard coded values
            //if (currentSingleSmallMapReferences == null) return false;
            if (CurrentSingleMapReference == null) return false;
            // yuck, but if the food is up one tile or down one tile, then food is nearby
            bool bIsFoodNearby = (GetTileReference(characterPos.X,characterPos.Y - 1).Index >= 154 &&
                GetTileReference(characterPos.X, characterPos.Y - 1).Index <= 156) ||
                (GetTileReference(characterPos.X, characterPos.Y + 1).Index >= 154 &&
                GetTileReference(characterPos.X, characterPos.Y + 1).Index <= 156);
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
            bool bStairGoUp = smallMaps.DoStrairsGoUp(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor, xy);
            return bStairGoUp;
        }

        /// <summary>
        /// Are the stairs at the given position going down?
        /// Be sure to check if they are stairs first
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public bool IsStairsGoingDown(Point2D xy)
        {
            bool bStairGoUp = smallMaps.DoStairsGoDown(CurrentSmallMap.MapLocation, CurrentSmallMap.MapFloor, xy);
            return bStairGoUp;
        }

        /// <summary>
        /// Are the stairs at the player characters current position going down?
        /// </summary>
        /// <returns></returns>
        public bool IsStairGoingDown()
        {
            return IsStairsGoingDown(CurrentPosition);
        }

        /// <summary>
        /// Are the stairs at the player characters current position going up?
        /// </summary>
        /// <returns></returns>
        public bool IsStairGoingUp()
        {
            return IsStairGoingUp(CurrentPosition);
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
            throw new Exception("Can't get stair direction - something is amiss....");
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
                    nSpriteNum = bGoingUp ? tileReferences.GetTileReferenceByName("StairsNorth").Index
                        : tileReferences.GetTileReferenceByName("StairsSouth").Index;
                    break;
                case VirtualMap.Direction.Down:
                    nSpriteNum = bGoingUp ? tileReferences.GetTileReferenceByName("StairsSouth").Index
                        : tileReferences.GetTileReferenceByName("StairsNorth").Index;
                    break;
                case VirtualMap.Direction.Left:
                    nSpriteNum = bGoingUp ? tileReferences.GetTileReferenceByName("StairsWest").Index
                        : tileReferences.GetTileReferenceByName("StairsEast").Index;
                    break;
                case VirtualMap.Direction.Right:
                    nSpriteNum = bGoingUp ? tileReferences.GetTileReferenceByName("StairsEast").Index
                        : tileReferences.GetTileReferenceByName("StairsWest").Index;
                    break;
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
        public bool IsNPCTile(Point2D xy)
        {
            // this method isnt super efficient, may want to optimize in the future
            if (IsLargeMap) return false;
            return (GetNPCOnTile(xy) != null);
        }

        /// <summary>
        /// Attempts to guess the tile underneath a thing that is upright such as a fountain
        /// </summary>
        /// <param name="xy">position of the thing</param>
        /// <returns>tile (sprite) number</returns>
        public int GuessTile(Point2D xy)
        {
            Dictionary<int, int> tileCountDictionary = new Dictionary<int, int>();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    // if it is out of bounds then we skips them altogether
                    if (xy.X + i < 0 || xy.X + i >= NumberOfRowTiles || xy.Y + j < 0 || xy.Y + j >= NumberOfColumnTiles)
                        continue;
                    TileReference tileRef = GetTileReference(xy.X + i, xy.Y + j);
                    // only look at non-upright sprites
                    if (!tileRef.IsUpright)
                    {
                        int nTile = tileRef.Index;
                        if (tileCountDictionary.ContainsKey(nTile)) { tileCountDictionary[nTile] += 1; }
                        else { tileCountDictionary.Add(nTile, 1); }
                    }
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
            return nMostTile == -1?5:nMostTile;
        }
        #endregion

    }
}