using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;
using static SkillTemplate;
using TMPro;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.TextCore.Text;
using KaimiraGames;

public class CharacterUseSkill : MonoBehaviour
{
    protected Character myCharacter;
    protected NavComponent myNavComponent;
    public MainGame mainGame;
    public CombatManager myCombatManager;
    protected Animator animator;
    Action previousStatusAction;
    [HideInInspector]
    public int selectedSkillIndex = -1;
    protected Vector3 targetPosition;
    [HideInInspector]
    public Character target;
    public SkillTemplate[] skills;
    RaycastHit []hits;
    RaycastHit targetHit;
    public float turnDirAmount;
    public float skillTravelTime;
    protected bool canRotate = false;
    public int characterIndex;
    System.Random rnd;
    public float FurthestSkillRange;
    public float maxBuffRange;
    public GameObject mainCamera;

    public virtual void InitializeSkillPool()
    {
        switch (myCharacter.characterName)
        {
            case "Aris":
                characterIndex = 0;
                break;
            case "Daff":
                characterIndex = 1;
                break;
            case "Jack":
                characterIndex = 2;
                break;
            default:
                break;
        }
        SkillManager.Instance().InitialPool(skills.Length, characterIndex, skills, false);
    }
    void Start()
    {
        mainCamera = GameObject.FindWithTag("MainCamera");
        rnd = new System.Random();
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
        if (Input.GetMouseButtonDown(0) && selectedSkillIndex != -1 && (myCharacter.myFSM.CurrentState != myCharacter.myFSM.Waiting))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int layerMaskNotWall = 1 << 8;
            layerMaskNotWall = ~layerMaskNotWall;
            hits = Physics.RaycastAll(ray, 2000.0f, layerMaskNotWall);
            float closestHitDistance = 100000; // init closestHitDistance to a huge number
            for (int i=0; i< hits.Length; i++)
            {
                float hitDistance = Vector3.Distance(hits[i].point, mainCamera.transform.position); 
                if (hitDistance < closestHitDistance) 
                {
                    closestHitDistance = hitDistance; //find furthest hit
                    targetHit = hits[i];
                }
            }
            //camera
            if (hits != null) 
            {
                //if(targetHit.collider.tag != "Player" && targetHit.collider.tag != "EnemyCombatants")
                //{
                //    targetPosition = hits[0].point;
                //}
                if (targetHit.collider.tag == "Player" || targetHit.collider.tag == "EnemyCombatants") 
                {
                    targetPosition = targetHit.collider.transform.position;
                    if(previousStatusAction!= myCharacter.myFSM.Exploring)
                    {
                        targetHit.collider.gameObject.GetComponent<ObstacleComponent>().enabled = false; //turn target's obstacle off to get close
                        //TurnOffNearbyColliders();
                        UpdateGrid();
                    }
                    
                }
                else
                {
                    targetPosition = hits[0].point;
                }

                CalculateTravelTime();
                //character is exploring
                if (myCharacter.myFSM.CurrentState == myCharacter.myFSM.ExploringSkill)
                {
                    if (skills[selectedSkillIndex].distance == SkillTemplate.Distance.SpawnAtTarget)
                    {
                        StartCoroutine(RotateTowardTarget());//use skill directly and spawn at target, i.e. 
                        return;
                    }
                    else if(skills[selectedSkillIndex].distance == SkillTemplate.Distance.Long)
                    {
                        StartCoroutine(RotateTowardTarget());//use skill directly and spawn at player i.e. bow
                    }
                    else if (skills[selectedSkillIndex].distance == SkillTemplate.Distance.Middle || skills[selectedSkillIndex].distance == SkillTemplate.Distance.Close)
                    {
                        MovetoTargetPosition();//move to attack position and spawn at player i.e. hammer or summongGolem
                    }
                }

                //character is in Combat
                else if (myCharacter.myFSM.CurrentState == myCharacter.myFSM.ChoosingTargetInCombat)
                {
                    if (Vector3.Distance(transform.position, targetPosition) <= skills[selectedSkillIndex].skillRange)
                    {
                        StartCoroutine(RotateTowardTarget()); //target is within skill range, no need to move
                        return;
                    }
                    else if (Vector3.Distance(transform.position, targetPosition) <= myCombatManager.movement + skills[selectedSkillIndex].skillRange)
                    {
                        MovetoTargetPosition(); //target is within reachable range, move to attack position before using skill
                        return;
                    }
                    else
                    {
                        //unreachable, won't use any skill
                        return;
                    }
                }
            }
        }
    }

    public virtual void SelectSkill(int skillIndex)
    {
        if (mainGame.inCombat == true && myCombatManager.currentActingCharacter != myCharacter) 
        {
            Debug.Log("not your tuen!");
            return;
        }
        if (myCombatManager.actionNumber == 0 && (mainGame.inCombat==true))
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
        Debug.Log("selectedSkillIndex: " + selectedSkillIndex);
    }
    private void TurnOffNearbyColliders()
    {
        target = targetHit.collider.transform.gameObject.GetComponent<Character>();
        int layerMaskNotTerrain = 1 << 6;
        layerMaskNotTerrain = ~layerMaskNotTerrain;
        Collider[] nearbyColliders = Physics.OverlapSphere(target.transform.position, 5f, layerMaskNotTerrain);
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
    private void CalculateTravelTime()
    {
        if (skills[selectedSkillIndex].flySpeed > 0.0f)
        {
            skillTravelTime = Vector3.Distance(targetPosition, transform.position) / skills[selectedSkillIndex].flySpeed;
        }
        else
            skillTravelTime = 0.0f;
    }
    private void RotateCharacterTowards(Vector3 targetPosition)
    {
        Vector3 targetDir = targetPosition - transform.position;
        Vector3 turningDir = targetDir - transform.forward;
        turningDir.Normalize();
        turningDir.y = 0; //body won't turn
        transform.forward += turningDir * turnDirAmount ;
    }

    public virtual void TriggerSkillEffect()
    {
        if (skills[selectedSkillIndex].distance == SkillTemplate.Distance.Close || skills[selectedSkillIndex].distance == SkillTemplate.Distance.Middle)
        {
            SkillManager.Instance().LoadGameObjectFromPool(characterIndex, selectedSkillIndex, skills[selectedSkillIndex].spawnPos.position, skills[selectedSkillIndex].spawnPos.rotation);

        }
        else if (skills[selectedSkillIndex].distance == SkillTemplate.Distance.SpawnAtTarget)
        {
            SkillManager.Instance().LoadGameObjectFromPool(characterIndex, selectedSkillIndex, targetPosition, Quaternion.identity);
        }
        else if (skills[selectedSkillIndex].distance == SkillTemplate.Distance.Long)
        {
            SkillManager.Instance().LoadGameObjectFromPool(characterIndex, selectedSkillIndex, skills[selectedSkillIndex].spawnPos.position, skills[selectedSkillIndex].spawnPos.rotation);
        }
        StartCoroutine(HitDetection());
    }
    public virtual void MovetoTargetPosition()
    {
        myNavComponent.movementStoppingDistance = skills[selectedSkillIndex].skillRange;//move to furtherst position needed
        if (myCharacter.myFSM.CurrentState == myCharacter.myFSM.ChoosingTargetInCombat) 
        {
            myNavComponent.CalculateAStarPath(targetPosition);
            myNavComponent.SetPathNodes(myNavComponent.GetPathNodes());
        }
        else
        {
            myNavComponent.MoveToWayPoint(myCharacter.transform.position, targetPosition);
        }
        StartCoroutine(RotateTowardTarget());

    }
    IEnumerator RotateTowardTarget()
    {
        yield return new WaitUntil(() => myNavComponent.movementStoppingDistance == 0.1f); // character is at attacking position

        canRotate = true;
        StartCoroutine(UseSkill());
    }
    IEnumerator UseSkill()
    {
        targetPosition.y = transform.position.y; // when exploring target's y is lower
        yield return new WaitUntil(() => Vector3.Angle(transform.forward, targetPosition-transform.position) < 0.1f);
        canRotate = false;
        animator.SetInteger("SkillNumber", selectedSkillIndex); //play skill animation
        animator.SetTrigger("UseSkill");

    }
    IEnumerator HitDetection()
    {
        yield return new WaitForSecondsRealtime(skillTravelTime);
        if (targetHit.collider.tag == "EnemyCombatants" || targetHit.collider.tag == "Enemy")
        {
            target = targetHit.collider.transform.gameObject.GetComponent<Character>();
            WeightedList<int> attackDice = new WeightedList<int>();
            for(int i = 0; i < 20; i++)
            {
                if (i > 10)
                {
                    attackDice.Add(i + 1, 7);
                }
                else
                    attackDice.Add(i + 1, 2);
            }
            //int attckRolls = attackDice.Next();
            int attckRolls = 20;
            //int attckRolls = rnd.Next(1, 21);
            int dmg = rnd.Next(skills[selectedSkillIndex].minDamage, skills[selectedSkillIndex].maxDamage+1);
            if (attckRolls >= target.armorClass)
            {
                target.myFSM.ReceiveDamage(dmg);
                target.DamageText(dmg, target);
            }
            else
            {
                target.MissText(target);
            }
            targetHit.collider.gameObject.GetComponent<ObstacleComponent>().enabled = true; //turn target's obstacle back on
        }
        else if (targetHit.collider.tag == "Player" && (skills[selectedSkillIndex].buffType == SkillTemplate.BuffType.Heal)) 
        {
            target = targetHit.collider.transform.gameObject.GetComponent<Character>();
            int hpRegained = rnd.Next(skills[selectedSkillIndex].minBuffAmount, skills[selectedSkillIndex].maxBuffAmount+1);
            if((hpRegained + target.hitPoints) > target.maxHitPoints) //if hpRegained overflow maxHitPoints
            {
                hpRegained = target.maxHitPoints - target.hitPoints; //max hpRegained can be
            }
            target.hitPoints += hpRegained;
            target.UpdateHealthBar();
            target.DamageText(hpRegained, target);

            if (previousStatusAction != myCharacter.myFSM.Exploring)
            {
                targetHit.collider.gameObject.GetComponent<ObstacleComponent>().enabled = true; //turn target's obstacle back on
                UpdateGrid();
            }
        }
        else if (targetHit.collider.tag == "Player" && (skills[selectedSkillIndex].condition == SkillTemplate.Condition.DefenceUp))
        {
            target = targetHit.collider.transform.gameObject.GetComponent<Character>();
            int conBuff = UnityEngine.Random.Range(skills[selectedSkillIndex].minBuffAmount, skills[selectedSkillIndex].minBuffAmount + 1);
            
            if (mainGame.inCombat == true)
            {
                StartCoroutine(BuffFriends(conBuff));
            }
            if (previousStatusAction != myCharacter.myFSM.Exploring)
            {
                targetHit.collider.gameObject.GetComponent<ObstacleComponent>().enabled = true; //turn target's obstacle back on
                UpdateGrid();
            }
        }
        if (myCharacter.myFSM.CurrentState == myCharacter.myFSM.ChoosingTargetInCombat)
        {
            myCombatManager.actionNumber -= 1;
        }

        if (!mainGame.inCombat)
        {
            myCharacter.myFSM.CurrentState = myCharacter.myFSM.Exploring;
        }
        else
        {
            myCharacter.myFSM.CurrentState = previousStatusAction;
        }

        selectedSkillIndex = -1; // reset selectedSkillIndex at the ends
    }
    private void UpdateGrid()
    {
        myCharacter.myFSM.myNavComponent.pathfindingManager.dgc.RefreshCellState();
        myCharacter.myFSM.myNavComponent.InitAStarAndNavGrid(myCharacter.myFSM.myNavComponent.pathfindingManager.dgc.DynamicGrid);
        myCharacter.myFSM.myNavComponent.NavGrid.SyncNodeState(myCharacter.myFSM.myNavComponent.AStarSystem.NodePool);  //update grid
    }
    IEnumerator BuffFriends(int buffAmount)
    {
        Character buffTarget = target;
        SkillTemplate buffSkill = skills[selectedSkillIndex];
        buffTarget.constitution += buffAmount;
        buffTarget.UIIcon.GetComponent<UIAppear>().DEFBuff();
        int endingTurnNumber = myCombatManager.turnCounter + 3; //buff duration 3 turns
        yield return new WaitUntil(() => myCombatManager.turnCounter == endingTurnNumber);
        buffTarget.constitution -= buffAmount; //buff wares off when duration's up
        buffTarget.UIIcon.GetComponent<UIAppear>().DEFBuffOver();
    }
}
