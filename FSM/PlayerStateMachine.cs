using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStateMachine : FiniteStateMachine
{
    public PathfindingManager myPathFindingManager;
    bool isOverUI;
    void Start()
    {
        mySkills = GetComponent<CharacterUseSkill>();
        myNavComponent = GetComponent<NavComponent>();
        myCharacter = GetComponent<Character>();
        myAnimator = GetComponent<Animator>();
        CurrentState = Exploring;
    }

    void Update()
    {
        CurrentState();
    }
    public override void Exploring()
    {
        if (myPathFindingManager.currentCharacter.name == myCharacter.name)
        {
            isOverUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) & (isOverUI == false))
            {
                myPathFindingManager.ExploringMove();
            }
        }
    }
    public override void Combat()
    {
        if (myPathFindingManager.currentCharacter.name == myCharacter.name)
        {
            CalculateMovement();
            if (myCombatManager.movement <= 0)
            {
                CurrentState = Waiting;  // if no more movment, can't move
            }
            else
            {
                myPathFindingManager.CombatMove();
            }
        }
    }
    public override void Waiting()
    {

    }
    public override void ReceiveDamage(int dmg)
    {
        myAnimator.SetTrigger("damage");
        myCharacter.hitPoints -= dmg;
        myCharacter.UpdateHealthBar();
        if (myCharacter.hitPoints <= 0)
        {
            //myCombatManager.combatants.Remove(myCharacter);
            gameObject.tag = "DeadPlayer";
            myAnimator.SetTrigger("dead");
            //myCharacter.UIIcon.SetActive(false);
            CurrentState = Dead;
        }
    }
    public override void ChoosingTargetInCombat()
    {
        CalculateMovement();
    }
    public override void CalculateMovement()
    {
        walkDistance = Vector3.Distance(transform.position, previousPos); //accumulating distance
        myCombatManager.movement -= walkDistance; //spend movement
        previousPos = transform.position;
    }
    public override void Dead()
    {
        myNavComponent.enabled = false;
    }
}
