﻿using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapCharacters
{
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
    
    public abstract class ShoppeKeeper
    {
        protected ShoppeKeeper(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference)
        {
            _shoppeKeeperDialogueReference = shoppeKeeperDialogueReference;
            _dataOvlReference = dataOvlReference;
            TheShoppeKeeperReference = theShoppeKeeperReference;
        }
        
        protected readonly ShoppeKeeperDialogueReference _shoppeKeeperDialogueReference;
        public readonly ShoppeKeeperReference TheShoppeKeeperReference;
        protected DataOvlReference _dataOvlReference;

        private readonly Dictionary<DataOvlReference.DataChunkName, int> _previousRandomSelectionByChunk =
            new Dictionary<DataOvlReference.DataChunkName, int>();

        private const int PISSED_OFF_START = 0;
        private const int PISSED_OFF_STOP = 3;
        private const int HAPPY_START = 4;
        private const int HAPPY_STOP = 7;

        public abstract List<ShoppeKeeperOption> ShoppeKeeperOptions { get; }

        //public abstract string GetWeHaveResponse();

        private string GetTimeOfDayName(TimeOfDay tod)
        {
            if (tod.Hour > 5 && tod.Hour < 12) return "morning";
            if (tod.Hour >= 12 && tod.Hour < 6) return "afternoon";
            return "evening";
        }
        
        public virtual string GetHelloResponse(TimeOfDay tod)
        {
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
            return _shoppeKeeperDialogueReference.GetRandomMerchantStringFromRange(PISSED_OFF_START, PISSED_OFF_STOP);
        }

        /// <summary>
        /// Get a random response when the shoppekeeper is happy as you leave
        /// </summary>
        /// <returns></returns>
        public string GetHappyShoppeKeeperGoodbyeResponse()
        {
            return _shoppeKeeperDialogueReference.GetRandomMerchantStringFromRange(HAPPY_START, HAPPY_STOP);
        }

        public string GetThanksAfterPurchaseResponse()
        {
            return "Thank thee kindly!";
        }

        public string GetPissedOffNotBuyingResponse()
        {
            return "Stop wasting my time!";
        }

        public string GetPissedOffNotEnoughMoney()
        {
            return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_NOT_ENOUGH_MONEY);
        }

        public string GetDoYouWantToBuy()
        {
            return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_DO_YOU_WANT);
        }

        protected string GetRandomStringFromChoices(DataOvlReference.DataChunkName chunkName)
        {
            List<string> responses = _dataOvlReference.GetDataChunk(chunkName)
                .GetChunkAsStringList().Strs;

            // if this hasn't been access before, then lets add a chunk to make sure we don't repeat the same thing 
            // twice in a row
            if (!_previousRandomSelectionByChunk.ContainsKey(chunkName))
            {
                _previousRandomSelectionByChunk.Add(chunkName, -1);
            }

            int nResponseIndex = _shoppeKeeperDialogueReference.GetRandomIndexFromRange(0, responses.Count);
            
            // if this response is the same as the last response, then we add one and make sure it is still in bounds 
            // by modding it 
            if (nResponseIndex == _previousRandomSelectionByChunk[chunkName])
                nResponseIndex = (nResponseIndex + 1) % responses.Count;

            _previousRandomSelectionByChunk[chunkName] = nResponseIndex;
            
            return responses[nResponseIndex];
        }
    }
}