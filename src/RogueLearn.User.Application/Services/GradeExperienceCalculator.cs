// RogueLearn.User/src/RogueLearn.User.Application/Services/GradeExperienceCalculator.cs
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Calculates XP awards from academic grades using a tiered cap system.
/// 
/// The tiered system prevents a perfect score in a beginner subject from 
/// making a user look like a master. Each subject tier contributes a limited
/// "pool" of XP to a skill.
/// 
/// Tier Caps (based on Subject.Semester):
/// - Tier 1 (Sem 1-3): Max 1500 XP contribution → Level 1.5
/// - Tier 2 (Sem 4-6): Max 2000 XP contribution → fills to Level 3.5
/// - Tier 3 (Sem 7+):  Max 1500 XP contribution → fills to Level 5
/// 
/// Formula: XP = TierPool × RelevanceWeight × (Grade / 10.0)
/// </summary>
public interface IGradeExperienceCalculator
{
    /// <summary>
    /// Calculates the XP award for a subject based on grade, semester tier, and skill relevance.
    /// </summary>
    /// <param name="grade">The student's grade (0.0 - 10.0 scale)</param>
    /// <param name="semester">The subject's semester (determines tier)</param>
    /// <param name="relevanceWeight">How relevant this subject is to the skill (0.0 - 1.0)</param>
    /// <returns>XP amount to award</returns>
    int CalculateXpAward(double grade, int semester, decimal relevanceWeight);

    /// <summary>
    /// Gets the tier information for a given semester.
    /// </summary>
    (int Tier, int MaxPool, string Description) GetTierInfo(int semester);
}

public class GradeExperienceCalculator : IGradeExperienceCalculator
{
    // XP pools per tier - these define how much XP each tier of subjects can contribute
    private const int Tier1Pool = 1500;  // Foundation subjects (Sem 1-3)
    private const int Tier2Pool = 2000;  // Intermediate subjects (Sem 4-6)
    private const int Tier3Pool = 1500;  // Advanced subjects (Sem 7+)

    // Minimum grade to earn meaningful XP (passing grade in Vietnam is 5.0)
    private const double PassingGrade = 5.0;
    
    // Consolation XP for attempting but failing
    private const int ConsolationXp = 50;

    public int CalculateXpAward(double grade, int semester, decimal relevanceWeight)
    {
        // Determine the XP pool based on semester tier
        int tierPool = GetTierPool(semester);

        // If failed (grade < 5.0), give small consolation XP for trying
        if (grade < PassingGrade)
        {
            return ConsolationXp;
        }

        // Calculate grade percentage (5.0 = 50%, 10.0 = 100%)
        double gradePercent = grade / 10.0;

        // Calculate XP: Pool × Relevance × Grade%
        int earnedXp = (int)(tierPool * (double)relevanceWeight * gradePercent);

        return earnedXp;
    }

    public (int Tier, int MaxPool, string Description) GetTierInfo(int semester)
    {
        return semester switch
        {
            <= 3 => (1, Tier1Pool, "Foundation (Semester 1-3)"),
            <= 6 => (2, Tier2Pool, "Intermediate (Semester 4-6)"),
            _ => (3, Tier3Pool, "Advanced (Semester 7+)")
        };
    }

    private static int GetTierPool(int semester)
    {
        return semester switch
        {
            <= 3 => Tier1Pool,
            <= 6 => Tier2Pool,
            _ => Tier3Pool
        };
    }
}
