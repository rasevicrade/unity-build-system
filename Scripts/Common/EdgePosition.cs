using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdgePosition : MonoBehaviour
{
    public Edge edge;
    public enum Edge
    {
        North,
        South,
        West,
        East,
        Top,
        VerticalSide
    }
}
