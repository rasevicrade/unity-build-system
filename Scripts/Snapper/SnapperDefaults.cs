using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Snapper
{
    public void SetDefaults()
    {
        SetAllowedTargets();
        SetHorizontalFrontShiftDirection();
        SetMyFrontShiftDistance();
        SetRotationType();
        SetShiftSideways();
        SetTargetSideWaysShiftDistance();
        SetShiftDown();
    }

    private void SetAllowedTargets()
    {
        defaults.targetsWithShift = new List<TargetPrefabShiftDistancePair>();
        switch (defaults.prefabType)
        {
            case PrefabType.Floor:
            case PrefabType.Stairs:
                defaults.targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Floor, HorizontalShiftDistance.Half));
                defaults.targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Wall));
                defaults.targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Stairs, HorizontalShiftDistance.Half));
                break;
            case PrefabType.Wall:
                defaults.targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Floor, HorizontalShiftDistance.Half));
                defaults.targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Wall));
                break;
            case PrefabType.Window:
            case PrefabType.Door:
                defaults.targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Wall));
                break;
            case PrefabType.Beam:
                defaults.targetsWithShift.Add(new TargetPrefabShiftDistancePair(PrefabType.Wall, HorizontalShiftDistance.Half));
                defaults.targetsWithShift.Add(new TargetPrefabShiftDistancePair(PrefabType.Floor, HorizontalShiftDistance.Half));
                break;
            case PrefabType.SideRoof:
                defaults.targetsWithShift.Add(new TargetPrefabShiftDistancePair(PrefabType.Beam, HorizontalShiftDistance.NegativeShorterHalf));
                break;
            case PrefabType.WallDecoration:
                defaults.targetsWithShift.Add(
                    new TargetPrefabShiftDistancePair(PrefabType.Wall, HorizontalShiftDistance.ShorterHalf));
                break;

        }
    }

    private void SetHorizontalFrontShiftDirection()
    {
        switch (defaults.prefabType)
        {
            default:
                defaults.horizontalFrontShiftDirection = HorizontalFrontShiftDirection.Forward; break;
        }
    }

    private void SetMyFrontShiftDistance()
    {
        switch (defaults.prefabType)
        {
            case PrefabType.Floor:
            case PrefabType.Stairs:
                defaults.myFrontShiftDistance = HorizontalShiftDistance.Half;
                break;
            case PrefabType.SideRoof:
                defaults.myFrontShiftDistance = HorizontalShiftDistance.ShorterHalf; break;
            default:
                defaults.myFrontShiftDistance = HorizontalShiftDistance.None;
                break;
        }
    }


    private void SetRotationType()
    {
        switch (defaults.prefabType)
        {

            case PrefabType.Floor: defaults.rotationType = RotationType.None; break;
            case PrefabType.Window: defaults.rotationType = RotationType.TargetRoot; break;
            default: defaults.rotationType = RotationType.TargetEdge; break;
        }
    }

    private void SetShiftSideways()
    {
        switch (defaults.prefabType)
        {
            case PrefabType.Beam:
                defaults.shiftSideways = true; break;
        }
    }

    private void SetTargetSideWaysShiftDistance()
    {
        switch (defaults.prefabType)
        {
            case PrefabType.Beam:
                defaults.targetSideWaysShiftDistance = HorizontalShiftDistance.Half; break;
        }
    }

    private void SetShiftDown()
    {
        switch (defaults.prefabType)
        {
            case PrefabType.Floor:
            case PrefabType.Seam:
                defaults.shiftDown = true;
                break;
            default:
                defaults.shiftDown = false;
                break;
        }
    }


}
