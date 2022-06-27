using System;
using UnityEngine;

public class GridPlacer : MonoBehaviour
{
    private static GridPlacer instance;
    private Blueprint _blueprint;
    private GameObject _wallPrefab;
    private GameObject _currentPrefabPreview;
    private GameObject _wallsGO;
    private GameObject _roomGO;
    private GameObject _floorsGO;


    public GameObject PlaceGrid(Blueprint blueprint, GameObject currentPrefabPreview, GameObject wallPrefab)
    {
        _blueprint = blueprint;
        _wallPrefab = wallPrefab;
        _currentPrefabPreview = currentPrefabPreview;
        _roomGO = new GameObject("Room");
        _floorsGO = new GameObject("Floors");
        _wallsGO = new GameObject("Walls");
        _floorsGO.transform.parent = _roomGO.transform;
        _wallsGO.transform.parent = _roomGO.transform;

        var currentPosition = blueprint.floorStartPosition;
        var direction = (blueprint.floorEndPosition - blueprint.floorStartPosition).normalized;
        var totalX = Math.Round(Math.Abs(blueprint.floorStartPosition.x - blueprint.floorEndPosition.x) / 4);
        var totalZ = Math.Round(Math.Abs(blueprint.floorStartPosition.z - blueprint.floorEndPosition.z) / 4);
        var fx = (float)Math.Round(direction.x, 0);
        var fz = (float)Math.Round(direction.z, 0);


        for (int x = 0; x <= totalX; x++)
        {
            for (int z = 0; z <= totalZ; z++)
            {
                var floor = blueprint.PlaceGameObject(currentPrefabPreview, new Vector3(currentPosition.x, blueprint.activeBaseHeight * blueprint.activeScale, currentPosition.z), currentPrefabPreview.transform.rotation);
                floor.transform.parent = _floorsGO.transform;

                if (wallPrefab != null)
                    AddWall(currentPosition, fx, fz, x, z, totalX, totalZ);
                currentPosition = new Vector3(currentPosition.x, blueprint.activeBaseHeight * blueprint.activeScale, currentPosition.z + 4 * fz);
            }
            currentPosition = new Vector3(currentPosition.x + 4 * fx, blueprint.activeBaseHeight * blueprint.activeScale, blueprint.floorStartPosition.z);
        }

        return _roomGO;
    }

    private void AddWall(Vector3 currentPosition, float fx, float fz, int x, int z, double totalX, double totalZ)
    {
        GameObject wall = null;
        if (x == 0)
            wall = _blueprint.PlaceGameObject(_wallPrefab, new Vector3(currentPosition.x - fx * 2, _blueprint.activeBaseHeight * _blueprint.activeScale, currentPosition.z), _currentPrefabPreview.transform.rotation * Quaternion.Euler(0, -90, 0));
        else if (x == totalX)
            wall = _blueprint.PlaceGameObject(_wallPrefab, new Vector3(currentPosition.x + fx * 2, _blueprint.activeBaseHeight * _blueprint.activeScale, currentPosition.z), _currentPrefabPreview.transform.rotation * Quaternion.Euler(0, -90, 0));

        if (wall != null)
            wall.transform.parent = _wallsGO.transform;
        if (z == 0)
            wall = _blueprint.PlaceGameObject(_wallPrefab, new Vector3(currentPosition.x, _blueprint.activeBaseHeight * _blueprint.activeScale, currentPosition.z - fz * 2), _currentPrefabPreview.transform.rotation);
        else if (z == totalZ)
            wall = _blueprint.PlaceGameObject(_wallPrefab, new Vector3(currentPosition.x, _blueprint.activeBaseHeight * _blueprint.activeScale, currentPosition.z + fz * 2), _currentPrefabPreview.transform.rotation);

        if (wall != null)
            wall.transform.parent = _wallsGO.transform;
    }
}
