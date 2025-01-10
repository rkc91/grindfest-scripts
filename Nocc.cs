using GrindFest;
using UnityEngine;

namespace Scripts
{
    public class Nocc : AutomaticHero
    {
        #region ClassMembers
        
        private MonsterBehaviour? _target;
        private Vector3 _targetPosition;
        private States _state;
        private const int AttackRange = 3;
        private const float SearchRange = 15f;
        private const float RetreatDistance = 5f;
        private List<string> _filteredItems = [];
        private ItemBehaviour? _lastAcquiredItem;
        
        private bool _hasTarget,
            _inAttackRange,
            _isTargetDead,
            _isLowHp,
            _shouldUsePotion;

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
            _state = States.Start;
        }

        private void Update()
        {
            // update our target position if we have a target
            if (_target)
            {
                _targetPosition = _target.transform.position;
                _inAttackRange = (this.transform.position - _target.transform.position).magnitude < SearchRange;
            }
            
            // update hp healing flag
            if (this.Health < MaxHealth * 0.3 ) _isLowHp = true;
            
            // skip state behavior if script is not running
            
            if (IsBotting) return;
            
            
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
                  
                    if (_isLowHp)
                    {
                        _state = States.Retreat;
                        break;
                    }
                  
                    if (_hasTarget && !_inAttackRange)
                    {
                        _state = States.MoveToTarget;
                        break;
                    }
                    
                    if (_hasTarget && _inAttackRange)
                    {
                        _state = States.Attack;
                        break;
                    }

                    if (!_hasTarget)
                    {
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
                    
                    if (_shouldUsePotion)
                    {
                        _state = States.Heal;
                        break;
                    }

                    if (this.Health >= MaxHealth / 2)
                    {
                        // healed to past 50%, okay to heal again
                        _shouldUsePotion = true;
                        _state = States.Idle;
                        break;
                    }
                    
                    // keep running away if not ready to heal and hp not yet >50%
                    _state = States.Retreat;
                    break;
                }

                case States.Heal:
                {
                    if (HealthPotionCount() > 0)
                    {
                        DrinkHealthPotion();
                        Say($"Drank potion: {this.HealthPotionCount()} left.");
                        _shouldUsePotion = false; // prevents repeatedly attempting to use potions
                    }
                    
                    // go back to retreating while we heal.
                    _state = States.Retreat;
                    break;
                }

                case States.MoveToTarget:
                { 
                    GoTo(_targetPosition, AttackRange); 
                    _state = States.Idle;
                    break;
                }

                case States.FindTarget:
                {
                    _target = FindTarget(SearchRange);

                    if (_target)
                    {
                        _hasTarget = true;
                        _state = States.Idle;
                        break;
                    }
                    
                    // no target found, move to find one
                    _hasTarget = false;
                    _state = States.MoveAround;
                    break;
                }

                case States.MoveAround:
                {
                    // run around to search for a new target
                    RunAroundInArea();
                    _state = States.Idle;
                    break;
                }

                case States.Attack:
                {
                    // attack at target position
                    this.Character.UseSkill(this.Character.Combat.AttackSkill, null, _targetPosition);
                    
                    // check if target died
                    if (_target && _target.Health.IsDead) _isTargetDead = true;
                    
                    if (_isTargetDead)
                    {
                        _hasTarget = false;
                        _state = States.Loot;
                        break;
                    }

                    _state = States.Idle;
                    break;
                }

                case States.Loot:
                {
                    var itemFound = GetNearestFilteredItem(_filteredItems);
                    if (!itemFound)
                    {
                        _state = States.Idle;
                        break;
                    }
                    if (!PickUp(itemFound))
                    {
                        _state = States.Loot;
                        break;
                    }
                    Say($"Acquired: {itemFound.name}");
                    _lastAcquiredItem = itemFound;
                    _state = States.CheckAndEquip;
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
        private ItemBehaviour? GetNearestFilteredItem(List<string> filteredItems)
        {
            var nearestItem = FindNearestItemOnGround();

            return !filteredItems.Contains(nearestItem.name) ? nearestItem : null;
        }

        // returns the closest target in search range or null
        private MonsterBehaviour? FindTarget(float searchRange)
        {
            Dictionary<MonsterBehaviour, float> targetTable = new Dictionary<MonsterBehaviour, float>();
            
            var allTargets = FindObjectsByType<MonsterBehaviour>(FindObjectsSortMode.None);
            
            foreach (var target in allTargets)
            {
                var distance = Vector3.Distance(target.transform.position, this.transform.position);
                if (distance < searchRange && !target.Health.IsDead) targetTable.Add(target, distance);
            }

            // no target within range
            if (targetTable.Count == 0) return null;
            
            // sort by distance and return closest
            targetTable = targetTable.OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            return targetTable.First().Key;
        }
        
        // check if an item is an upgrade and equips it.
        private bool CheckAndEquip()
        {
            return false;
        }
        #endregion
    }
}


