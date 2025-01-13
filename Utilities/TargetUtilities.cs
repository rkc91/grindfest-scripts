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
    public static class TargetUtilities
    {
        // returns nearest item to hero if not included on filteredItemsList
        public static ItemBehaviour? GetNearestFilteredItem(HashSet<string> filteredItemsList, AutomaticHero hero)
        {
            // get items in area
            var nearestItems = hero.FindItemsOnGround();
            if (nearestItems == null) return null;
            
            List<ItemBehaviour> filteredItems = new List<ItemBehaviour>();
            filteredItems.AddRange(nearestItems);
            
            // ignore any flags
            filteredItems.RemoveAll(item => item.name.Contains("Flag"));
            
            // apply item filter list
            filteredItems.RemoveAll(item => filteredItemsList.Contains(item.Name));
            
            return filteredItems.Count == 0 ? null : filteredItems
                .OrderBy(item => Vector3.Distance(item.transform.position, hero.transform.position)).FirstOrDefault();
        }
        
        // returns the closest living target in search range to hero or null
        public static MonsterBehaviour? FindTarget(float searchRange, List<string> ignoredMobs, AutomaticHero hero)
        {
            var targetTable = new Dictionary<MonsterBehaviour, float>();
            
            var allTargets = UnityEngine.Object.FindObjectsByType<MonsterBehaviour>(FindObjectsSortMode.None);
            
            foreach (var target in allTargets)
            {
                var distance = Vector3.Distance(target.transform.position, hero.transform.position);
                
                // valid target conditions
                if (distance < searchRange &&
                    !target.Health.IsDead &&
                    !ignoredMobs.Contains(target.name) &&
                    Mathf.Abs(target.transform.position.y - hero.transform.position.y) < 3 &&
                    !target.Character.IsInWater)
                    
                {
                    targetTable.Add(target, distance);
                }
            }

            // no target within range
            if (targetTable.Count == 0) return null;
            
            // sort by distance 
            targetTable = targetTable.ToList().OrderBy(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            
            return targetTable.First().Key;
        }
        
        // returns distance from hero to the target
        public static float GetTargetDistance(MonsterBehaviour target, AutomaticHero hero)
        {
            var distance = Vector3.Distance(hero.transform.position, target.transform.position);
            return distance;
        }
        
        // returns if hero is in attack range of target
        public static bool InAttackRange(MonsterBehaviour target, float range, AutomaticHero hero)
        {
            return GetTargetDistance(target, hero) <= range;
        }
    }
    
    
}

