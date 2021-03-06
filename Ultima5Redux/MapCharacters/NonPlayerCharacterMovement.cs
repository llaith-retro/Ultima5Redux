﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapCharacters
{
    public partial class NonPlayerCharacterMovement
    {
        #region Constructors
        /// <summary>
        /// Construct a NonPlayerCharacterMovement 
        /// </summary>
        /// <param name="nDialogIndex">the index of an NPC</param>
        /// <param name="movementInstructionDataChunk">The full memory chunk of all movement instructions</param>
        /// <param name="movementOffsetDataChunk">the full memory chunk of the movement offsets</param>
        public NonPlayerCharacterMovement(int nDialogIndex, DataChunk movementInstructionDataChunk, DataChunk movementOffsetDataChunk)
        {
            // we totally ignore the first entry, since it's bad stuff
            if (nDialogIndex == 0) return;

            this._movementInstructionDataChunk = movementInstructionDataChunk;
            this._movementOffsetDataChunk = movementOffsetDataChunk;
            this._nDialogIndex = nDialogIndex;
            
            // todo: not a very efficient method of getting a UINT16 from the list -> it has to create a brand new list!
            _nOffset = movementOffsetDataChunk.GetChunkAsUint16List() [nDialogIndex];

            // if it has the value of 0xFFFF then it indicates there are currently no instructions
            if (_nOffset == 0xFFFF) return;

            // calculate the offset
            int nOffsetIndex = (nDialogIndex) * (MAX_COMMAND_LIST_ENTRIES * MAX_MOVEMENT_COMMAND_SIZE);

            // get a copy because the GetAsByteList is an expensive method call
            List<byte> rawData = movementInstructionDataChunk.GetAsByteList();

            // gets a smaller version of it - much easier to keep track of
            _loadedData = rawData.GetRange(nOffsetIndex, MAX_COMMAND_LIST_ENTRIES * 2);

            int nIndex = _nOffset;
            for (int i = 0; i < MAX_COMMAND_LIST_ENTRIES; i++)
            {
                byte nIterations = _loadedData[nIndex];
                MovementCommandDirection direction = (MovementCommandDirection)_loadedData[nIndex + 1];

                // if we have hit 0xFF then there is nothing else in the list and we can just return
                if (nIterations == 0xFF || nIterations == 0) return;

                if (!(direction == MovementCommandDirection.East || direction == MovementCommandDirection.West || direction == MovementCommandDirection.North
                    || direction == MovementCommandDirection.South)) { throw new Ultima5ReduxException("a bad direction was set: " + direction.ToString()); }


                // we have a proper movement instruction so let's add it to the queue
                MovementCommand movementCommand = new MovementCommand(direction, nIterations);
                //this.movementQueue.Enqueue(movementCommand);
                AddNewMovementInstruction(movementCommand);

                // we actually grab from the offset, but it is circular, so we need to mod it
                nIndex = (nIndex + 2) % (MAX_COMMAND_LIST_ENTRIES * 2);
            }
        }
        #endregion

        #region Static Methods

        internal static Point2D GetAdjustedPos(Point2D xy, VirtualMap.Direction direction)
        {
            return NonPlayerCharacterMovement.GetAdjustedPos(xy, GetDirection(direction));
        }

        private static MovementCommandDirection GetDirection(VirtualMap.Direction vmapDirection)
        {
            switch (vmapDirection)
            {
                case VirtualMap.Direction.Right:
                    return MovementCommandDirection.East;
                case VirtualMap.Direction.Up:
                    return MovementCommandDirection.North;
                case VirtualMap.Direction.Left:
                    return MovementCommandDirection.West;
                case VirtualMap.Direction.Down:
                    return MovementCommandDirection.South;
                case VirtualMap.Direction.None:
                default:
                    throw new Ultima5ReduxException("Cannot convert " + vmapDirection + " to MovementCommandDirection");
            }
        }

        /// <summary>
        /// Provides an adjust xy coordinate based on a given direction
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        internal static Point2D GetAdjustedPos(Point2D xy, NonPlayerCharacterMovement.MovementCommandDirection direction,
            int nSpaces = 1)
        {
            Point2D adjustedPos = new Point2D(xy.X, xy.Y);

            switch (direction)
            {
                case MovementCommandDirection.None:
                    // no movement
                    break;
                case MovementCommandDirection.East:
                    adjustedPos.X += nSpaces;
                    break;
                case MovementCommandDirection.North:
                    adjustedPos.Y -= nSpaces;
                    break;
                case MovementCommandDirection.West:
                    adjustedPos.X -= nSpaces;
                    break;
                case MovementCommandDirection.South:
                    adjustedPos.Y += nSpaces;
                    break;
            }
            return adjustedPos;
        }
        #endregion

        #region Private fields
        /// <summary>
        /// DataChunk of current map characters movement list
        /// </summary>
        private DataChunk _movementInstructionDataChunk;
        /// <summary>
        /// DataChunk of current map characters offset into movement list
        /// </summary>
        private DataChunk _movementOffsetDataChunk;
        /// <summary>
        /// Dialog index of the map character
        /// </summary>
        private int _nDialogIndex;
        /// <summary>
        /// The data that was loaded into the list initially (primarily for debug)
        /// </summary>
        private List<byte> _loadedData = new List<byte>();
        /// <summary>
        /// the offset into the movement list
        /// </summary>
        private UInt16 _nOffset;
        /// <summary>
        /// Maximum number of movement commands per map character
        /// </summary>
        private const int MAX_COMMAND_LIST_ENTRIES = 0x10;
        /// <summary>
        /// Maximum size of each command (iterations+direction)
        /// </summary>
        private const int MAX_MOVEMENT_COMMAND_SIZE = sizeof(byte) * 2;
        /// <summary>
        /// all movements 
        /// </summary>
        private Queue<MovementCommand> _movementQueue = new Queue<MovementCommand>(MAX_COMMAND_LIST_ENTRIES);
        #endregion

        #region Public enums
        /// <summary>
        /// The direction of the movement as defined in saved.gam
        /// </summary>
        public enum MovementCommandDirection {None = 0, East = 1, North = 2, West = 3, South = 4 }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a new movemenent instruction to the end of the queue
        /// </summary>
        /// <param name="movementCommand"></param>
        public void AddNewMovementInstruction(MovementCommand movementCommand)
        {
            _movementQueue.Enqueue(movementCommand);
        }

        /// <summary>
        /// Clear all movements from the current list
        /// </summary>
        public void ClearMovements()
        {
            _movementQueue.Clear();
        }


        /// <summary>
        /// Checks to see if any movement commands are available
        /// </summary>
        /// <returns>true if there are commands available</returns>
        public bool IsNextCommandAvailable()
        {
            if (_movementQueue.Count > 0)
            {
                Debug.Assert(_movementQueue.Peek().Iterations > 0,
                    "You have no iterations left on your movement command but it's still in the queue");
            }
            return _movementQueue.Count > 0;
        }


        /// <summary>
        /// Gets the next movement command - expects you to have confirmed there is a movement first
        /// </summary>
        /// <returns></returns>
        public MovementCommandDirection GetNextMovementCommandDirection(bool bPeek = false)
        {
            if (_movementQueue.Count <= 0) { throw new Ultima5ReduxException("You have requested to GetNextMovementCommand but there are non left."); }
            MovementCommandDirection direction = _movementQueue.Peek().Direction;
            
            // calculate how many you will have left after you 
            int nRemaining = _movementQueue.Peek().Iterations - 1;
            
            Debug.Assert(nRemaining >= 0);

            if (nRemaining == 0 && !bPeek)
            {
                // we are done with it, so let's toss it
                _movementQueue.Dequeue();
            }
            else if (!bPeek)
            {
                // we have more moves, but we are going to spend one 
                int nRemainingMovements = _movementQueue.Peek().SpendSingleMovement();
            }

            return direction;
        }

        /// <summary>
        /// ToString override to simplify debug reading
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this._movementQueue.Count == 0) return "Empty";
            return "First: " + _movementQueue.Peek().Direction.ToString() + " for " + _movementQueue.Peek().Iterations + " times";
        }
        #endregion
    }
}
