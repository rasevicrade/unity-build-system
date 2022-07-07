using UnityEngine;


public class PrefabGroup
{
    public string Name { get; set; }
    public GameObject[] Prefabs { get; set; }
    public int activePrefabIndex { get; set; } = 0;
    public Material Material { get; set; }
}
