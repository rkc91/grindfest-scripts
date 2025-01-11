using GrindFest;
using ItemBehaviour = GrindFest.ItemBehaviour;
using System.Collections.Generic;

namespace Scripts
{
    public static class GearUtilities
    {
    
        public static bool MeetsStatRequirements(ItemBehaviour item, AutomaticHero hero)
        {
            if (!item.equipable) return false;
            
            return item.equipable.RequiredDexterity <= hero.Character.Dexterity &&
                   item.equipable.RequiredStrength <= hero.Character.Strength &&
                   item.equipable.RequiredIntelligence <= hero.Character.Intelligence;
        }

        public static float GetWeaponDps(ItemBehaviour item)
        {
            return ((item.Weapon.MinDamage + item.Weapon.MaxDamage) / 2f) / item.Weapon.BaseAttackSpeed;
        }

        public static float GetArmorValue(ItemBehaviour item)
        {
            return item.Armor.Armor;
        }
    
        public static bool IsWeaponUpgrade(ItemBehaviour item, List<string> wantedWeaponTypes, AutomaticHero hero)
        {
            // bad type
            if (!wantedWeaponTypes.Contains(item.Weapon.WeaponType.ToString())) return false;

            var currentWeapon = hero.Character.Equipment[EquipmentSlot.RightHand];
        
            // nothing equipped in right hand, is upgrade.
            if (!currentWeapon) return true;
            
            return (GetWeaponDps(item) > GetWeaponDps(currentWeapon.Item));
        }
        
        public static bool IsArmorUpgrade(ItemBehaviour item, AutomaticHero hero)
        {
            var itemSlot = item.equipable.Slot;
            
            // nothing equipped in current slot, equip item.
            if (!hero.Character.Equipment[itemSlot]) return true;
                
            return GetArmorValue(item) > GetArmorValue(hero.Character.Equipment[itemSlot].Item);
        }
    }
}

