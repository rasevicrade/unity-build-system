using System;
using System.Linq;
using UnityEngine;

public class GridPlacer : MonoBehaviour
{
    private Blueprint _blueprint;
    private GameObject _gridSurroundPreview;
    private Material _gridSurroundMaterial;
    private GameObject _gridBasePreview;
    private Material _gridBaseMaterial;
    private GameObject _wallsGO;
    private GameObject _roomGO;
    private GameObject _floorsGO;

    public void SetSurroundingObject(GameObject prefab, Material material)
    {
        _gridSurroundPreview = prefab;
        _gridSurroundMaterial = material;
    }

    public void SetBaseObject(GameObject prefab, Material material)
    {
        _gridBasePreview = prefab;
        _gridBaseMaterial = material;
    }

    public GameObject PlaceGrid(Blueprint blueprint)
    {
        _blueprint = blueprint;
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

        if (_gridSurroundPreview != null)
            PlaceSurroundAroundBase(totalX, totalZ, floorWidth, fx, fz);

        return _roomGO;
    }

    private void PlaceBase(float totalX, float totalZ, float floorWidth, float fx, float fz)
    {
        for (int x = 0; x <= totalX; x++)
        {
            for (int z = 0; z <= totalZ; z++)
            {
                var pos = new Vector3(_blueprint.floorStartPosition.x + floorWidth * x * fx, _blueprint.activeBaseHeight * _blueprint.activeScale, _blueprint.floorStartPosition.z + floorWidth * z * fz);
                PlacePrefab(
                    _gridBasePreview,
                    pos,
                    _gridBasePreview.transform.rotation,
                    _floorsGO,
                    _gridBaseMaterial);
            }
        }
    }

    private void PlaceSurroundAroundBase(float totalX, float totalZ, float floorWidth, float fx, float fz)
    {
        for (float x = 0; x <= totalX; x++)
        {
            var worldX = _blueprint.floorStartPosition.x + x * floorWidth * fx;
            var posZero = new Vector3(worldX, _blueprint.activeBaseHeight * _blueprint.activeScale, _blueprint.floorStartPosition.z - fz * 2);
            PlacePrefab(_gridSurroundPreview, posZero, _gridBasePreview.transform.rotation, _wallsGO, _gridSurroundMaterial);

            var posLast = new Vector3(worldX, _blueprint.activeBaseHeight * _blueprint.activeScale, _blueprint.floorStartPosition.z + fz * totalZ * floorWidth + fz * 2);
            PlacePrefab(_gridSurroundPreview, posLast, _gridBasePreview.transform.rotation, _wallsGO, _gridSurroundMaterial);
        }
        for (float z = 0; z <= totalZ; z++)
        {
            var worldZ = _blueprint.floorStartPosition.z + z * floorWidth * fz;
            PlacePrefab(_gridSurroundPreview, new Vector3(_blueprint.floorStartPosition.x - fx * 2, _blueprint.activeBaseHeight * _blueprint.activeScale, worldZ), _gridBasePreview.transform.rotation * Quaternion.Euler(0, -90, 0), _wallsGO, _gridSurroundMaterial);
            PlacePrefab(_gridSurroundPreview, new Vector3(_blueprint.floorStartPosition.x + fx * totalX * floorWidth + fx * 2, _blueprint.activeBaseHeight * _blueprint.activeScale, worldZ), _gridBasePreview.transform.rotation * Quaternion.Euler(0, -90, 0), _wallsGO, _gridSurroundMaterial);
        }

    }

    private void PlacePrefab(GameObject prefab, Vector3 position, Quaternion rotation, GameObject parent, Material customMaterial)
    {
        _blueprint.PlaceGameObject(
                    prefab,
                    position,
                    rotation,
                    parent,
                    customMaterial);
    }
}
