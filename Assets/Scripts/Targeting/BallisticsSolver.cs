using System;
using UnityEngine;

/// <summary>
/// Closed-form ballistic solver with light-weight adjustments for Unity physics drag/damping.
/// Uses the provided analytic solution (no iteration) and compensates for Rigidbody drag / linear damping
/// by scaling the effective horizontal distance and gravity terms.
/// </summary>
public static class BallisticsSolver
{
    const double DragDistanceScale = 0.5;   // heuristically reduces horizontal reach based on drag
    const double DragVerticalCoupling = 0.15; // shifts apex downward when drag is present
    const double Deg2Rad = Math.PI / 180.0;

    public static bool SolveWithUnityDrag(
        float horizontalDistance,
        float verticalOffset,
        float gravityMagnitude,
        float vMax,
        float thetaMaxRad,
        float rigidbodyDrag,
        float linearDamping,
        out float launchSpeed,
        out float launchAngleRad)
    {
        launchSpeed = 0f;
        launchAngleRad = 0f;

        if (horizontalDistance <= 0f || vMax <= 0f || thetaMaxRad <= 0f || thetaMaxRad >= Mathf.PI * 0.5f)
            return false;

        double R = Math.Max(0.0001, horizontalDistance);
        double h = verticalOffset;
        double g = Math.Max(0.0001, gravityMagnitude);
        double drag = Math.Max(0.0, rigidbodyDrag);
        double damping = Math.Max(0.0, linearDamping);

        // Apply mild adjustments so drag-heavy projectiles do not overshoot.
        double dragScale = 1.0 / (1.0 + drag * DragDistanceScale);
        double adjustedR = R * dragScale;
        double adjustedH = h - (drag * R * DragVerticalCoupling);
        double adjustedG = g * (1.0 + damping);

        return SolveAdaptive(adjustedR, adjustedH, adjustedG, vMax, thetaMaxRad, out launchSpeed, out launchAngleRad);
    }

    static bool SolveAdaptive(
        double R,
        double h,
        double g,
        float vMax,
        float thetaMaxRad,
        out float launchSpeed,
        out float launchAngleRad)
    {
        launchSpeed = 0f;
        launchAngleRad = 0f;

        if (R <= 0.0 || g <= 0.0)
            return false;

            // Aim slightly upward even for downhill shots so gravity can arc the projectile back.
            double thetaMin = Deg2Rad * 0.25; // 0.25° minimum
        double thetaMax = Math.Max(thetaMin, Math.Min(thetaMaxRad, Math.PI * 0.499));
        const int angleSamples = 72; // ~0.2° resolution for 15° cap

        bool found = false;
        double bestTheta = 0.0;
        double bestSpeed = double.MaxValue;

        for (int i = angleSamples; i >= 1; i--)
        {
            double t = thetaMin + (thetaMax - thetaMin) * i / angleSamples;
            double cos = Math.Cos(t);
            double cos2 = cos * cos;
            double tan = Math.Tan(t);
            double verticalTerm = R * tan - h;
            if (verticalTerm <= 0.0)
                continue;

            double denom = 2.0 * cos2 * verticalTerm;
            if (denom <= double.Epsilon)
                continue;

            double v2req = g * R * R / denom;
            if (v2req <= 0.0)
                continue;

            double vReq = Math.Sqrt(v2req);
            if (vReq <= vMax + 1e-3)
            {
                found = true;
                if (vReq < bestSpeed)
                {
                    bestSpeed = vReq;
                    bestTheta = t;
                }
            }
        }

        if (!found)
            return false;

        launchSpeed = (float)bestSpeed;
        launchAngleRad = (float)bestTheta;
        return true;
    }
}
