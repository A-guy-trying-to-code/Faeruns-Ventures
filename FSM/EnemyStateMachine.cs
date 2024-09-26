using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UnityEngine.GraphicsBuffer;
using System.Linq;
using Unity.VisualScripting;
using KaimiraGames;

public class EnemyStateMachine :  FiniteStateMachine
{
    Vector3 startLocation;
    private Character target;
    
    System.Random rnd;
    private WeightedList<Character> myVictims;
    private WeightedList<Character> myFriends;
    private WeightedList<Character> myFinalTarget;
    private Character weakestPlayer;
    public UIAppear uiAppear;
    private bool canGoInCombat = true;
    void Start()
    {
        myVictims = new();
        myFriends = new();
        myFinalTarget = new();
        mySkills = GetComponent<EnemyUseSkill>();
        myNavComponent = GetComponent<NavComponent>();
        myCharacter = GetComponent<Character>();
        myAnimator = GetComponent<Animator>();
        startLocation = transform.position;
        CurrentState = Exploring;
        rnd = new System.Random();
    }
    void Update()
    {
        CurrentState();
    }
    public override void ReceiveDamage(int dmg)
    {
        myAnimator.SetTrigger("damage");
        myCharacter.hitPoints -= dmg;
        myCharacter.UpdateHealthBar();
        if (myCharacter.hitPoints <= 0)
        {
            //myCombatManager.combatants.Remove(myCharacter);
            gameObject.tag = "DeadEnemy";
            myAnimator.SetTrigger("dead");
            myCharacter.UIIcon.SetActive(false);
            Destroy(myNavComponent);
            if (this.name == "HYDRA")
            {
                Destroy(this.GetComponent<Collider>());
            }
            CurrentState = Dead;
        }
    }
    public override void Exploring()
    {
        if (Time.frameCount % 1800 == 0 && myNavComponent.myAStarResult == false) 
        {
            Vector3 patrolTarget = GetPatrolTarget();
            myNavComponent.MoveToWayPoint(transform.position, patrolTarget);
        }
    }
    private Vector3 GetPatrolTarget()
    {
        Vector3 newPos = startLocation;
        newPos.x += rnd.Next(-10, 11);
        newPos.z += rnd.Next(-10, 11);
        return newPos;

    }
    public override void Combat()
    {
        uiAppear.ChangeColor();
        if ((myCombatManager.currentActingCharacter == myCharacter) && (mySkills.selectedSkillIndex == -1) && (canGoInCombat == true))   
        {
            SelectSkillAndTarget();
        }
        CalculateMovement();
    }
    
    public override void Waiting()
    {
        
    }
    public override void Dead()
    {
        
    }
    public void SelectSkillAndTarget()
    {
        InitVictimsList(); //determine targets weights and their useable skill
        InitFriendsList(); //determine friends weights and their useable skill

        if (GetFinalTarget()) //get target
        {
            SkillTemplate selectedSkill = target.potentialSkill.Next(); //select useable skill for selected target
            int selectedIndex = ConverPotentialAttackIndex(selectedSkill);
            
            mySkills.target = target; //assign target to EnemyUseSkill
            mySkills.target.gameObject.GetComponent<ObstacleComponent>().enabled = false; //turn target's obstacle off to get close
            //TurnOffNearbyColliders();
            UpdateGrid();
            Debug.Log("target "+ target.name);

            mySkills.SelectSkill(selectedIndex); //assign skillIndex to EnemyUseSkill
        }
        else
        {
            Debug.Log("all too far ");
            canGoInCombat = false; //so it won't go inside Combat
            StartCoroutine(WaitToSwitchTurn());
        }

    }
    private void UpdateGrid()
    {
        myNavComponent.pathfindingManager.dgc.RefreshCellState();
        myNavComponent.InitAStarAndNavGrid(myNavComponent.pathfindingManager.dgc.DynamicGrid);
        myNavComponent.NavGrid.SyncNodeState(myNavComponent.AStarSystem.NodePool);  //update grid
    }
    private void TurnOffNearbyColliders()
    {
        int layerMaskNotTerrain = 1 << 6;
        layerMaskNotTerrain = ~layerMaskNotTerrain;
        Collider[] nearbyColliders = Physics.OverlapSphere(mySkills.target.transform.position, 5f, layerMaskNotTerrain);
        if (nearbyColliders.Length > 0) // turn off nearby colliders to move
        {
            for (int i = 0; i < nearbyColliders.Length; i++)
            {
                if (nearbyColliders[i].gameObject.tag == "Player" || nearbyColliders[i].gameObject.tag == "EnemyCombatants")
                {
                    nearbyColliders[i].gameObject.GetComponent<ObstacleComponent>().enabled = false;
                }
            }
        }
    }
    public void InitVictimsList()
    {
        myVictims.Clear();
        UnityEngine.GameObject[] potentialTarget = UnityEngine.GameObject.FindGameObjectsWithTag("Player");
        foreach (UnityEngine.GameObject target in potentialTarget)
        {
            //myNavComponent.CalculateDistanceForEnemy(target.transform.position)
            if (Vector3.Distance(transform.position, target.transform.position) > myCombatManager.movement + mySkills.FurthestSkillRange)
            {
                continue; //unreachable target
            }
            Character targetCharacter = target.GetComponent<Character>();
            myVictims.Add(targetCharacter, 10);
            InitTargetPotentialSkill(targetCharacter); //init reachable skills
        }
        float minDistance = 100;
        float victimDist;
        int closestIndex = 0;
        for (int i = 0; i < myVictims.Count; i++) 
        {
            victimDist = Vector3.Distance(transform.position, myVictims[i].transform.position);
            if (victimDist < minDistance)
            {
                minDistance = victimDist;
                closestIndex = i;
            }
        }
        if (myVictims.Count != 0)
        {
            myVictims.SetWeightAtIndex(closestIndex, 40); //heavier weight for close victim
        }
        if (myVictims.Count == 0)
        {
            Debug.Log("no myVictims");
        }
    }
    public void InitTargetPotentialSkill(Character skillTarget)
    {
        skillTarget.potentialSkill.Clear();
        if (skillTarget.tag == "Player") //init reachable attacks for players
        {
            foreach (SkillTemplate potentialSkill in mySkills.skills)
            {
                if (Vector3.Distance(transform.position, skillTarget.transform.position) <= potentialSkill.skillRange && (potentialSkill.minBuffAmount == 0)) 
                {
                    skillTarget.potentialSkill.Add(potentialSkill, 10);


                    //RaycastHit blockHit;
                    //Physics.Linecast(transform.position, skillTarget.transform.position, out blockHit);
                    //if (potentialSkill.distance == SkillTemplate.Distance.Long && blockHit.collider.transform.position != skillTarget.transform.position) //long range got blocked
                    //{
                    //    for (int i = 0; i < target.potentialSkill.Count; i++)
                    //    {
                    //        if (target.potentialSkill[i].distance == SkillTemplate.Distance.Long)
                    //        {

                    //        }
                    //    }

                    //    selectedSkillIndex =
                    //        return;
                    //}
                }


                else if ((Vector3.Distance(transform.position, skillTarget.transform.position) <= (myCombatManager.movement + potentialSkill.skillRange)) && (potentialSkill.minBuffAmount == 0)) 
                {
                    //if (potentialSkill.skillRange > Vector3.Distance(transform.position, skillTarget.transform.position)) //won't use longer range skill in close distance
                    //{
                    //    continue;
                    //}
                    skillTarget.potentialSkill.Add(potentialSkill, 10);
                }
            }
        }
        else //init buffs for enemies
        {
            foreach (SkillTemplate potentialSkill in mySkills.skills) 
            {
                if (potentialSkill.condition == SkillTemplate.Condition.AttackUp || potentialSkill.condition == SkillTemplate.Condition.DefenceUp) 
                {
                    if (Vector3.Distance(transform.position, skillTarget.transform.position) <= myCombatManager.movement + potentialSkill.skillRange)
                    {
                        skillTarget.potentialSkill.Add(potentialSkill, 10);
                    }
                }
            }
        }
        
        
    }
    public void InitFriendsList()
    {
        myFriends.Clear();
        UnityEngine.GameObject[] potentialTarget = UnityEngine.GameObject.FindGameObjectsWithTag("EnemyCombatants");
        foreach (UnityEngine.GameObject target in potentialTarget)
        {
            if ((Vector3.Distance(transform.position, target.transform.position) > (myCombatManager.movement + mySkills.maxBuffRange))|| mySkills.maxBuffRange == 0)
            {
                continue; //unreachable friend or no buff skill
            }
            Character targetCharacter = target.GetComponent<Character>();
            myFriends.Add(targetCharacter, 10);
            InitTargetPotentialSkill(targetCharacter); //init reachable skills
        }
    }
    
    public override void CalculateMovement()
    {
        walkDistance = Vector3.Distance(transform.position, previousPos); //accumulating distance
        myCombatManager.movement -= walkDistance; //spend movement
        previousPos = transform.position;
    }
    private int ConverPotentialAttackIndex(SkillTemplate theSkill)
    {
        int newIndex = 0;
        for (int i = 0; i < mySkills.skills.Count(); i++) 
        {
            if (mySkills.skills[i] == theSkill) 
            {
                newIndex = i;
                break;
            }
        }
        return newIndex;
    }
    private bool GetFinalTarget()
    {
        myFinalTarget.Clear();
        if (myVictims.Count == 0 && myFriends.Count == 0) //no reachable player or enemy
        {
            Character chasingCharacter = FindWeakestPlayer();
            target = chasingCharacter;
            float chasingStopDistance = mySkills.FurthestSkillRange - 5; //desired distance is closer than FurthestSkillRange
            float targetDistance = Vector3.Distance(transform.position, chasingCharacter.transform.position);
            if ((targetDistance - chasingStopDistance) > myCombatManager.movement)
            {
                chasingStopDistance = myCombatManager.movement; // if desired location is further than movement allows, use all movement
            }
            myNavComponent.movementStoppingDistance = chasingStopDistance; //move towards weakest player as close as movement allows
            chasingCharacter.gameObject.GetComponent<ObstacleComponent>().enabled = false; //turn target's obstacle off
            UpdateGrid(); //update grid
            myNavComponent.AStarPathChasinigPlayer(chasingCharacter.transform.position); // calculate and move to position
            return false;
        }
        else if (myVictims.Count == 0)
        {
            myFinalTarget.Add(myFriends.Next(), 100); //no reachable player
        }
        else if (myFriends.Count == 0)
        {
            myFinalTarget.Add(myVictims.Next(), 100); //no reachable enemy
        }
        else
        {
            myFinalTarget.Add(myVictims.Next(), 70); //put players in with higher weights
            myFinalTarget.Add(myFriends.Next(), 30); //put enemies in with lower weights
        }
        target = myFinalTarget.Next();
        return true;

    }
    private Character FindWeakestPlayer()
    {
        int minHp = 100;
        for (int i = 0; i < myCombatManager.myPartyManager.partyMembers.Count(); i++) 
        {
            if (myCombatManager.myPartyManager.partyMembers[i].hitPoints <= minHp)  
            {
                minHp = myCombatManager.myPartyManager.partyMembers[i].hitPoints;
                weakestPlayer = myCombatManager.myPartyManager.partyMembers[i];
            }
        }
        return weakestPlayer;
    }
    public IEnumerator WaitToSwitchTurn()
    {
        yield return new WaitUntil(() => myNavComponent.movementStoppingDistance == 0.1f); // wait until stops moving
        target.gameObject.GetComponent<ObstacleComponent>().enabled = true; //turn target's obstacle back on
        UpdateGrid();

        myCombatManager.SwitchTurn(); //this'll stop moving clear canMove
        canGoInCombat = true;
    }
}
