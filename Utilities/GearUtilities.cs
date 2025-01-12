// ReSharper disable RedundantUsingDirective
using GrindFest;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GrindFest.Characters;
using System;
using System.Collections;
using GrindFest;

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

        private static bool IsWeaponUpgrade(ItemBehaviour item, List<string> wantedWeaponTypes, AutomaticHero hero)
        {
            // bad type
            if (!wantedWeaponTypes.Contains(item.Weapon.WeaponType.ToString())) return false;

            var currentWeapon = hero.Character.Equipment[EquipmentSlot.RightHand];
        
            // nothing equipped in right hand, is upgrade.
            if (!currentWeapon) return true;
            
            return (GetWeaponDps(item) > GetWeaponDps(currentWeapon.Item));
        }

        private static bool IsArmorUpgrade(ItemBehaviour item, AutomaticHero hero)
        {
            var itemSlot = item.equipable.Slot;
            
            // nothing equipped in current slot, equip item.
            if (!hero.Character.Equipment[itemSlot]) return true;
                
            return GetArmorValue(item) > GetArmorValue(hero.Character.Equipment[itemSlot].Item);
        }
        
        // returns a successful equip
        public static bool CheckForUpgradeAndEquip(ItemBehaviour item, List<string> wantedWeaponTypes,
            AutomaticHero hero)
        {
            if (!item) throw new ArgumentNullException(nameof(item));
            
            if (!MeetsStatRequirements(item, hero)) return false;

            if ((!item.Weapon || !IsWeaponUpgrade(item, wantedWeaponTypes, hero)) &&
                (!item.Armor || !IsArmorUpgrade(item, hero)) ||
                !item.equipable) return false;

            var replacedItem = hero.Character.Equipment[item.equipable.Slot] != null ?
                hero.Character.Equipment[item.equipable.Slot].Item : null;
            
            if(!replacedItem) Debug.Log("No weapon to replace.");
            
            hero.Equip(item);
            hero.Say($"Upgrade! Equipped: {item.name}");
            
            if(replacedItem) hero.PickUp(replacedItem);
            
            return true;
        }
    }
}

