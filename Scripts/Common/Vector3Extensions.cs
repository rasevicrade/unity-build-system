using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 HeightIgnored(this Vector3 target)
    {
        return new Vector3(target.x, 0, target.z);
    }
}
