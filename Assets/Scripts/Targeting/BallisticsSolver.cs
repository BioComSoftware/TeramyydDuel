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

        double dragScale = 1.0 / (1.0 + drag * DragDistanceScale);
        double adjustedR = R * dragScale;
        double adjustedH = h - (drag * R * DragVerticalCoupling);
        double adjustedG = g * (1.0 + damping);

        return SolveFastCore(adjustedR, adjustedH, adjustedG, vMax, thetaMaxRad, out launchSpeed, out launchAngleRad);
    }

    static bool SolveFastCore(
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

        double tanThetaMax = Math.Tan(thetaMaxRad);
        double v2 = vMax * vMax;
        double gR = g * R;
        double disc = v2 * v2 - g * (g * R * R + 2.0 * h * v2);

        if (disc >= 0.0 && Math.Abs(gR) > double.Epsilon)
        {
            double sqrtDisc = Math.Sqrt(disc);
            double u1 = (v2 - sqrtDisc) / gR;
            double u2 = (v2 + sqrtDisc) / gR;

            bool u1Valid = (u1 > 0.0 && u1 <= tanThetaMax);
            bool u2Valid = (u2 > 0.0 && u2 <= tanThetaMax);

            if (u1Valid || u2Valid)
            {
                double u = u1Valid && u2Valid ? Math.Min(u1, u2) : (u1Valid ? u1 : u2);
                launchAngleRad = (float)Math.Atan(u);
                launchSpeed = vMax;
                return true;
            }
        }

        double cosMax = Math.Cos(thetaMaxRad);
        double cosMax2 = cosMax * cosMax;
        double denom = 2.0 * cosMax2 * (R * tanThetaMax - h);
        if (denom <= 0.0)
            return false;

        double v2req = g * R * R / denom;
        if (v2req <= 0.0)
            return false;

        double vReq = Math.Sqrt(v2req);
        if (vReq > vMax)
            return false;

        launchSpeed = (float)vReq;
        launchAngleRad = thetaMaxRad;
        return true;
    }
}
