﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapCharacters
{
    /// <summary>
    /// Describes individual Shoppe Keepers
    /// </summary>
    [DataContract]
    public class ShoppeKeeperReference
    {
        [DataMember(Name="Location")] 
        private string _locationStr = default;
            
        [DataMember(Name = "ShoppeKeeperType")]
        private string _shoppeKeeperType = default;

        public string ShoppeKeeperName { get; set; }
        public string ShoppeName { get; set; }

        public DataOvlReference TheDataOvlReference { private get; set; }
        
        public List<DataOvlReference.Equipment> EquipmentForSaleList
        {
            get;
            set;
            // get
            // {
            //     List<DataOvlReference.Equipment> equipmentList = new List<DataOvlReference.Equipment>();
            //     
            // }
        } 
        
        public SmallMapReferences.SingleMapReference.Location ShoppeKeeperLocation
        {
            get
            {
                bool bSuccess = Enum.TryParse(this._locationStr, 
                    out SmallMapReferences.SingleMapReference.Location npcLocation);
                if (!bSuccess)
                {
                    throw new Ultima5ReduxException("Asked for an NPC "+this._locationStr+" type but the type didn't exist");
                }

                return npcLocation;
            }
        }

        public NonPlayerCharacterReference.NPCDialogTypeEnum TheShoppeKeeperType
        {
            get
            {
                bool bSuccess = Enum.TryParse(this._shoppeKeeperType, 
                    out NonPlayerCharacterReference.NPCDialogTypeEnum npcDialogTypeEnum);
                if (!bSuccess)
                {
                    throw new Ultima5ReduxException("Asked for an NPC "+this._shoppeKeeperType+" type but the type didn't exist");
                }

                return npcDialogTypeEnum;
            }
        }
            
        public NonPlayerCharacterReference NpcRef { get; set; }
    }
}