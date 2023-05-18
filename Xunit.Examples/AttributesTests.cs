
using Tms.Adapter.Core.Attributes;
using Tms.Adapter.Core.Models;
using Tms.Adapter.XUnit.Attributes;

namespace XUnitTests;

public class AttributesTests
{
    [TmsFact]
    public void WithoutAttributes_Success()
    {
        Assert.True(true);
    }
    
    [TmsFact]
    public void WithoutAttributes_Failed()
    {
        Assert.True(false);
    }
    
    [ExternalId("ExternalIdAttribute_Success")]
    [TmsFact]
    public void ExternalIdAttribute_Success()
    {
        Assert.True(true);
    }
    
    [ExternalId("ExternalIdAttribute_Failed")]
    [TmsFact]
    public void ExternalIdAttribute_Failed()
    {
        Assert.True(false);
    }
    
    [DisplayName("DisplayNameAttribute_Success DisplayName")]
    [TmsFact]
    public void DisplayNameAttribute_Success()
    {
        Assert.True(true);
    }
    
    [DisplayName("DisplayNameAttribute_Failed DisplayName")]
    [TmsFact]
    public void DisplayNameAttribute_Failed()
    {
        Assert.True(false);
    }
    
    [Title("TitleAttribute_Success Title")]
    [TmsFact]
    public void TitleAttribute_Success()
    {
        Assert.True(true);
    }
    
    [Title("TitleAttribute_Failed Title")]
    [TmsFact]
    public void TitleAttribute_Failed()
    {
        Assert.True(false);
    }
    
    [Description("DescriptionAttribute_Success")]
    [TmsFact]
    public void DescriptionAttribute_Success()
    {
        Assert.True(true);
    }
    
    [Description("DescriptionAttribute_Failed")]
    [TmsFact]
    public void DescriptionAttribute_Failed()
    {
        Assert.True(false);
    }
    
    [Labels(new string[] { "Label1", "Label2" })]
    [TmsFact]
    public void LablesAttribute_Success()
    {
        Assert.True(true);
    }
    
    [Labels(new string[] { "Label1", "Label2" })]
    [TmsFact]
    public void LablesAttribute_Failed()
    {
        Assert.True(false);
    }

    [Links("https://test01.example", "Example01", "Example01 description", LinkType.Issue)]
    [Links("https://test02.example")]
    [TmsFact]
    public void LinksAttribute_Success()
    {
        Assert.True(true);
    }

    [Links("https://test01.example", "Example01", "Example01 description", LinkType.Issue)]
    [Links("https://test02.example")]
    [TmsFact]
    public void LinksAttribute_Failed()
    {
        Assert.True(false);
    }

    [WorkItemIds(new string[] { "123", "321" })]
    [TmsFact]
    public void WorkItemIdsAttribute_Success()
    {
        Assert.True(true);
    }

    [WorkItemIds(new string[] { "123", "321" })]
    [TmsFact]
    public void WorkItemIdsAttribute_Failed()
    {
        Assert.True(false);
    }

    [ExternalId("WithAllAttributes_Success")]
    [DisplayName("WithAllAttributes_Success DisplayName")]
    [Title("WithAllAttributes_Success Title")]
    [Description("WithAllAttributes_Success Description")]
    [Labels(new string[] { "Label1", "Label2" })]
    [Links("https://test01.example", "Example01", "Example01 description", LinkType.Issue)]
    [Links("https://test02.example")]
    [WorkItemIds(new string[] { "123", "321" })]
    [TmsFact]
    public void WithAllAttributes_Success()
    {
        Assert.True(true);
    }

    [ExternalId("WithAllAttributes_Failed")]
    [DisplayName("WithAllAttributes_Failed DisplayName")]
    [Title("WithAllAttributes_Failed Title")]
    [Description("WithAllAttributes_Failed Description")]
    [Labels(new string[] { "Label1", "Label2" })]
    [Links("https://test01.example", "Example01", "Example01 description", LinkType.Issue)]
    [Links("https://test02.example")]
    [WorkItemIds(new string[] { "123", "321" })]
    [TmsFact]
    public void WithAllAttributes_Failed()
    {
        Assert.True(false);
    }

    [InlineData(1, "string1")]
    [InlineData(2, "string2")]
    [InlineData(3, "string3")]
    [ExternalId("ParametrizedTest_Success_{number}_{str}")]
    [Title("ParametrizedTest_Success Title {number} {str}")]
    [DisplayName("ParametrizedTest_Success DisplayName {number} {str}")]
    [TmsTheory]
    public void ParametrizedTest_Success(int number, string str)
    {
        Assert.True(true);
    }
    
    [InlineData(1, "string1")]
    [InlineData(2, "string2")]
    [InlineData(3, "string3")]
    [ExternalId("ParametrizedTest_Failed_{number}_{str}")]
    [Title("ParametrizedTest_Failed Title {number} {str}")]
    [DisplayName("ParametrizedTest_Failed DisplayName {number} {str}")]
    [TmsTheory]
    public void ParametrizedTest_Failed(int number, string str)
    {
        Assert.True(false);
    }
}