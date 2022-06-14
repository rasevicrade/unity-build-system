using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Snapper
{
    public void SetDefaults()
    {
        SetAllowedTargets();
        SetSnapDistance();
        SetHorizontalFrontShiftDirection();
        SetMyFrontShiftDistance();
        SetRotationType();
        SetShiftSideways();
        SetMySideWaysShiftDistance();
        SetTargetSideWaysShiftDistance();
        SetShiftDown();
    }

    private void SetAllowedTargets()
    {
        targetsWithShift = new List<TargetPrefabShiftDistancePair>();
        switch (prefabType)
        {
            case PrefabType.Floor:
                targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Floor, HorizontalShiftDistance.Half));
                targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Wall));
                break;
            case PrefabType.Wall:
                targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Floor, HorizontalShiftDistance.Half));
                break;
            case PrefabType.Window:
                case PrefabType.Door:
                targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Wall));
                break;
            case PrefabType.Beam:
                targetsWithShift.Add(new TargetPrefabShiftDistancePair(PrefabType.Wall, HorizontalShiftDistance.Half));
                targetsWithShift.Add(new TargetPrefabShiftDistancePair(PrefabType.Floor, HorizontalShiftDistance.Half));
                break;
            case PrefabType.SideRoof:
                targetsWithShift.Add(new TargetPrefabShiftDistancePair(PrefabType.Beam, HorizontalShiftDistance.NegativeShorterHalf));
                break;
        }
    }

    private void SetHorizontalFrontShiftDirection()
    {
        switch (prefabType)
        {
            case PrefabType.Stairs: horizontalFrontShiftDirection = HorizontalFrontShiftDirection.Backward; break;
            default:
                horizontalFrontShiftDirection = HorizontalFrontShiftDirection.Forward; break;
        }
    }

    private void SetMyFrontShiftDistance()
    {
        switch (prefabType)
        {
            case PrefabType.Floor:
                myFrontShiftDistance = HorizontalShiftDistance.Half;
                break;
            case PrefabType.SideRoof:
                myFrontShiftDistance = HorizontalShiftDistance.ShorterHalf; break;
            default:
                myFrontShiftDistance = HorizontalShiftDistance.None;
                break;
        }
    }


    private void SetRotationType()
    {
        switch (prefabType)
        {
            case PrefabType.Floor: rotationType = RotationType.None; break;
            case PrefabType.Window: rotationType = RotationType.TargetRoot; break;
            default: rotationType = RotationType.TargetEdge; break;
        }
    }

    private void SetShiftSideways()
    {
        switch (prefabType)
        {
            case PrefabType.Beam:
                shiftSideways = true; break;
        }
    }

    private void SetMySideWaysShiftDistance()
    {

    }

    private void SetTargetSideWaysShiftDistance()
    {
        switch (prefabType)
        {
            case PrefabType.Beam:
                targetSideWaysShiftDistance = HorizontalShiftDistance.Half; break;
        }
    }

    private void SetShiftDown()
    {
        switch (prefabType)
        {
            case PrefabType.Floor:
            case PrefabType.Seam:
                shiftDown = true;
                break;
            default:
                shiftDown = false;
                break;
        }
    }

    private void SetSnapDistance()
    {

    }

}
