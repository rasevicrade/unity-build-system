using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoundExtensions
{
    public static float LongerSideLength(this Bounds target)
    {
        return target.size.x > target.size.z ? target.size.x : target.size.z;
    }

    public static float ShorterSideLength(this Bounds target)
    {
        return target.size.x < target.size.z ? target.size.x : target.size.z;
    }
}
