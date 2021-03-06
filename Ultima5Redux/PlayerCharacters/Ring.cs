﻿using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters
{
    public class Ring : Armour
    {
        public RingEnum RingType;

        public enum RingEnum { RingInvisibility = 0x244, RingProtection = 0x245, RingRegeneration = 0x246 }

        private const int RING_SPRITE = 266;

        public override bool HideQuantity => false;

        public Ring(RingEnum ringType, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
            : base(equipment, dataOvlRef, gameStateByteArray, (int)ringType, RING_SPRITE)
        {
            RingType = ringType;
        }
    }
}