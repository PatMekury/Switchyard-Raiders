using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    // Metres in the unscaled personal presentation. Authoring script reads these constants.
    // Changing dimensions requires regenerating assets and running RecessedTableTask1 checks.
    public static class TabletopViewContract
    {
        public const float OuterSize = 1.30f;
        public const float UsableLandSize = 1.05f;
        public const float SlabSize = 1.058f;
        public const float SlabHeight = .15f;
        public const float TerrainMountHeight = .885f;
        public const float TerrainDatum = .15f;
        public const float RimHeight = .165f;
        public const float CupLipHeight = .168f;
        public const float TrayBottom = -.035f;
        public const float NormalScale = 1f;
        // Calculated pre-redesign MR land (no observed session). Double BOTH axes first,
        // then preserve their area in the new square instead of stretching finished meshes.
        public const float PreviousMRLandWidth = 1.50f, PreviousMRLandDepth = 1.05f;
        public const float MRLinearEnlargement = 2f;
        public const float MinimumMRRimBelowEyes = .25f;
        public static float MRBaselineScale(float previousScale) => MRLinearEnlargement *
            Mathf.Sqrt(PreviousMRLandWidth*PreviousMRLandDepth) / UsableLandSize *
            (float.IsNaN(previousScale)||float.IsInfinity(previousScale)?1f:Mathf.Clamp(previousScale,.35f,1f));
        public const float NearEdgeDistance = .30f;
        public const float RimBelowEyes = .50f;
        public const float GameplayScale = 1f;
        public const float GameplayHeight = .154f;
        public const float MiniatureScale = .01f;
        // Compatibility for the existing authored-asset migration/check entry points.
        public const float PreservedTrainScale = MiniatureScale;
        public const float MaximumZoom = 6f;
        public const float ReviewIntermediateZoom = 3f;
        public const float RailRadius = .44f;
        public const float RailGauge = 1.435f * MiniatureScale;
        public const float RailHeadWidth = .165f * MiniatureScale;
        public const float RailHeadHeight = .003f;
        public const float RailRise = .0306f;
        public const float LocomotiveHalfLength = 7.4f * MiniatureScale;
        public const float WagonBodyLength = .049f;
        public const float WagonWidth = .026f;
        public const float CouplerHeight = 1.05f * MiniatureScale;
        public static float PlacementDistance => OuterSize * .5f * NormalScale + NearEdgeDistance;
        public static Vector2 UsableLandBounds => Vector2.one * UsableLandSize;
        public static Vector2 OuterBounds => Vector2.one * OuterSize;
    }
}
