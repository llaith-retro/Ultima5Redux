﻿using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;

namespace Ultima5Redux.MapCharacters
{
    public class BarKeeper : ShoppeKeeper
    {
        public BarKeeper(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
        }

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new List<ShoppeKeeperOption>() 
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyBarkeeper),
        };

        public override string GetHelloResponse(TimeOfDay tod = null, ShoppeKeeperReference shoppeKeeperReference = null)
        {
            throw new System.NotImplementedException();
        }

        public override string GetWhichWouldYouSee()
        {
            throw new System.NotImplementedException();
        }

        public override string GetForSaleList()
        {
            throw new System.NotImplementedException();
        }
    }
}