// ReSharper disable RedundantUsingDirective
using GrindFest;
using GrindFest.Characters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts.Utilities
{
    public static class GearUtilities
    {
        private static bool MeetsStatRequirements(ItemBehaviour item, AutomaticHero hero)
        {
            if (!item.equipable) return false;
            
            return item.equipable.RequiredDexterity <= hero.Character.Dexterity &&
                   item.equipable.RequiredStrength <= hero.Character.Strength &&
                   item.equipable.RequiredIntelligence <= hero.Character.Intelligence;
        }
        
        private static float GetWeaponDps(ItemBehaviour item)
        {
            return ((item.Weapon.MinDamage + item.Weapon.MaxDamage) / 2f) / item.Weapon.BaseAttackSpeed;
        }

        private static float GetArmorValue(ItemBehaviour item)
        {
            return item.Armor.Armor;
        }

        // returns true if weapon is a DPS increase.
        private static bool IsWeaponUpgrade(ItemBehaviour item, List<string> wantedWeaponTypes, AutomaticHero hero)
        {
            // bad type
            if (!wantedWeaponTypes.Contains(item.Weapon.WeaponType.ToString())) return false;

            var currentWeapon = hero.Character.Equipment[EquipmentSlot.RightHand];
        
            // nothing equipped in right hand, is upgrade.
            if (!currentWeapon) return true;
            
            return (GetWeaponDps(item) > GetWeaponDps(currentWeapon.Item));
        }
        
        // returns true if armor piece provides more armor.
        private static bool IsArmorUpgrade(ItemBehaviour item, AutomaticHero hero)
        {
            var itemSlot = item.equipable.Slot;
            
            // nothing equipped in current slot, equip item.
            if (!hero.Character.Equipment[itemSlot]) return true;
                
            return GetArmorValue(item) > GetArmorValue(hero.Character.Equipment[itemSlot].Item);
        }
        
        // will check for upgrade and equip it if we can. will also keep replaced item.
        // returns true for successful upgrade.
        public static bool CheckForUpgradeAndEquip(ItemBehaviour item, List<string> wantedWeaponTypes,
            AutomaticHero hero)
        {
            if (!item) throw new ArgumentNullException(nameof(item));
            
            if (!MeetsStatRequirements(item, hero)) return false;

            // check for upgrades or if not-equippable
            if ((!item.Weapon || !IsWeaponUpgrade(item, wantedWeaponTypes, hero)) &&
                (!item.Armor || !IsArmorUpgrade(item, hero)) ||
                !item.equipable) return false;
            
            // track our previous weapon if we had one
            var replacedItem = hero.Character.Equipment[item.equipable.Slot] ?
                hero.Character.Equipment[item.equipable.Slot].Item : null;
            
            if(!replacedItem) Debug.Log("No weapon to replace.");
            
            // equip upgrade
            hero.Equip(item);
            hero.Say($"Upgrade! Equipped: {item.name}");
            
            // grab our previous item we replaced
            if(replacedItem) hero.PickUp(replacedItem);
            
            return true;
        }
    }
}

