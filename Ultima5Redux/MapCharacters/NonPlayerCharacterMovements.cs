﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    /// <summary>
    /// Stores all movements of current NPC/monsters on current map
    /// </summary>
    public class NonPlayerCharacterMovements
    {
        private const int MAX_PLAYERS = 0x020;

        /// <summary>
        /// All available movement lists
        /// </summary>
        private List<NonPlayerCharacterMovement> movementList = new List<NonPlayerCharacterMovement>(MAX_PLAYERS);
        
        /// <summary>
        /// DataChunk of all loaded instructions (only needed during save and load)
        /// </summary>
        private DataChunk movementInstructionDataChunk;
        /// <summary>
        /// DataChunk of all loaded offsets into the movement lists (only needed during save and load)
        /// </summary>
        private DataChunk movementOffsetDataChunk;

        public NonPlayerCharacterMovements(DataChunk movementInstructionDataChunk, DataChunk movementOffsetDataChunk)
        {
            this.movementInstructionDataChunk = movementInstructionDataChunk;
            this.movementOffsetDataChunk = movementOffsetDataChunk;
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                movementList.Add(new NonPlayerCharacterMovement(i, movementInstructionDataChunk, movementOffsetDataChunk));
            }
        }

        /// <summary>
        /// Gets a movement from the list (often corresponds to the NPC index)
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        public NonPlayerCharacterMovement GetMovement(int nIndex)
        {
            return movementList[nIndex];
        }
    }
}