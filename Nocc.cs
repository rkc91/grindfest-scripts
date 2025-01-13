// ReSharper disable RedundantUsingDirective
// ReSharper disable UseCollectionExpression
using GrindFest;
using GrindFest.Characters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Scripts.States;
using static Scripts.Utilities.GearUtilities;
using static Scripts.Utilities.GeneralUtilities;
using static Scripts.Utilities.TargetUtilities;

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
        public FlagBehaviour? _destinationFlag;

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
        
        #endregion
        
        #region EventLoop
        
        private void Start()
        {
            _currentArea = CurrentArea;
            _target = null;
            _filteredItems = new HashSet<string>();
            _canUsePotion = true;
            _state = Stop; 
        }

        private void Update()
        {
            _currentArea = CurrentArea;
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
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
                case Initial:
                {
                    _state = Reset;
                    break;
                }

                case Reset:
                {
                  
                    if (LowHp(30))
                    {
                        Debug.Log("LowHp");
                        _state = Retreat;
                        break;
                    }
                  
                    if (_target)
                    {
                        
                        if (InAttackRange(_target, AttackRange, this))
                        {
                            _state = Attack;
                            break;
                        }
                        
                        // out of range
                        _state = MoveToTarget;
                        break;
                    }
                    
                    if (!_target)
                    {
                        // find new target
                        _state = Target;
                        break;
                    }

                    Say("Back to Idle");
                    _state = Reset;
                    break;
                }

                case Retreat:
                {
                    RunAwayFromNearestEnemy(RetreatDistance);
                    Debug.Log(_canUsePotion);
                    if (Character.Equipment[EquipmentSlot.RightHand] == null ||
                        Character.Equipment[EquipmentSlot.RightHand].Item.name != "Vial of Health")
                    {
                        _canUsePotion = true;
                        _state = Heal;
                        break;
                    }

                    if (!LowHp(50))
                    {
                        _state = Reset;
                        break;
                    }
                    
                    // keep running away if not ready to heal and hp not yet >80%
                    _state = Retreat;
                    break;
                }

                case Heal:
                {
                    if (HealthPotionCount() > 0 && _canUsePotion)
                    {
                        DrinkHealthPotion();
                        _canUsePotion = false;
                        Say($"Drank potion: {HealthPotionCount()} left.");
                    }
                    
                    // go back to retreating while we heal.
                    _state = Retreat;
                    break;
                }

                case MoveToTarget:
                {
                    if (_target)
                    {
                        GoTo(_target.transform.position, AttackRange);
                        _state = Reset;
                        break;
                    }
                    
                    _state = Reset;
                    break;
                }

                case Target:
                {
                    _target = FindTarget(SearchRange, IgnoredMobs, this);
                    
                    if (_target)
                    {
                        Say($"Target: {_target.name}: Distance: {GetTargetDistance(_target, this):0.0}");
                        _state = Reset;
                        break;
                    }

                    // no target found, move to find one
                    Say("No Target; Moving...");
                    _target = null;
                    
                    _state = MoveAround;
                    break;
                }

                case MoveAround:
                {
                    // run around to search for a new target
                    RunAroundInArea();
                    _state = Reset;
                    break;
                }

                case Navigate:
                {
                    _target = null;

                    if (!NavigateToFlag(_destinationFlag!, this)) _state = Navigate;
                    
                    _state = Stop;
                    break;
                }

                case Attack:
                {
                    if (_target)
                    {
                        if (_target.Health.IsDead)
                        {
                            Say("Target Dead");
                            _target = null;
                            _state = Loot;
                            break;
                        }
                       
                        //Say($"Attacking. Target Distance: {GetTargetDistance(_target)}.");
                        Character.UseSkill(Character.Combat.AttackSkill, null, _target!.transform.position);
                        _state = Reset;
                        break;
                        
                    }
                    
                    // no target. go back to idle.
                    _state = Reset;
                    break;
                }

                case Loot:
                {
                    var itemFound = GetNearestFilteredItem(_filteredItems!, this);
                    if (!itemFound)
                    {
                        Debug.Log("No Item Found");
                        _state = Reset;
                        break;
                    }
                    
                    if (CheckForUpgradeAndEquip(itemFound, WantedWeaponTypes, this))
                    {
                        _state = Loot;
                        break;
                    }

                    if (!itemFound.Armor && !itemFound.Weapon )
                    {
                        if(PickUp(itemFound)) Say($"Acquired: {itemFound.name}");
                        _state = Loot;
                        break;
                    }
                    
                    _state = Reset;
                    break;
                    
                }
                
                case Stop:
                {
                    // call this state to give more say commands.
                    _target = null;
                    break;
                }

                default:
                {
                    _state = Reset;
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
            // get the state we will transition to after parsing the say string
            var futureState = HandleSay(what, this);

            if (futureState == Navigate)
            {
                // extract the flag index from a "goto" command and get the right FlagBehaviour
                _destinationFlag = FlagBehaviour.Flags[int.Parse(what.ToLower().Split(' ')[1]) - 1];
            }
            
            // commit the state change
            _state = futureState;
            
        }
        
        // returns if HP is low or not
        private bool LowHp(float percentage)
        {
            percentage /= 100f;
            return Health < MaxHealth * percentage;
        }
        
        #endregion
    }
}


