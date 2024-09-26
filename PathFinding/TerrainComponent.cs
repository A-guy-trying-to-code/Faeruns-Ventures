using UnityEngine;
using System.Collections;

public abstract class TerrainComponent : MonoBehaviour
{
    protected TerrainInterface m_TerrainRepresentation;
    public TerrainInterface TerrainRepresentation
    {
        get { return m_TerrainRepresentation; }
    }
}