using System.Collections.Generic;
using FluentAssertions;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;
using RogueLearn.User.Application.Models;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.CurriculumImport.Queries.ValidateSyllabus;

public class SyllabusDataValidatorTests
{
    [Fact]
    public void ValidSyllabus_Passes()
    {
        var syllabus = new SyllabusData
        {
            SubjectCode = "SC",
            SubjectName = "SN",
            Credits = 3,
            VersionNumber = 1,
            Content = new SyllabusContent
            {
                CourseDescription = "Desc",
                CourseLearningOutcomes = new List<CourseLearningOutcome> { new() { Id = "CLO1", Details = "Do X" } },
                SessionSchedule = new List<SyllabusSessionDto> { new() { SessionNumber = 1, Topic = "Intro", Activities = new List<string> { "Lecture" }, Readings = new List<string> { "Book" } } },
                ConstructiveQuestions = new List<ConstructiveQuestion> { new() { Name = "CQ1.1", Question = "Q?", SessionNumber = 1 } }
            }
        };
        var validator = new SyllabusDataValidator();
        var result = validator.Validate(syllabus);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvalidCLO_Fails()
    {
        var syllabus = new SyllabusData
        {
            SubjectCode = "SC",
            SubjectName = "SN",
            Credits = 3,
            VersionNumber = 1,
            Content = new SyllabusContent
            {
                CourseLearningOutcomes = new List<CourseLearningOutcome> { new() { Id = "", Details = "" } }
            }
        };
        var validator = new SyllabusDataValidator();
        var result = validator.Validate(syllabus);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ApprovedDate_Too_Old_Fails()
    {
        var syllabus = new SyllabusData
        {
            SubjectCode = "SC",
            VersionNumber = 1,
            Content = new SyllabusContent()
        };
        syllabus.ApprovedDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-11));
        var validator = new SyllabusDataValidator();
        validator.Validate(syllabus).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ApprovedDate_Too_Future_Fails()
    {
        var syllabus = new SyllabusData
        {
            SubjectCode = "SC",
            VersionNumber = 1,
            Content = new SyllabusContent()
        };
        syllabus.ApprovedDate = DateOnly.FromDateTime(DateTime.Now.AddYears(6));
        var validator = new SyllabusDataValidator();
        validator.Validate(syllabus).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SubjectCode_InvalidPattern_Fails()
    {
        var syllabus = new SyllabusData
        {
            SubjectCode = "bad code",
            VersionNumber = 1,
            Content = new SyllabusContent()
        };
        var validator = new SyllabusDataValidator();
        validator.Validate(syllabus).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Content_Null_Fails()
    {
        var syllabus = new SyllabusData
        {
            SubjectCode = "SC",
            VersionNumber = 1,
            Content = null!
        };
        var validator = new SyllabusDataValidator();
        validator.Validate(syllabus).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConstructiveQuestion_Invalid_Session_Fails()
    {
        var syllabus = new SyllabusData
        {
            SubjectCode = "SC",
            VersionNumber = 1,
            Content = new SyllabusContent
            {
                ConstructiveQuestions = new List<ConstructiveQuestion> { new() { Name = "n", Question = "q", SessionNumber = 0 } }
            }
        };
        var validator = new SyllabusDataValidator();
        validator.Validate(syllabus).IsValid.Should().BeFalse();
    }
}
