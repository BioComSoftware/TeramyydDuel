using UnityEngine;

public static class CrewSkillUtility
{
    public static float EvaluateAccuracyScale(float skillLevel)
    {
        if (skillLevel <= 1f)
            return 1f;

        float clamped = Mathf.Clamp(skillLevel, 1f, 10f);
        if (clamped >= 7f)
        {
            float t = (clamped - 7f) / 3f; // 7 → 0, 10 → 1
            return Mathf.Lerp(0.25f, 0f, t);
        }

        if (clamped >= 5f)
        {
            float t = (clamped - 5f) / 2f; // 5 → 0, 7 → 1
            return Mathf.Lerp(0.5f, 0.25f, t);
        }

        float lowT = (clamped - 1f) / 4f; // 1 → 0, 5 → 1
        return Mathf.Lerp(1f, 0.5f, lowT);
    }

    public static CrewSkill GetDominantSkill(CrewMember crew)
    {
        if (crew == null)
            return CrewSkill.None;

        CrewSkill bestSkill = CrewSkill.Gunnery;
        float bestValue = crew.GetSkillLevel(bestSkill);

        TryPromote(ref bestSkill, ref bestValue, CrewSkill.Navigation, crew);
        TryPromote(ref bestSkill, ref bestValue, CrewSkill.Repair, crew);
        TryPromote(ref bestSkill, ref bestValue, CrewSkill.PowerEngineering, crew);
        TryPromote(ref bestSkill, ref bestValue, CrewSkill.LiftEngineering, crew);

        return bestSkill;
    }

    public static string GetShortLabel(CrewSkill skill)
    {
        switch (skill)
        {
            case CrewSkill.Gunnery: return "GUN";
            case CrewSkill.Navigation: return "NAV";
            case CrewSkill.Repair: return "REP";
            case CrewSkill.PowerEngineering: return "PWR";
            case CrewSkill.LiftEngineering: return "LIFT";
            default: return "--";
        }
    }

    public static string BuildStatSummary(CrewMember crew)
    {
        if (crew == null)
            return string.Empty;

        return $"G {crew.gunnery:0.0}  N {crew.navigation:0.0}\nR {crew.repair:0.0}  P {crew.powerEngineering:0.0}  L {crew.liftEngineering:0.0}";
    }

    static void TryPromote(ref CrewSkill current, ref float currentValue, CrewSkill candidate, CrewMember crew)
    {
        float value = crew.GetSkillLevel(candidate);
        if (value > currentValue + 0.01f || (Mathf.Approximately(value, currentValue) && candidate < current))
        {
            currentValue = value;
            current = candidate;
        }
    }
}
