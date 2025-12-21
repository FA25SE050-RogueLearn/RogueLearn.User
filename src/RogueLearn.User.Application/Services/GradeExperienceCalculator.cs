namespace RogueLearn.User.Application.Services;

/// <summary>
/// This service implements the "Tiered Reward System" for calculating XP awards from academic grades.
/// It uses a "Per-Subject Contribution" model, where each subject acts as a self-contained pool of potential XP.
/// The tier of the subject determines the size of this pool. A skill's total XP is the sum of XP awarded
/// from all relevant subjects.
///
/// This is distinct from a "Cumulative Skill Cap" model where a skill itself has a total XP limit that
/// increases with each tier. The current model is simpler to implement and still effectively rewards
/// students more for mastering advanced subjects.
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
    // These constants define the maximum XP that a single subject can contribute to a skill,
    // based on the semester it belongs to. This creates the "Tiered Reward System".
    private const int Tier1Pool = 1500;  // Tier 1 (Foundation): For subjects in Semesters 1-3.
    private const int Tier2Pool = 2000;  // Tier 2 (Intermediate): For subjects in Semesters 4-6.
    private const int Tier3Pool = 2500;  // Tier 3 (Advanced): For subjects in Semester 7+, providing the highest reward.

    // A grade of 5.0 is the minimum passing grade required to earn substantial XP.
    private const double PassingGrade = 5.0;

    // A small, flat amount of XP awarded for subjects that were attempted but not passed.
    private const int ConsolationXp = 50;

    /// <summary>
    /// This is the core method where the XP calculation happens based on the Per-Subject Contribution model.
    /// </summary>
    public int CalculateXpAward(double grade, int semester, decimal relevanceWeight)
    {
        // First, determine the maximum XP pool for this subject based on its semester tier.
        int tierPool = GetTierPool(semester);

        // If the grade is below passing, award a small, fixed amount of XP for the effort.
        if (grade < PassingGrade)
        {
            return ConsolationXp;
        }

        // Convert the grade (e.g., 8.5) into a percentage (0.85).
        double gradePercent = grade / 10.0;

        // The final XP is calculated by multiplying the tier's max pool by the skill's relevance
        // to the subject, and then by the student's grade percentage.
        int earnedXp = (int)(tierPool * (double)relevanceWeight * gradePercent);

        return earnedXp;
    }

    /// <summary>
    /// A helper method to get the details of a tier based on the semester number.
    /// This is used for logging and can also be used to display information to the user.
    /// </summary>
    public (int Tier, int MaxPool, string Description) GetTierInfo(int semester)
    {
        return semester switch
        {
            <= 3 => (1, Tier1Pool, "Foundation (Semester 1-3)"),
            <= 6 => (2, Tier2Pool, "Intermediate (Semester 4-6)"),
            _ => (3, Tier3Pool, "Advanced (Semester 7+)")
        };
    }

    /// <summary>
    /// A private helper that returns the correct XP pool for a given semester.
    /// </summary>
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