﻿namespace Ultima5Redux.PlayerCharacters
{
    public class Potion : InventoryItem
    {
        public enum PotionColor { Blue = 0x282, Yellow = 0x283, Red = 0x284, Green = 0x285, Orange = 0x286, Purple = 0x287, Black = 0x288, White = 0x289 }

        public PotionColor Color { get; }

        private const int POTION_SPRITE = 259;
        
        public override bool HideQuantity { get; } = false;

        public Potion(PotionColor color, int quantity, string longName, string shortName) : base(quantity, longName, shortName, POTION_SPRITE)
        {
            Color = color;
        }
    }
}