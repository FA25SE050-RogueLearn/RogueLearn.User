using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserProfiles.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandValidatorTests
{
    [Fact]
    public void Valid_Minimal_Command()
    {
        var avatarUrlValidator = Substitute.For<IAvatarUrlValidator>();
        var validator = new UpdateMyProfileCommandValidator(avatarUrlValidator);
        var cmd = new UpdateMyProfileCommand { AuthUserId = Guid.NewGuid() };
        var res = validator.TestValidate(cmd);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Fails_When_Both_Url_And_File_Provided()
    {
        var avatarUrlValidator = Substitute.For<IAvatarUrlValidator>();
        var validator = new UpdateMyProfileCommandValidator(avatarUrlValidator);
        var cmd = new UpdateMyProfileCommand { AuthUserId = Guid.NewGuid(), ProfileImageUrl = "http://x", ProfileImageBytes = new byte[10] };
        var res = validator.TestValidate(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Select(e => e.ErrorMessage).Should().Contain("Provide either ProfileImageUrl or an uploaded profileImage file, not both.");
    }

    [Fact]
    public void Passes_With_Allowed_ContentType()
    {
        var avatarUrlValidator = Substitute.For<IAvatarUrlValidator>();
        var validator = new UpdateMyProfileCommandValidator(avatarUrlValidator);
        var cmd = new UpdateMyProfileCommand { AuthUserId = Guid.NewGuid(), ProfileImageBytes = new byte[10], ProfileImageContentType = "image/png" };
        var res = validator.TestValidate(cmd);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Fails_With_Unsupported_Image_Type()
    {
        var avatarUrlValidator = Substitute.For<IAvatarUrlValidator>();
        var validator = new UpdateMyProfileCommandValidator(avatarUrlValidator);
        var cmd = new UpdateMyProfileCommand { AuthUserId = Guid.NewGuid(), ProfileImageBytes = new byte[10], ProfileImageContentType = "image/tiff", ProfileImageFileName = "avatar.tiff" };
        var res = validator.TestValidate(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Select(e => e.ErrorMessage).Should().Contain("Unsupported image type. Allowed: png, jpg/jpeg, webp, gif.");
    }

    [Fact]
    public void Fails_When_Image_Too_Large()
    {
        var avatarUrlValidator = Substitute.For<IAvatarUrlValidator>();
        var validator = new UpdateMyProfileCommandValidator(avatarUrlValidator);
        var cmd = new UpdateMyProfileCommand { AuthUserId = Guid.NewGuid(), ProfileImageBytes = new byte[5 * 1024 * 1024 + 1], ProfileImageContentType = "image/png" };
        var res = validator.TestValidate(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Select(e => e.ErrorMessage).Should().Contain("Image file too large (max 5MB).");
    }

    [Fact]
    public void Fails_When_AvatarUrl_Invalid()
    {
        var avatarUrlValidator = Substitute.For<IAvatarUrlValidator>();
        avatarUrlValidator.IsValid("http://invalid").Returns(false);
        var validator = new UpdateMyProfileCommandValidator(avatarUrlValidator);
        var cmd = new UpdateMyProfileCommand { AuthUserId = Guid.NewGuid(), ProfileImageUrl = "http://invalid" };
        var res = validator.TestValidate(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Select(e => e.ErrorMessage).Should().Contain("ProfileImageUrl must be a valid and allowed avatar URL");
    }

    [Fact]
    public void PreferencesJson_Valid_Object_Passes_And_Array_Fails()
    {
        var avatarUrlValidator = Substitute.For<IAvatarUrlValidator>();
        var validator = new UpdateMyProfileCommandValidator(avatarUrlValidator);
        var cmdOk = new UpdateMyProfileCommand { AuthUserId = Guid.NewGuid(), PreferencesJson = "{}" };
        var resOk = validator.TestValidate(cmdOk);
        resOk.IsValid.Should().BeTrue();

        var cmdBad = new UpdateMyProfileCommand { AuthUserId = Guid.NewGuid(), PreferencesJson = "[]" };
        var resBad = validator.TestValidate(cmdBad);
        resBad.IsValid.Should().BeFalse();
        resBad.Errors.Select(e => e.ErrorMessage).Should().Contain("PreferencesJson must be a valid JSON object");
    }

    [Fact]
    public void PreferencesJson_Malformed_Fails()
    {
        var avatarUrlValidator = Substitute.For<IAvatarUrlValidator>();
        var validator = new UpdateMyProfileCommandValidator(avatarUrlValidator);
        var cmdBad = new UpdateMyProfileCommand { AuthUserId = Guid.NewGuid(), PreferencesJson = "{invalid" };
        var resBad = validator.TestValidate(cmdBad);
        resBad.IsValid.Should().BeFalse();
        resBad.Errors.Select(e => e.ErrorMessage).Should().Contain("PreferencesJson must be a valid JSON object");
    }
}