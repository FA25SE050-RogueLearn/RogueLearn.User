using FluentAssertions;
using Microsoft.AspNetCore.Http;
using RogueLearn.User.Application.Features.LecturerVerificationRequests;

namespace RogueLearn.User.Application.Tests.Features.LecturerVerificationRequests;

public class CreateLecturerVerificationFormDataTests
{
    [Fact]
    public void Form_SetsFields()
    {
        var form = new CreateLecturerVerificationFormData
        {
            Email = "e@x.com",
            StaffId = "S",
            Screenshot = null
        };
        form.Email.Should().Be("e@x.com");
        form.StaffId.Should().Be("S");
        form.Screenshot.Should().BeNull();
    }
}