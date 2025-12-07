using System.Linq;
using FluentAssertions;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Tests.Services;

public class SearchQueryBuilderTests
{
    [Fact]
    public void BuildQueryVariants_Programming_WithContext_Includes_Context_Tokens()
    {
        var list = SearchQueryBuilder.BuildQueryVariants("RecyclerView", "Android Kotlin", SubjectCategory.Programming);
        list.Should().NotBeEmpty();
        list.First().ToLowerInvariant().Should().Contain("android");
    }

    [Fact]
    public void BuildQueryVariants_ComputerScience_Vietnamese_Topic_Contains_Vietnamese_Phrase()
    {
        var list = SearchQueryBuilder.BuildQueryVariants("Thu thập yêu cầu", null, SubjectCategory.ComputerScience);
        list.Should().Contain(x => x.Contains("tài liệu học tập"));
    }

    [Fact]
    public void BuildContextAwareQuery_Default_General_Uses_Guide_Tutorial()
    {
        var q = SearchQueryBuilder.BuildContextAwareQuery("Binary trees", null, SubjectCategory.General);
        q.ToLowerInvariant().Should().Contain("guide");
    }

    [Fact]
    public void BuildQueryVariants_Programming_Vietnamese_Diacritics_Includes_Vietnamese_Variant()
    {
        var list = SearchQueryBuilder.BuildQueryVariants("Thu thập yêu cầu", "Android", SubjectCategory.Programming);
        list.Should().Contain(x => x.Contains("hướng dẫn lập trình"));
    }

    [Fact]
    public void BuildQueryVariants_General_NoDiacritics_English_Variant()
    {
        var list = SearchQueryBuilder.BuildQueryVariants("binary trees", null, SubjectCategory.General);
        list.Should().Contain(x => x.ToLowerInvariant().Contains("guide"));
    }

    [Fact]
    public void BuildContextAwareQuery_ByCategory_Mappings()
    {
        var prog = SearchQueryBuilder.BuildContextAwareQuery("RecyclerView", "Android", SubjectCategory.Programming);
        var cs = SearchQueryBuilder.BuildContextAwareQuery("Cache memory", null, SubjectCategory.ComputerScience);
        var pol = SearchQueryBuilder.BuildContextAwareQuery("Tư tưởng Hồ Chí Minh", null, SubjectCategory.VietnamesePolitics);
        var his = SearchQueryBuilder.BuildContextAwareQuery("Chiến tranh", null, SubjectCategory.History);
        var lit = SearchQueryBuilder.BuildContextAwareQuery("Ngữ văn", null, SubjectCategory.VietnameseLiterature);
        var sci = SearchQueryBuilder.BuildContextAwareQuery("Toán học", null, SubjectCategory.Science);
        var bus = SearchQueryBuilder.BuildContextAwareQuery("Kinh tế vi mô", null, SubjectCategory.Business);
        prog.ToLowerInvariant().Should().Contain("tutorial");
        cs.ToLowerInvariant().Should().Contain("guide");
        pol.Should().Contain("lý thuyết");
        his.Should().Contain("tài liệu lịch sử");
        lit.Should().Contain("bài tập trắc nghiệm");
        sci.Should().Contain("lý thuyết");
        bus.Should().Contain("bài giảng");
    }

    [Fact]
    public void BuildQueryVariants_Requirements_Topics_Map_Core_Phrases()
    {
        var list1 = SearchQueryBuilder.BuildQueryVariants("Use case template", null, SubjectCategory.ComputerScience);
        var list2 = SearchQueryBuilder.BuildQueryVariants("Vision and scope document", null, SubjectCategory.ComputerScience);
        var list3 = SearchQueryBuilder.BuildQueryVariants("Requirements prioritization techniques", null, SubjectCategory.ComputerScience);
        var list4 = SearchQueryBuilder.BuildQueryVariants("Requirements reuse", null, SubjectCategory.ComputerScience);
        var list5 = SearchQueryBuilder.BuildQueryVariants("Business analyst", null, SubjectCategory.ComputerScience);
        list1.Should().Contain(x => x.ToLowerInvariant().Contains("use case"));
        list2.Should().Contain(x => x.ToLowerInvariant().Contains("vision and scope"));
        list3.Should().Contain(x => x.ToLowerInvariant().Contains("requirements prioritization"));
        list4.Should().Contain(x => x.ToLowerInvariant().Contains("requirements reuse"));
        list5.Should().Contain(x => x.ToLowerInvariant().Contains("business analyst"));
    }

    [Fact]
    public void BuildQueryVariants_Requirements_Other_Keys_Map_Core()
    {
        var list1 = SearchQueryBuilder.BuildQueryVariants("Verification of requirements", null, SubjectCategory.ComputerScience);
        var list2 = SearchQueryBuilder.BuildQueryVariants("Elicitation techniques", null, SubjectCategory.ComputerScience);
        var list3 = SearchQueryBuilder.BuildQueryVariants("Stakeholder analysis", null, SubjectCategory.ComputerScience);
        list1.Should().Contain(x => x.ToLowerInvariant().Contains("requirements validation") || x.ToLowerInvariant().Contains("verification"));
        list2.Should().Contain(x => x.ToLowerInvariant().Contains("requirements elicitation"));
        list3.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildContextAwareQuery_Default_Vietnamese_Phrases_Detected()
    {
        var q1 = SearchQueryBuilder.BuildContextAwareQuery("được sử dụng trong hệ thống", null, SubjectCategory.General);
        var q2 = SearchQueryBuilder.BuildContextAwareQuery("cấu trúc trong phần mềm", null, SubjectCategory.General);
        q1.Should().Contain("tài liệu học tập");
        q2.Should().Contain("tài liệu học tập");
    }

    [Fact]
    public void BuildQueryVariants_VietnameseCategories_Diacritics_Use_Vietnamese_Phrase()
    {
        var pol = SearchQueryBuilder.BuildQueryVariants("Tư tưởng Hồ Chí Minh", null, SubjectCategory.VietnamesePolitics);
        var his = SearchQueryBuilder.BuildQueryVariants("Chiến tranh", null, SubjectCategory.History);
        var lit = SearchQueryBuilder.BuildQueryVariants("Ngữ văn", null, SubjectCategory.VietnameseLiterature);
        var sci = SearchQueryBuilder.BuildQueryVariants("Toán học", null, SubjectCategory.Science);
        var bus = SearchQueryBuilder.BuildQueryVariants("Kinh tế", null, SubjectCategory.Business);
        pol.Should().Contain(x => x.Contains("tài liệu bài giảng"));
        his.Should().Contain(x => x.Contains("tài liệu bài giảng"));
        lit.Should().Contain(x => x.Contains("tài liệu bài giảng"));
        sci.Should().Contain(x => x.Contains("tài liệu bài giảng"));
        bus.Should().Contain(x => x.Contains("tài liệu bài giảng"));
    }

    [Fact]
    public void BuildQueryVariants_NonVietnameseCategories_NoDiacritics_Use_English_Phrase()
    {
        var pol = SearchQueryBuilder.BuildQueryVariants("Ho Chi Minh thought", null, SubjectCategory.VietnamesePolitics);
        var his = SearchQueryBuilder.BuildQueryVariants("War", null, SubjectCategory.History);
        var lit = SearchQueryBuilder.BuildQueryVariants("Literature", null, SubjectCategory.VietnameseLiterature);
        var sci = SearchQueryBuilder.BuildQueryVariants("Mathematics", null, SubjectCategory.Science);
        var bus = SearchQueryBuilder.BuildQueryVariants("Economics", null, SubjectCategory.Business);
        pol.Should().Contain(x => x.ToLowerInvariant().Contains("study materials"));
        his.Should().Contain(x => x.ToLowerInvariant().Contains("study materials"));
        lit.Should().Contain(x => x.ToLowerInvariant().Contains("study materials"));
        sci.Should().Contain(x => x.ToLowerInvariant().Contains("study materials"));
        bus.Should().Contain(x => x.ToLowerInvariant().Contains("study materials"));
    }

    [Fact]
    public void BuildContextAwareQuery_Default_Vietnamese_Topic_Uses_Vietnamese_Phrase()
    {
        var q = SearchQueryBuilder.BuildContextAwareQuery("Thu thập yêu cầu và xác thực", null, SubjectCategory.General);
        q.ToLowerInvariant().Should().Contain("tài liệu học tập");
    }

    [Fact]
    public void BuildQueryVariants_ComputerScience_Requirements_Includes_SE_Phrases()
    {
        var list = SearchQueryBuilder.BuildQueryVariants("Requirements validation checklist", null, SubjectCategory.ComputerScience);
        list.Should().Contain(x => x.ToLowerInvariant().Contains("software requirements engineering"));
        list.Should().Contain(x => x.ToLowerInvariant().Contains("requirements validation"));
    }

    [Fact]
    public void BuildQueryVariants_AcceptanceCriteria_Maps_Core_Phrase()
    {
        var list = SearchQueryBuilder.BuildQueryVariants("Acceptance criteria examples", null, SubjectCategory.ComputerScience);
        list.Should().Contain(x => x.ToLowerInvariant().Contains("acceptance criteria"));
    }
}
