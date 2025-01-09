using GrindFest;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Scripts
{
    public class Nocc : AutomaticHero
    {
        
        private MonsterBehaviour? _target = null;
        private Vector3 _targetPosition;
        private Nocc.States _state;
        private readonly int _attackRange = 3;
        private readonly float _searchRange = 15f;
        private readonly float _retreatDistance = 5f;
        

        private bool _hasTarget = false,
            _inAttackRange = false,
            _isTargetDead = false,
            _isLowHp = false,
            _shouldUsePotion = false;

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
        void Start()
        {
            _state = States.Start;
        }
        
        void Update()
        {
            if (_target) _targetPosition = _target.transform.position;
            
            if (!IsBotting) return;
            // transition table

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
                    RunAwayFromNearestEnemy(_retreatDistance);
                    
                    if (_shouldUsePotion)
                    {
                        _state = States.Heal;
                        break;
                    }

                    if (this.Health >= MaxHealth / 2)
                    {
                        _shouldUsePotion = true;
                        _state = States.Idle;
                        break;
                    }
                    
                    _state = States.Retreat;
                    break;
                }

                case States.Heal:
                {
                    if (HealthPotionCount() > 0)
                    {
                        DrinkHealthPotion();
                        Say($"Drank potion: {this.HealthPotionCount()} left.");
                        _shouldUsePotion = false;
                    }
                    
                    _state = States.Retreat;
                    
                    break;
                }

                case States.MoveToTarget:
                {
                    GoTo(_targetPosition, _attackRange);
                    _state = States.Idle;
                    
                    break;
                }

                case States.FindTarget:
                {
                    _target = FindTarget(_searchRange);

                    if (_target)
                    {
                        // found target, go back to idle
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
                    RunAroundInArea();
                    
                    _state = States.Idle;
                    break;
                }

                case States.Attack:
                {
                    // attack()
                    
                    if (_isTargetDead)
                    {
                        _state = States.Loot;
                        break;
                    }

                    _state = States.Idle;
                    break;
                }

                case States.Loot:
                {
                    var itemFound = scanForItemsFilterAndReturnNearestItem();
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

                    _state = States.CheckAndEquip;
                    break;
                    
                }
                
                case States.CheckAndEquip:
                {
                    //CheckAndEquip()
                    _state = States.Loot;
                    break;
                }

                default:
                {
                    _state = States.Idle;
                    break;
                }
            }
        }

        // function will return clo
        private MonsterBehaviour? FindTarget(float searchRange)
        {
            Dictionary<MonsterBehaviour, float> targetTable = new Dictionary<MonsterBehaviour, float>();
            
            var allTargets = FindObjectsByType<MonsterBehaviour>(FindObjectsSortMode.None);
            
            foreach (var target in allTargets)
            {
                var distance = Vector3.Distance(target.transform.position, this.transform.position);
                if (distance < searchRange) targetTable.Add(target, distance);
            }

            // no target within range
            if (targetTable.Count == 0) return null;
            
            // sort by distance and return closest
            targetTable = targetTable.OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            return targetTable.First().Key;
        }
        
    }
}


