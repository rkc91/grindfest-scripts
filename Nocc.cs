using GrindFest;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Scripts
{
    public class Nocc : AutomaticHero
    {
        
        private MonsterBehaviour? _target = null;
        private Nocc.States _state;

        private bool _hasTarget = false,
            _inAttackRange = false,
            _isTargetDead = false,
            _isLowHp = false;

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
        
        void Awake()
        {
            
        }

        void Start()
        {
            _state = States.Start;
        }
        
        void Update()
        {
            
        }
    }
}


