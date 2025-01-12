// ReSharper disable RedundantUsingDirective
// ReSharper disable UseCollectionExpression
using GrindFest;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GrindFest.Characters;
using System;
using System.Collections;
using static Scripts.Utilities.GearUtilities;
using static Scripts.Utilities.TargetUtilities;
using Random = UnityEngine.Random;


namespace Scripts
{
    public class Nocc : AutomaticHero
    {
        #region ClassMembers
        
        private MonsterBehaviour? _target;
        private States _state;
        private const float AttackRange = 2f;
        private const float SearchRange = 15f;
        private const float RetreatDistance = 8f;
        private HashSet<string>? _filteredItems = new HashSet<string>() { "Flag" };
        private bool _canUsePotion;
        private AreaBehaviour? _currentArea;
        private FlagBehaviour? _destinationFlag;
        private int _flagCount;

        private static readonly List<string> IgnoredMobs = new List<string>()
        {
            "Crow"
        };
        
        private static readonly List<string> WantedWeaponTypes = new List<string>()
        {
            "Axe",
            "Sword",
            "TwoHandedSword",
            "Spear",
            "Mace"
        };
        
        private enum States
        {
            Start,
            Idle,
            Retreat,
            Heal,
            MoveToTarget,
            MoveAround,
            Navigate,
            FindTarget,
            Attack,
            Loot,
            Stop,
        }
        
        #endregion
        
        #region EventLoop
        
        private void Start()
        {
            _currentArea = this.CurrentArea;
            _target = null;
            _filteredItems = new HashSet<string>();
            _canUsePotion = true;
            _state = States.Stop;
        }

        private void Update()
        {
            _currentArea = this.CurrentArea;
            _flagCount = FlagBehaviour.Flags.Count;
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                // This is a static method, that means you don't need to have an instance of the class to call it
                var flag = AutomaticParty.PlaceFlag();
                flag.name = "Flag: " + CurrentArea.Name;
                flag.Index = FlagBehaviour.Flags.Count;
            }
            
            // check if at gold cap
            if (Party.Party.Gold >= Party.Party.GoldCap && !_filteredItems!.Contains("Gold Coins"))
            {
                Say("Over Gold Cap!!");
                _filteredItems!.Add("Gold Coins");
            }
            
            // skip state behavior if script is not running
            if (!IsBotting) return;
            
            // log our state
            Debug.Log(_state.ToString());
            
            // turn off AI
            if (Input.GetKeyDown(KeyCode.F4)) IsBotting = false;
            
            #region TransitionTable
            
            Debug.Log(_state.ToString());
            
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
                        Debug.Log("LowHp");
                        _state = States.Retreat;
                        break;
                    }
                  
                    if (_target)
                    {
                        
                        if (InAttackRange(_target, AttackRange, this))
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
                    Debug.Log(_canUsePotion);
                    if (Character.Equipment[EquipmentSlot.RightHand] == null ||
                        Character.Equipment[EquipmentSlot.RightHand].Item.name != "Vial of Health")
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
                    _target = FindTarget(SearchRange, IgnoredMobs, this);
                    
                    if (_target)
                    {
                        Say($"Target: {_target.name}: Distance: {GetTargetDistance(_target, this):0.0}");
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
                    // run around to search for a new target
                    RunAroundInArea();
                    _state = States.Idle;
                    break;
                }

                case States.Navigate:
                {
                    _target = null;
                    
                    if (Vector3.Distance(_destinationFlag!.transform.position,
                            this.transform.position) > 5)
                    {
                        GoTo(_destinationFlag.transform.position);
                        _state = States.Navigate;
                        break;
                    }
                    Say($"Arrived at Flag: {_destinationFlag.Index}," +
                        $" {_destinationFlag.name.Replace("Flag: ", "")}");
                    
                    _state = States.Stop;
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
                    var itemFound = GetNearestFilteredItem(_filteredItems!, this);
                    if (!itemFound)
                    {
                        Debug.Log("No Item Found");
                        _state = States.Idle;
                        break;
                    }
                    
                    if (CheckForUpgradeAndEquip(itemFound, WantedWeaponTypes, this))
                    {
                        _state = States.Loot;
                        break;
                    }

                    if (!itemFound.Armor && !itemFound.Weapon )
                    {
                        if(PickUp(itemFound)) Say($"Acquired: {itemFound.name}");
                        _state = States.Loot;
                        break;
                    }
                    
                    _state = States.Idle;
                    break;
                    
                }
                
                case States.Stop:
                {
                    // call this state to give more say commands.
                    _target = null;
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
        
        // listen for Say() commands
        public override void OnSay(string what, Transform target)
        {
            if (what.ToLower().Contains("stop"))
            {
                StopAllCoroutines();
                _state = States.Stop;
                return;
            }
            if (what.ToLower().Contains("goto"))
            {
                var flagIndex = int.Parse(what.Split(' ')[1])- 1;
                
                if (FlagBehaviour.Flags.Count == 0)
                {
                    Say("Error: No flags placed.");
                    return;
                }

                if (flagIndex >= FlagBehaviour.Flags.Count)
                {
                    Say("Error: No flag exists at this index.");
                    return;
                }
                
                _destinationFlag = FlagBehaviour.Flags[flagIndex];
                
                _state = States.Navigate;
                return;
            }
            
            _state = what.ToLower() switch
            {
                "loot" => _state = States.Loot,
                "start" => _state = States.Start,
                _ => _state = States.Stop
            };
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


