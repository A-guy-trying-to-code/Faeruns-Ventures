using UnityEngine;
using System.Collections;

public class ObstacleComponent : MonoBehaviour
{

    public enum eObstacleType
    {
        Rock = 0,
        Chest,
        Tree,
    }
    
    public eObstacleType obstacleType;

    void Start()
    {

    }

}