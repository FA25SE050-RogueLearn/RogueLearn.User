using System;
using FluentAssertions;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

namespace RogueLearn.User.Application.Tests.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommandValidatorTests
{
    [Fact]
    public void Validator_Fails_When_MandatoryFieldsMissing()
    {
        var v = new IngestXpEventCommandValidator();
        var cmd = new IngestXpEventCommand { AuthUserId = Guid.Empty, SkillId = null, Points = 0, SourceService = "" };
        var res = v.Validate(cmd);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Succeeds_With_ValidPayload()
    {
        var v = new IngestXpEventCommandValidator();
        var cmd = new IngestXpEventCommand { AuthUserId = Guid.NewGuid(), SkillId = Guid.NewGuid(), Points = 10, SourceService = "svc", SourceType = "type", Reason = "r" };
        var res = v.Validate(cmd);
        res.IsValid.Should().BeTrue();
    }
}