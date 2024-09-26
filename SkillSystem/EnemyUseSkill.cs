using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;
using KaimiraGames;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

public class EnemyUseSkill : CharacterUseSkill
{
    Action previousStatusAction;
    public int tempBuffDmg = 0;
    public int enemyIndex;
    System.Random rnd;
    [HideInInspector]
    public bool hasTarget = false;
    private bool canDoMoreDMG = true;
    void Start()
    {
        rnd = new System.Random();
        GetFurthestSkillRange();
        GetFurthestBuffRange();
        myNavComponent = GetComponent<NavComponent>();
        myCharacter = GetComponent<Character>();
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found!");
        }
        animator.SetInteger("SkillNumber", -1);
        InitializeSkillPool(); //init resource pool for skills
    }

    void Update()
    {
        if (canRotate)
        {
            RotateCharacterTowards(targetPosition);
        }
        if ((hasTarget == true) && (selectedSkillIndex != -1) && (myCharacter.myFSM.CurrentState != myCharacter.myFSM.Waiting))
        {
            hasTarget = false;
            targetPosition = target.transform.position;
            CalculateTravelTime();
            //character is in Combat
            if (myCharacter.myFSM.CurrentState == myCharacter.myFSM.ChoosingTargetInCombat)
            {
                if (Vector3.Distance(transform.position, targetPosition) <= skills[selectedSkillIndex].skillRange)
                {
                    StartCoroutine(RotateTowardTarget()); //target is within skill range, no need to move
                    return;
                }
                else if ((Vector3.Distance(transform.position, targetPosition)- skills[selectedSkillIndex].skillRange) <= myCombatManager.movement + skills[selectedSkillIndex].skillRange)
                {
                    MovetoTargetPosition(); //target is within reachable range, move to attack position before using skill
                    return;
                }
                else
                {
                    Debug.Log("unreachable in skill");//unreachable, won't use any skill
                    return;
                }
            }
            
        }
    }
    private void RotateCharacterTowards(Vector3 targetPosition)
    {
        Vector3 targetDir = targetPosition - transform.position;
        Vector3 turningDir = targetDir - transform.forward;
        turningDir.Normalize();
        turningDir.y = 0; //body won't turn
        transform.forward += turningDir * turnDirAmount;
        if (Vector3.Angle(transform.forward, targetDir) < 0.1)
        {
            //transform.forward = targetDir;
            canRotate = false;
        }
    }
    public override void InitializeSkillPool()
    {
        bool isHydra = false;
        if (this.name == "HYDRA")
        {
            isHydra = true;
        }
        SkillManager.Instance().InitialPool(skills.Length, enemyIndex, skills, isHydra);
    }

    public override void SelectSkill(int skillIndex)
    {
        if (myCombatManager.actionNumber == 0 && (mainGame.inCombat == true))
        {
            Debug.Log("no more action this turn!");
            return;
        }
        previousStatusAction = myCharacter.myFSM.CurrentState;
        if (previousStatusAction == myCharacter.myFSM.Exploring)
        {
            myCharacter.myFSM.CurrentState = myCharacter.myFSM.ExploringSkill; //no skill limit in the wild
        }
        else
        {
            myCharacter.myFSM.CurrentState = myCharacter.myFSM.ChoosingTargetInCombat;
        }
        selectedSkillIndex = skillIndex;

        hasTarget = true;
    }
    private void CalculateTravelTime()
    {
        if (skills[selectedSkillIndex].flySpeed > 0.0f)
        {
            skillTravelTime = Vector3.Distance(targetPosition, transform.position) / skills[selectedSkillIndex].flySpeed;
        }
        else
            skillTravelTime = 0.0f;
    }

    public override void TriggerSkillEffect()
    {
        if (skills[selectedSkillIndex].distance == SkillTemplate.Distance.Close || skills[selectedSkillIndex].distance == SkillTemplate.Distance.Middle)
        {
            if(skills[selectedSkillIndex].name== "FlameStream")
            {
                SkillManager.Instance().LoadGameObjectFromPool(enemyIndex, selectedSkillIndex, skills[selectedSkillIndex].spawnPos.position, skills[selectedSkillIndex].spawnPos.rotation);
            }
            else
            {
                SkillManager.Instance().LoadGameObjectFromPool(enemyIndex, selectedSkillIndex, skills[selectedSkillIndex].spawnPos.position, skills[selectedSkillIndex].spawnPos.rotation);
                MoveCamToMiddle();
            }
        }
        else if (skills[selectedSkillIndex].distance == SkillTemplate.Distance.SpawnAtTarget)
        {
            MoveCamToMiddle();

            SkillManager.Instance().LoadGameObjectFromPool(enemyIndex, selectedSkillIndex, targetPosition, Quaternion.identity);
        }
        else if (skills[selectedSkillIndex].distance == SkillTemplate.Distance.Long)
        {
            MoveCamToMiddle();

            SkillManager.Instance().LoadGameObjectFromPool(enemyIndex, selectedSkillIndex, skills[selectedSkillIndex].spawnPos.position, skills[selectedSkillIndex].spawnPos.rotation);
        }
        if (skills[selectedSkillIndex].AddtionalSpawnPos.Count() > 0)
        {
            SkillManager.Instance().LoadGameObjectFromPool(enemyIndex, selectedSkillIndex, skills[selectedSkillIndex].AddtionalSpawnPos[0].position, skills[selectedSkillIndex].spawnPos.rotation);
            SkillManager.Instance().LoadGameObjectFromPool(enemyIndex, selectedSkillIndex, skills[selectedSkillIndex].AddtionalSpawnPos[1].position, skills[selectedSkillIndex].spawnPos.rotation);
            //SkillManager.Instance().CreateAdditionalGameObjects(enemyIndex, selectedSkillIndex, skills[selectedSkillIndex].AddtionalSpawnPos.Count(), skills[selectedSkillIndex].AddtionalSpawnPos);
        }


        if (canDoMoreDMG)
        {
            canDoMoreDMG = false;
            StartCoroutine(HitDetection()); //normal skills
            if (!mainGame.inCombat)
            {
                myCharacter.myFSM.CurrentState = myCharacter.myFSM.Exploring;
            }
            else
            {
                myCharacter.myFSM.CurrentState = previousStatusAction;
            }
        }
        //if (name == "HYDRA" && (selectedSkillIndex != 1)) //skills with multiple trigger times 
        //{
        //    canDoMoreDMG = false;
        //}

        //if (!mainGame.inCombat)
        //{
        //    myCharacter.myFSM.CurrentState = myCharacter.myFSM.Exploring;
        //}
        //else
        //{
        //    myCharacter.myFSM.CurrentState = previousStatusAction;
        //}
    }
    private void MoveCamToMiddle()
    {
        Vector3 middlePoint;
        Vector3 deltaDir = target.transform.position - transform.position;
        middlePoint = transform.position + deltaDir * 0.66f;
        myCombatManager.myCameraController.OnMove(middlePoint); //move camera to the middle of target and attacker
    }
    public override void MovetoTargetPosition()
    {
        myNavComponent.movementStoppingDistance = skills[selectedSkillIndex].skillRange; //move to furtherst position needed
        if (myCharacter.myFSM.CurrentState == myCharacter.myFSM.ChoosingTargetInCombat)
        {
            myNavComponent.AStarPathChasinigPlayer(targetPosition);
        }
        StartCoroutine(RotateTowardTarget());

    }
    IEnumerator RotateTowardTarget()
    {
        yield return new WaitUntil(() => myNavComponent.movementStoppingDistance == myNavComponent.originalmovementStoppingDistance); // character is at attacking position
        canRotate = true;
        StartCoroutine(UseSkill());
    }
    IEnumerator UseSkill()
    {
        targetPosition.y = transform.position.y;
        yield return new WaitUntil(() => canRotate == false);
        animator.SetInteger("SkillNumber", selectedSkillIndex); //play skill animation
        animator.SetTrigger("UseSkill");
    }
    IEnumerator HitDetection()
    {
        yield return new WaitForSecondsRealtime(skillTravelTime);
        if (target.tag == "Player")
        {
            DealDamage(target);
            Vector3 effectPos = skills[selectedSkillIndex].spawnPos != null ? skills[selectedSkillIndex].spawnPos.position : targetPosition;
            if (skills[selectedSkillIndex].isAOE == true)
            {
                for(int i = 0; i < mainGame.players.Count; i++)
                {
                    if (mainGame.players[i] == target)
                    {
                        continue;
                    }
                    else if (Vector3.Distance(mainGame.players[i].transform.position, effectPos) <= skills[selectedSkillIndex].skillRange)
                    {
                        DealDamage(mainGame.players[i]); //damage if within aoe range 
                    }
                }
            }
        }
        else if (target.tag == "EnemyCombatants" && (skills[selectedSkillIndex].condition == SkillTemplate.Condition.AttackUp))
        {
            target = target.transform.gameObject.GetComponent<Character>();
            int dmgBuff = UnityEngine.Random.Range(skills[selectedSkillIndex].minBuffAmount, skills[selectedSkillIndex].minBuffAmount + 1);
            StartCoroutine(BuffEnemy(dmgBuff));
        }
        else if (target.tag == "EnemyCombatants" && (skills[selectedSkillIndex].condition == SkillTemplate.Condition.DefenceUp))
        {
            int conBuff = UnityEngine.Random.Range(skills[selectedSkillIndex].minBuffAmount, skills[selectedSkillIndex].minBuffAmount + 1);

            if (mainGame.inCombat == true)
            {
                StartCoroutine(BuffFriends(conBuff));
            }
        }
        if (myCharacter.myFSM.CurrentState == myCharacter.myFSM.Combat || myCharacter.myFSM.CurrentState == myCharacter.myFSM.ChoosingTargetInCombat) 
        {
            myCombatManager.actionNumber -= 1; //minus actionNumber if in combat
        }
        StartCoroutine(WaitToSwitchTurn());
    }
    public void DealDamage(Character theTarget)
    {
        theTarget = theTarget.transform.gameObject.GetComponent<Character>();

        WeightedList<int> attackDice = new WeightedList<int>();
        for (int i = 0; i < 20; i++)
        {
            if (i > 10)
            {
                attackDice.Add(i + 1, 2);
            }
            else
                attackDice.Add(i + 1, 4);
        }
        if (this.name == "HYDRA")
        {
            attackDice.Clear();
            for (int i = 0; i < 20; i++)
            {
                if (i > 10)
                {
                    attackDice.Add(i + 1, 5);
                }
                else
                    attackDice.Add(i + 1, 2);
            }
        }
        int attckRolls = attackDice.Next();
        //int attckRolls = rnd.Next(1, 21);
        int dmg = rnd.Next(skills[selectedSkillIndex].minDamage, skills[selectedSkillIndex].maxDamage + 1) + tempBuffDmg;
        if (attckRolls >= theTarget.armorClass)
        {
            theTarget.myFSM.ReceiveDamage(dmg); //damage
            //if(myCharacter.name!= "HYDRA")
            {
                theTarget.DamageText(dmg, theTarget);
            }
        }
        else
        {
            //if (myCharacter.name != "HYDRA")
            {
                theTarget.MissText(theTarget);
            }
        }
        theTarget.gameObject.GetComponent<ObstacleComponent>().enabled = true; //turn target's obstacle back on
        UpdateGrid();
    }
    public IEnumerator WaitToSwitchTurn()
    {
        yield return new WaitForSeconds(2);

        //if (canDoMoreDMG)
        //{
        myCombatManager.SwitchTurn(); //normal skills
        //}
        //if (name == "HYDRA" && (selectedSkillIndex != 1)) //skills with multiple trigger times 
        //{
        //    canDoMoreDMG = false;
        //}

        //myCombatManager.SwitchTurn();
        selectedSkillIndex = -1; // reset selectedSkillIndex at the end
        canDoMoreDMG = true;
        //StartCoroutine(DelayCanSwitch());
    }
    IEnumerator DelayCanSwitch()
    {
        yield return new WaitForSecondsRealtime(2);
        canDoMoreDMG = true;
    }

    IEnumerator BuffEnemy(int buffAmount)
    {
        Character buffTarget = target;
        SkillTemplate buffSkill = skills[selectedSkillIndex];
        buffTarget.UIIcon.GetComponent<UIAppear>().ATKBuff();

        target.gameObject.GetComponent<ObstacleComponent>().enabled = true; //turn target's obstacle back on
        UpdateGrid();

        buffTarget.gameObject.GetComponent<EnemyUseSkill>().tempBuffDmg = buffAmount;
        int endingTurnNumber = myCombatManager.turnCounter + 5; //buff duration 5 turns
        yield return new WaitUntil(() => myCombatManager.turnCounter == endingTurnNumber);
        buffTarget.gameObject.GetComponent<EnemyUseSkill>().tempBuffDmg = 0; //buff wares off when duration's up
        buffTarget.UIIcon.GetComponent<UIAppear>().ATKBuffOver();
    }
    IEnumerator BuffFriends(int buffAmount)
    {
        Character buffTarget = target;
        SkillTemplate buffSkill = skills[selectedSkillIndex];
        buffTarget.constitution += buffAmount;
        buffTarget.UIIcon.GetComponent<UIAppear>().DEFBuff();
        
        target.gameObject.GetComponent<ObstacleComponent>().enabled = true; //turn target's obstacle back on
        UpdateGrid();

        int endingTurnNumber = myCombatManager.turnCounter + 3; //buff duration 3 turns
        yield return new WaitUntil(() => myCombatManager.turnCounter == endingTurnNumber);
        buffTarget.constitution -= buffAmount; //buff wares off when duration's up
        buffTarget.UIIcon.GetComponent<UIAppear>().DEFBuffOver();
    }
    private void UpdateGrid()
    {
        myCharacter.myFSM.myNavComponent.pathfindingManager.dgc.RefreshCellState();
        myCharacter.myFSM.myNavComponent.InitAStarAndNavGrid(myCharacter.myFSM.myNavComponent.pathfindingManager.dgc.DynamicGrid);
        myCharacter.myFSM.myNavComponent.NavGrid.SyncNodeState(myCharacter.myFSM.myNavComponent.AStarSystem.NodePool);  //update grid
    }
    public void GetFurthestSkillRange()
    {
        float maxRange = 0;
        for (int i = 0; i < skills.Length; i++) 
        {
            if (skills[i].skillRange > maxRange && (skills[i].minBuffAmount == 0)) //buff doesn't count
            {
                maxRange = skills[i].skillRange;
            }
        }
        FurthestSkillRange = maxRange;
    }
    public void GetFurthestBuffRange()
    {
        float maxRange = 0;
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i].condition == SkillTemplate.Condition.AttackUp || skills[i].condition == SkillTemplate.Condition.DefenceUp) 
            {
                if(skills[i].skillRange > maxRange)
                {
                    maxRange = skills[i].skillRange;
                }
            }
        }
        maxBuffRange = maxRange;
    }
}
