using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}