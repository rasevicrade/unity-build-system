using System;
using System.Linq;
using UnityEngine;

public class GridPlacer : MonoBehaviour
{
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

        var floorWidth = 4f;
        
        var direction = (blueprint.floorEndPosition - blueprint.floorStartPosition).normalized;
        var totalX = (float)Math.Round(Math.Abs(blueprint.floorStartPosition.x - blueprint.floorEndPosition.x) / 4);
        var totalZ = (float)Math.Round(Math.Abs(blueprint.floorStartPosition.z - blueprint.floorEndPosition.z) / 4);
        var fx = (float)Math.Round(direction.x, 0);
        var fz = (float)Math.Round(direction.z, 0);

        PlaceBase(totalX, totalZ, floorWidth, fx, fz);

        if (wallPrefab != null)
            PlaceWallsAroundBase(totalX, totalZ, floorWidth, fx, fz);

        return _roomGO;
    }

    private void PlaceBase(float totalX, float totalZ, float floorWidth, float fx, float fz)
    {
        for (int x = 0; x <= totalX; x++)
        {
            for (int z = 0; z <= totalZ; z++)
            {
                var pos = new Vector3(_blueprint.floorStartPosition.x + floorWidth * x * fx, _blueprint.activeBaseHeight * _blueprint.activeScale, _blueprint.floorStartPosition.z + floorWidth * z * fz);
                _blueprint.PlaceGameObject(
                    _currentPrefabPreview,
                    pos,
                    _currentPrefabPreview.transform.rotation,
                    _floorsGO);
            }
        }
    }

    private void PlaceWallsAroundBase(float totalX, float totalZ, float floorWidth, float fx, float fz )
    {
        for (float x = 0; x <= totalX; x++)
        {
            var worldX = _blueprint.floorStartPosition.x + x * floorWidth * fx;
            var posZero = new Vector3(worldX, _blueprint.activeBaseHeight * _blueprint.activeScale, _blueprint.floorStartPosition.z - fz * 2);
            _blueprint.PlaceGameObject(_wallPrefab, posZero, _currentPrefabPreview.transform.rotation, _wallsGO);

            var posLast = new Vector3(worldX, _blueprint.activeBaseHeight * _blueprint.activeScale, _blueprint.floorStartPosition.z + fz * totalZ * floorWidth + fz * 2);
            _blueprint.PlaceGameObject(_wallPrefab, posLast, _currentPrefabPreview.transform.rotation, _wallsGO);
        }
        for (float z = 0; z <= totalZ; z++)
        {
            var worldZ = _blueprint.floorStartPosition.z + z * floorWidth * fz;
            _blueprint.PlaceGameObject(_wallPrefab, new Vector3(_blueprint.floorStartPosition.x - fx * 2, _blueprint.activeBaseHeight * _blueprint.activeScale, worldZ), _currentPrefabPreview.transform.rotation * Quaternion.Euler(0, -90, 0), _wallsGO);
            _blueprint.PlaceGameObject(_wallPrefab, new Vector3(_blueprint.floorStartPosition.x + fx * totalX * floorWidth + fx * 2, _blueprint.activeBaseHeight * _blueprint.activeScale, worldZ), _currentPrefabPreview.transform.rotation * Quaternion.Euler(0, -90, 0), _wallsGO);
        }

    }
}
