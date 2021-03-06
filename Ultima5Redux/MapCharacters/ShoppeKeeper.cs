﻿using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.MapCharacters
{
    /// <summary>
    /// Shoppe keeper options
    /// Each of these describe the different behaviours that the shoppe keeper is responsible for
    /// </summary>
    public class ShoppeKeeperOption
    {
        public enum DialogueType { None, OkGoodbye, BuyBlacksmith, SellBlacksmith }

        public string ButtonName { get; }
        public DialogueType DialogueOption { get; }

        public ShoppeKeeperOption(string buttonName, DialogueType dialogueOption)
        {
            ButtonName = buttonName;
            DialogueOption = dialogueOption;
        }
    }
    
    /// <summary>
    /// A generic shoppe keeper with all common elements described
    /// </summary>
    public abstract class ShoppeKeeper
    {
        /// <summary>
        /// Construct a shoppe keeper
        /// </summary>
        /// <param name="shoppeKeeperDialogueReference"></param>
        /// <param name="theShoppeKeeperReference"></param>
        /// <param name="dataOvlReference"></param>
        protected ShoppeKeeper(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference)
        {
            ShoppeKeeperDialogueReference = shoppeKeeperDialogueReference;
            DataOvlReference = dataOvlReference;
            TheShoppeKeeperReference = theShoppeKeeperReference;
        }
        /// <summary>
        /// A list of the shoppe keeper options (abilities)
        /// </summary>
        public abstract List<ShoppeKeeperOption> ShoppeKeeperOptions { get; }

        protected readonly ShoppeKeeperDialogueReference ShoppeKeeperDialogueReference;
        protected readonly ShoppeKeeperReference TheShoppeKeeperReference;
        protected readonly DataOvlReference DataOvlReference;

        /// <summary>
        /// Dictionary that tracks previous random choice and helps to make sure they don't repeat
        /// </summary>
        private readonly Dictionary<DataOvlReference.DataChunkName, int> _previousRandomSelectionByChunk =
            new Dictionary<DataOvlReference.DataChunkName, int>();

        private const int PISSED_OFF_START = 0;
        private const int PISSED_OFF_STOP = 3;
        private const int HAPPY_START = 4;
        private const int HAPPY_STOP = 7;

        /// <summary>
        /// Gets a string describing the current time of day
        /// </summary>
        /// <param name="tod"></param>
        /// <returns></returns>
        private string GetTimeOfDayName(TimeOfDay tod)
        {
            if (tod.Hour > 5 && tod.Hour < 12) return "morning";
            if (tod.Hour >= 12 && tod.Hour < 6) return "afternoon";
            return "evening";
        }
        
        /// <summary>
        /// Gets a standard hello response based on current time of day 
        /// </summary>
        /// <param name="tod"></param>
        /// <returns></returns>
        public virtual string GetHelloResponse(TimeOfDay tod)
        {
            // todo: convert to data.ovl @ 0x8028
            string response = @"Good " + GetTimeOfDayName(tod) + ", and welcome to " +
                              TheShoppeKeeperReference.ShoppeName + "!"; 
            return response;
        }
        
        
        /// <summary>
        /// Get a random response when the shoppekeeper gets pissed off at you
        /// </summary>
        /// <returns></returns>
        public string GetPissedOffShoppeKeeperGoodbyeResponse()
        {
            return ShoppeKeeperDialogueReference.GetRandomMerchantStringFromRange(PISSED_OFF_START, PISSED_OFF_STOP);
        }

        /// <summary>
        /// Get a random response when the shoppekeeper is happy as you leave
        /// </summary>
        /// <returns></returns>
        public string GetHappyShoppeKeeperGoodbyeResponse()
        {
            return ShoppeKeeperDialogueReference.GetRandomMerchantStringFromRange(HAPPY_START, HAPPY_STOP);
        }

        /// <summary>
        /// Gets a common response after a purchase
        /// </summary>
        /// <returns></returns>
        public string GetThanksAfterPurchaseResponse()
        {
            return "Thank thee kindly!";
        }

        /// <summary>
        /// Gets a common response after deciding not to buy
        /// </summary>
        /// <returns></returns>
        public string GetPissedOffNotBuyingResponse()
        {
            return "Stop wasting my time!";
        }

        /// <summary>
        /// Gets a pissed off response when you don't have enough to buy the thing you tried to buy
        /// </summary>
        /// <returns></returns>
        public string GetPissedOffNotEnoughMoney()
        {
            return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_NOT_ENOUGH_MONEY);
        }

        /// <summary>
        /// Gets a common response asking if you want to buy the thing
        /// </summary>
        /// <returns></returns>
        public string GetDoYouWantToBuy()
        {
            return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_DO_YOU_WANT);
        }

        /// <summary>
        /// Gets a random string from a datachunk string list
        /// It automatically prevents the same string from being selected twice in a row
        /// </summary>
        /// <param name="chunkName"></param>
        /// <returns></returns>
        protected string GetRandomStringFromChoices(DataOvlReference.DataChunkName chunkName)
        {
            List<string> responses = DataOvlReference.GetDataChunk(chunkName)
                .GetChunkAsStringList().Strs;

            // if this hasn't been access before, then lets add a chunk to make sure we don't repeat the same thing 
            // twice in a row
            if (!_previousRandomSelectionByChunk.ContainsKey(chunkName))
            {
                _previousRandomSelectionByChunk.Add(chunkName, -1);
            }

            int nResponseIndex = ShoppeKeeperDialogueReference.GetRandomIndexFromRange(0, responses.Count);
            
            // if this response is the same as the last response, then we add one and make sure it is still in bounds 
            // by modding it 
            if (nResponseIndex == _previousRandomSelectionByChunk[chunkName])
                nResponseIndex = (nResponseIndex + 1) % responses.Count;

            _previousRandomSelectionByChunk[chunkName] = nResponseIndex;
            
            return responses[nResponseIndex];
        }
    }
}