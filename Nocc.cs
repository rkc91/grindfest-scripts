using GrindFest;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GrindFest.Characters;

namespace Scripts
{
    public class Nocc : AutomaticHero
    {
        #region ClassMembers
        
        private MonsterBehaviour? _target;
        private States _state;
        private const float AttackRange = 2f;
        private const float SearchRange = 15f;
        private const float RetreatDistance = 10f;
        private float _targetDistance;
        private HashSet<string>? _filteredItems;
        private ItemBehaviour? _lastAcquiredItem;
        private States _lastState;
        private List<string> _ignoredMobs = new List<string>() { "Crow" };
        
        private bool 
            _inAttackRange,
            _isLowHp,
            _canUsePotion;

        private enum States
        {
            Start,
            Idle,
            Retreat,
            Heal,
            MoveToTarget,
            MoveAround,
            FindTarget,
            Attack,
            Loot,
            CheckAndEquip,
        }
        
        #endregion
        
        #region EventLoop

        private void Start()
        {
            _target = null;
            _filteredItems = new HashSet<string>();
            _inAttackRange = false;
            _isLowHp = false;
            _canUsePotion = true;
            _state = States.Start;
            _lastState = States.Start;
        }

        private void Update()
        {
            // check if at gold cap
            if (Party.Party.Gold >= Party.Party.GoldCap && !_filteredItems!.Contains("Gold Coins"))
            {
                Say("Over Gold Cap!!");
                _filteredItems!.Add("Gold Coins");
            }
            
            // skip state behavior if script is not running
            if (!IsBotting) return;
            
            // turn off botting
            if (Input.GetKeyDown(KeyCode.F4)) IsBotting = false;
            
            
            #region TransitionTable
            
            switch (_state)
            {
                case States.Start:
                {
                    _state = States.Idle;
                    break;
                }

                case States.Idle:
                {
                  
                    if (LowHp(30))
                    {
                        _state = States.Retreat;
                        break;
                    }
                  
                    if (_target)
                    {
                        
                        if (InAttackRange(_target, AttackRange))
                        {
                            _state = States.Attack;
                            break;
                        }
                        
                        // out of range
                        _state = States.MoveToTarget;
                        break;
                    }
                    
                    if (!_target)
                    {
                        // find new target
                        _state = States.FindTarget;
                        break;
                    }

                    Say("Back to Idle");
                    _state = States.Idle;
                    break;
                }

                case States.Retreat:
                {
                    RunAwayFromNearestEnemy(RetreatDistance);

                    if (Equipment[EquipmentSlot.RightHand].name != "Vial of Health")
                    {
                        _canUsePotion = true;
                        _state = States.Heal;
                        break;
                    }

                    if (this.Health >= MaxHealth * 0.8)
                    {
                        _state = States.Idle;
                        break;
                    }
                    
                    // keep running away if not ready to heal and hp not yet >80%
                    _state = States.Retreat;
                    break;
                }

                case States.Heal:
                {
                    if (HealthPotionCount() > 0 && _canUsePotion)
                    {
                        DrinkHealthPotion();
                        _canUsePotion = false;
                        Say($"Drank potion: {this.HealthPotionCount()} left.");
                    }
                    
                    // go back to retreating while we heal.
                    _state = States.Retreat;
                    break;
                }

                case States.MoveToTarget:
                {
                    if (_target)
                    {
                        GoTo(_target.transform.position, AttackRange);
                        _state = States.Idle;
                        break;
                    }
                    
                    _state = States.Idle;
                    break;
                }

                case States.FindTarget:
                {
                    _target = FindTarget(SearchRange, _ignoredMobs);
                    
                    if (_target)
                    {
                        Say($"Target: {_target.name}: Distance: {GetTargetDistance(_target).ToString("0.0")}");
                        _state = States.Idle;
                        break;
                    }

                    // no target found, move to find one
                    Say("No Target; Moving...");
                    _target = null;
                    
                    _state = States.MoveAround;
                    break;
                }

                case States.MoveAround:
                {
                    //Say("MoveAround");
                    // run around to search for a new target
                    RunAroundInArea();
                    _state = States.Idle;
                    break;
                }

                case States.Attack:
                {
                    if (_target)
                    {
                        if (_target.Health.IsDead)
                        {
                            Say("Target Dead");
                            _target = null;
                            _state = States.Loot;
                            break;
                        }
                       
                        //Say($"Attacking. Target Distance: {GetTargetDistance(_target)}.");
                        this.Character.UseSkill(this.Character.Combat.AttackSkill, null, _target!.transform.position);
                        _state = States.Idle;
                        break;
                        
                    }
                    
                    // no target. go back to idle.
                    _state = States.Idle;
                    break;
                }

                case States.Loot:
                {
                    var itemFound = GetNearestFilteredItem(_filteredItems!);
                    if (!itemFound)
                    {
                        _state = States.Idle;
                        break;
                    }
                    if (PickUp(itemFound))
                    {
                        _lastAcquiredItem = itemFound;
                        Say($"Acquired: {itemFound.name}");
                        _state = States.CheckAndEquip;
                        break;
                    }
                    
                    _state = States.Loot;
                    break;
                    
                }
                
                case States.CheckAndEquip:
                {
                    CheckAndEquip();
                    _state = States.Loot;
                    break;
                }

                default:
                {
                    _state = States.Idle;
                    break;
                }
            }
            #endregion
        }
        
        #endregion

        #region ClassMethods
        
        // returns nearest item if not included on filteredItems
        private ItemBehaviour? GetNearestFilteredItem(HashSet<string> filteredItems)
        {
            var nearestItems = FindItemsOnGround();

            return nearestItems?.FirstOrDefault(item => !filteredItems.Contains(item.name));
        }

        // returns the closest living target in search range or null
        private MonsterBehaviour? FindTarget(float searchRange, List<string> ignoredMobs)
        {
            Dictionary<MonsterBehaviour, float> targetTable = new Dictionary<MonsterBehaviour, float>();
            
            var allTargets = FindObjectsByType<MonsterBehaviour>(FindObjectsSortMode.None);
            
            foreach (var target in allTargets)
            {
                var distance = Vector3.Distance(target.transform.position, this.transform.position);
                if (distance < searchRange &&
                    !target.Health.IsDead &&
                    !ignoredMobs.Contains(target.name)
                    && Mathf.Abs(target.transform.position.y - this.transform.position.y) < 3)
                {
                    targetTable.Add(target, distance);
                }
            }

            // no target within range
            if (targetTable.Count == 0) return null;
            
            // sort by distance and return closest
            targetTable = targetTable.ToList().OrderBy(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            return targetTable.First().Key;
        }
        
        // check if an item is an upgrade and equips it.
        private bool CheckAndEquip()
        {
            //_target.Character.MovementState.
            return false;
        }
        
        // returns distance to the target
        private float GetTargetDistance(MonsterBehaviour target)
        {
            var distance = Vector3.Distance(this.transform.position, target.transform.position);
            return distance;
        }
        
        // returns if were in attack range of target
        private bool InAttackRange(MonsterBehaviour target, float range)
        {
            return GetTargetDistance(target) <= range;
        }

        // returns if HP is low or not
        private bool LowHp(float percentage)
        {
            percentage /= 100f;
            return this.Health < this.MaxHealth * percentage;
        }
        #endregion
    }
}


