using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;



public abstract class FiniteStateMachine : MonoBehaviour
{
    public Character myCharacter;
    protected Animator myAnimator;
    public NavComponent myNavComponent;
    public CombatManager myCombatManager;
    [HideInInspector]
    public float walkDistance = 0;

    [HideInInspector]
    public CharacterUseSkill mySkills;

    public Vector3 previousPos;

    public Action CurrentState;

    // Start is called before the first frame update

    public abstract void ReceiveDamage(int dmg);
    
    public virtual void CombateIdle()
    {

    }
    public virtual void Combat()
    {
        
    }
    public virtual void Exploring()
    {

    }
    public virtual void Waiting()
    {

    }
    public virtual void ChoosingTargetInCombat()
    {

    }
    public virtual void CalculateMovement()
    {
        
    }
    public virtual void MoveToCombatPos()
    {

    }
    public virtual void ExploringSkill()
    {
        
    }
    public virtual void Dead()
    {

    }
}
