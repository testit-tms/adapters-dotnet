#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tms.Adapter.Core.Utils;

namespace Tms.Adapter.CoreTests.Utils;

[TestClass]
public class HtmlEscapeUtilsTests
{
    [TestCleanup]
    public void TestCleanup()
    {
        Environment.SetEnvironmentVariable("NO_ESCAPE_HTML", null);
    }

    #region EscapeHtmlTags Tests

    [TestMethod]
    public void EscapeHtmlTags_NullInput_ReturnsNull()
    {
        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(null);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void EscapeHtmlTags_EmptyString_ReturnsEmptyString()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void EscapeHtmlTags_NoHtmlTags_ReturnsOriginalString()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);

        // Assert
        Assert.AreEqual("Hello World", result);
    }

    [TestMethod]
    public void EscapeHtmlTags_WithHtmlTags_EscapesTags()
    {
        // Arrange
        var input = "Hello <script>alert('XSS')</script> World";

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);

        // Assert
        Assert.AreEqual("Hello &lt;script&gt;alert('XSS')&lt;/script&gt; World", result);
    }

    [TestMethod]
    public void EscapeHtmlTags_OnlyLessThan_ReturnsUnchanged()
    {
        // Arrange
        var input = "Value < 10";
        
        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);
        
        // Assert - no HTML tags detected, should return unchanged
        Assert.AreEqual(input, result);
        Assert.AreSame(input, result); // Should return the same reference for efficiency
    }

    [TestMethod]
    public void EscapeHtmlTags_OnlyGreaterThan_ReturnsUnchanged()
    {
        // Arrange
        var input = "Value > 5";
        
        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);
        
        // Assert - no HTML tags detected, should return unchanged
        Assert.AreEqual(input, result);
        Assert.AreSame(input, result); // Should return the same reference for efficiency
    }

    [TestMethod]
    public void EscapeHtmlTags_AlreadyEscaped_DoesNotDoubleEscape()
    {
        // Arrange
        var input = "Hello \\<script\\>alert('XSS')\\</script\\> World";

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);

        // Assert
        Assert.AreEqual("Hello \\&lt;script\\&gt;alert('XSS')\\&lt;/script\\&gt; World", result);
    }

    [TestMethod]
    public void EscapeHtmlTags_PartiallyEscaped_DoesNotDoubleEscape()
    {
        // Arrange
        var input = "Hello \\<script>alert('XSS')</script> World";

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);

        // Assert - regex properly escapes only non-escaped characters
        Assert.AreEqual("Hello \\&lt;script&gt;alert('XSS')&lt;/script&gt; World", result);
    }

    [TestMethod]
    public void EscapeHtmlTags_MultipleHtmlTags_EscapesAll()
    {
        // Arrange
        var input = "<div><span>Test</span></div>";

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);

        // Assert
        Assert.AreEqual("&lt;div&gt;&lt;span&gt;Test&lt;/span&gt;&lt;/div&gt;", result);
    }

    [TestMethod]
    public void EscapeHtmlTags_RegexNegativeLookbehind_WorksCorrectly()
    {
        // Arrange
        var input = "Already \\<escaped\\> and <script>not escaped</script>";
        
        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);
        
        // Assert - only unescaped HTML tags should be escaped
        Assert.AreEqual("Already \\&lt;escaped\\&gt; and &lt;script&gt;not escaped&lt;/script&gt;", result);
    }

    [TestMethod]
    public void EscapeHtmlTags_WithComplexHtmlTag_ShouldEscapeCorrectly()
    {
        // Arrange
        var input = "<div class='test'>Content</div>";
        
        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);
        
        // Assert
        Assert.AreEqual("&lt;div class='test'&gt;Content&lt;/div&gt;", result);
    }

    [TestMethod]
    public void EscapeHtmlTags_WithPlainText_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "This is plain text without any HTML tags";
        
        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);
        
        // Assert
        Assert.AreEqual(input, result);
        Assert.AreSame(input, result); // Should return the same reference for efficiency
    }

    [TestMethod]
    public void EscapeHtmlTags_WithAngleBracketsButNoHtmlTags_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "Math expression: 5 < 10 and 20 > 15";
        
        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);
        
        // Assert
        Assert.AreEqual(input, result);
        Assert.AreSame(input, result); // Should return the same reference for efficiency
    }

    [TestMethod]
    public void EscapeHtmlTags_WithNumbersAndSymbols_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "Email: user@domain.com, Phone: +1-234-567-8900";
        
        // Act
        var result = HtmlEscapeUtils.EscapeHtmlTags(input);
        
        // Assert
        Assert.AreEqual(input, result);
        Assert.AreSame(input, result); // Should return the same reference for efficiency
    }

    #endregion

    #region EscapeHtmlInObject Tests

    [TestMethod]
    public void EscapeHtmlInObject_NullObject_ReturnsNull()
    {
        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject<TestModel>(null);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void EscapeHtmlInObject_SimpleObject_EscapesStringProperties()
    {
        // Arrange
        var model = new TestModel
        {
            Name = "Test<script>",
            Description = "Description with <b>bold</b> text",
            Number = 42
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert
        Assert.AreEqual("Test&lt;script&gt;", result.Name);
        Assert.AreEqual("Description with &lt;b&gt;bold&lt;/b&gt; text", result.Description);
        Assert.AreEqual(42, result.Number); // Numbers should not be affected
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithNullProperties_HandlesNullValues()
    {
        // Arrange
        var model = new TestModel
        {
            Name = null,
            Description = "Valid<tag>",
            Number = 10
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert
        Assert.IsNull(result.Name);
        Assert.AreEqual("Valid&lt;tag&gt;", result.Description);
        Assert.AreEqual(10, result.Number);
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithReadOnlyProperties_SkipsReadOnlyProperties()
    {
        // Arrange
        var model = new TestModelWithReadOnly("ReadOnly<script>");
        model.WritableProperty = "Writable<script>";

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert
        Assert.AreEqual("ReadOnly<script>", result.ReadOnlyProperty); // Should not be escaped
        Assert.AreEqual("Writable&lt;script&gt;", result.WritableProperty); // Should be escaped
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithListOfStrings_EscapesAllStrings()
    {
        // Arrange
        var model = new TestModelWithList
        {
            StringList = ["Item1<script>", "Item2<b>bold</b>", "Item3"]
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert
        Assert.AreEqual(3, result.StringList.Count);
        Assert.AreEqual("Item1&lt;script&gt;", result.StringList[0]);
        Assert.AreEqual("Item2&lt;b&gt;bold&lt;/b&gt;", result.StringList[1]);
        Assert.AreEqual("Item3", result.StringList[2]);
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithListOfObjects_EscapesObjectProperties()
    {
        // Arrange
        var model = new TestModelWithObjectList
        {
            ObjectList =
            [
                new() { Name = "Object1<script>", Description = "Desc1<b>" },
                new() { Name = "Object2<div>", Description = "Desc2<span>" }
            ]
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert
        Assert.AreEqual(2, result.ObjectList.Count);
        Assert.AreEqual("Object1&lt;script&gt;", result.ObjectList[0].Name);
        Assert.AreEqual("Desc1&lt;b&gt;", result.ObjectList[0].Description);
        Assert.AreEqual("Object2&lt;div&gt;", result.ObjectList[1].Name);
        Assert.AreEqual("Desc2&lt;span&gt;", result.ObjectList[1].Description);
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithEmptyList_DoesNotThrow()
    {
        // Arrange
        var model = new TestModelWithList
        {
            StringList = []
        };

        // Act & Assert
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.StringList.Count);
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithEscapingDisabled_DoesNotEscape()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NO_ESCAPE_HTML", "true");
        var model = new TestModel
        {
            Name = "Test<script>",
            Description = "Description with <b>bold</b> text"
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert
        Assert.AreEqual("Test<script>", result.Name);
        Assert.AreEqual("Description with <b>bold</b> text", result.Description);
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithEscapingDisabledCaseInsensitive_DoesNotEscape()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NO_ESCAPE_HTML", "TRUE");
        var model = new TestModel { Name = "Test<script>" };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert
        Assert.AreEqual("Test<script>", result.Name);
    }

    #endregion

    #region EscapeHtmlInObjectList Tests

    [TestMethod]
    public void EscapeHtmlInObjectList_NullList_ReturnsNull()
    {
        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObjectList<TestModel>(null);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void EscapeHtmlInObjectList_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var list = new List<TestModel>();

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObjectList(list);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void EscapeHtmlInObjectList_ListOfObjects_EscapesAllObjects()
    {
        // Arrange
        var list = new List<TestModel>
        {
            new() { Name = "Object1<script>", Description = "Desc1<b>" },
            new() { Name = "Object2<div>", Description = "Desc2<span>" },
            new() { Name = "Object3", Description = "Desc3<img>" }
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObjectList(list);

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("Object1&lt;script&gt;", result[0].Name);
        Assert.AreEqual("Desc1&lt;b&gt;", result[0].Description);
        Assert.AreEqual("Object2&lt;div&gt;", result[1].Name);
        Assert.AreEqual("Desc2&lt;span&gt;", result[1].Description);
        Assert.AreEqual("Object3", result[2].Name);
        Assert.AreEqual("Desc3&lt;img&gt;", result[2].Description);
    }

    [TestMethod]
    public void EscapeHtmlInObjectList_WithEscapingDisabled_DoesNotEscape()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NO_ESCAPE_HTML", "true");
        var list = new List<TestModel>
        {
            new() { Name = "Object1<script>", Description = "Desc1<b>" }
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObjectList(list);

        // Assert
        Assert.AreEqual("Object1<script>", result[0].Name);
        Assert.AreEqual("Desc1<b>", result[0].Description);
    }

    #endregion

    #region Nested Objects Tests

    [TestMethod]
    public void EscapeHtmlInObject_WithNestedObjects_EscapesNestedProperties()
    {
        // Arrange
        var model = new TestModelWithNested
        {
            Name = "Parent<script>",
            Nested = new TestModel
            {
                Name = "Nested<b>",
                Description = "Nested description<div>"
            }
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert
        Assert.AreEqual("Parent&lt;script&gt;", result.Name);
        Assert.AreEqual("Nested&lt;b&gt;", result.Nested.Name);
        Assert.AreEqual("Nested description&lt;div&gt;", result.Nested.Description);
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithComplexNestedStructure_EscapesAllLevels()
    {
        // Arrange
        var model = new TestModelWithComplexNesting
        {
            Name = "Root<script>",
            NestedList =
            [
                new()
                {
                    Name = "ListItem1<b>",
                    Nested = new TestModel { Name = "DeepNested<span>", Description = "Deep<div>" }
                }
            ]
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert
        Assert.AreEqual("Root&lt;script&gt;", result.Name);
        Assert.AreEqual("ListItem1&lt;b&gt;", result.NestedList[0].Name);
        Assert.AreEqual("DeepNested&lt;span&gt;", result.NestedList[0].Nested.Name);
        Assert.AreEqual("Deep&lt;div&gt;", result.NestedList[0].Nested.Description);
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithSimpleTypes_IgnoresPrimitiveTypes()
    {
        // Arrange
        var model = new TestModelWithSimpleTypes
        {
            StringProperty = "Test<script>",
            BoolProperty = true,
            ByteProperty = 255,
            CharProperty = '<',
            ShortProperty = 32767,
            IntProperty = 2147483647,
            LongProperty = 9223372036854775807,
            FloatProperty = 3.14f,
            DoubleProperty = 3.14159265359,
            DecimalProperty = 123.456m,
            DateTimeProperty = DateTime.Now,
            DateTimeOffsetProperty = DateTimeOffset.Now,
            TimeSpanProperty = TimeSpan.FromHours(1),
            GuidProperty = Guid.NewGuid(),
            UriProperty = new Uri("https://example.com"),
            // Nullable wrappers
            NullableBoolProperty = false,
            NullableByteProperty = 128,
            NullableCharProperty = '>',
            NullableShortProperty = 16383,
            NullableIntProperty = 42,
            NullableLongProperty = 123456789L,
            NullableFloatProperty = 2.71f,
            NullableDoubleProperty = 2.718281828,
            NullableDecimalProperty = 456.789m,
            NullableDateTimeProperty = DateTime.Now.AddDays(1),
            NullableDateTimeOffsetProperty = DateTimeOffset.Now.AddHours(1),
            NullableTimeSpanProperty = TimeSpan.FromMinutes(30),
            NullableGuidProperty = Guid.NewGuid()
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert - only string property should be escaped
        Assert.AreEqual("Test&lt;script&gt;", result.StringProperty);
        Assert.IsTrue(result.BoolProperty);
        Assert.AreEqual(255, result.ByteProperty);
        Assert.AreEqual('<', result.CharProperty);
        Assert.AreEqual(32767, result.ShortProperty);
        Assert.AreEqual(2147483647, result.IntProperty);
        Assert.AreEqual(9223372036854775807, result.LongProperty);
        Assert.AreEqual(3.14f, result.FloatProperty);
        Assert.AreEqual(3.14159265359, result.DoubleProperty);
        Assert.AreEqual(123.456m, result.DecimalProperty);
        Assert.IsNotNull(result.DateTimeProperty);
        Assert.IsNotNull(result.DateTimeOffsetProperty);
        Assert.AreEqual(TimeSpan.FromHours(1), result.TimeSpanProperty);
        Assert.IsNotNull(result.GuidProperty);
        Assert.IsNotNull(result.UriProperty);
        // Nullable wrappers should remain unchanged
        Assert.AreEqual(false, result.NullableBoolProperty);
        Assert.AreEqual((byte)128, result.NullableByteProperty);
        Assert.AreEqual('>', result.NullableCharProperty);
        Assert.AreEqual((short)16383, result.NullableShortProperty);
        Assert.AreEqual(42, result.NullableIntProperty);
        Assert.AreEqual(123456789L, result.NullableLongProperty);
        Assert.AreEqual(2.71f, result.NullableFloatProperty);
        Assert.AreEqual(2.718281828, result.NullableDoubleProperty);
        Assert.AreEqual(456.789m, result.NullableDecimalProperty);
        Assert.IsNotNull(result.NullableDateTimeProperty);
        Assert.IsNotNull(result.NullableDateTimeOffsetProperty);
        Assert.AreEqual(TimeSpan.FromMinutes(30), result.NullableTimeSpanProperty);
        Assert.IsNotNull(result.NullableGuidProperty);
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithEnumProperty_IgnoresEnum()
    {
        // Arrange
        var model = new TestModelWithEnum
        {
            Name = "Test<script>",
            Status = TestStatus.Active
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert
        Assert.AreEqual("Test&lt;script&gt;", result.Name);
        Assert.AreEqual(TestStatus.Active, result.Status); // Enum should remain unchanged
    }

    [TestMethod]
    public void EscapeHtmlInObject_WithNullableTypesSetToNull_HandlesNullValues()
    {
        // Arrange
        var model = new TestModelWithSimpleTypes
        {
            StringProperty = "Test<script>",
            // All Nullable properties remain null by default
            NullableBoolProperty = null,
            NullableByteProperty = null,
            NullableCharProperty = null,
            NullableShortProperty = null,
            NullableIntProperty = null,
            NullableLongProperty = null,
            NullableFloatProperty = null,
            NullableDoubleProperty = null,
            NullableDecimalProperty = null,
            NullableDateTimeProperty = null,
            NullableDateTimeOffsetProperty = null,
            NullableTimeSpanProperty = null,
            NullableGuidProperty = null
        };

        // Act
        var result = HtmlEscapeUtils.EscapeHtmlInObject(model);

        // Assert - only string property should be escaped, null values should remain null
        Assert.AreEqual("Test&lt;script&gt;", result.StringProperty);
        Assert.IsNull(result.NullableBoolProperty);
        Assert.IsNull(result.NullableByteProperty);
        Assert.IsNull(result.NullableCharProperty);
        Assert.IsNull(result.NullableShortProperty);
        Assert.IsNull(result.NullableIntProperty);
        Assert.IsNull(result.NullableLongProperty);
        Assert.IsNull(result.NullableFloatProperty);
        Assert.IsNull(result.NullableDoubleProperty);
        Assert.IsNull(result.NullableDecimalProperty);
        Assert.IsNull(result.NullableDateTimeProperty);
        Assert.IsNull(result.NullableDateTimeOffsetProperty);
        Assert.IsNull(result.NullableTimeSpanProperty);
        Assert.IsNull(result.NullableGuidProperty);
    }

    #endregion

    #region Test Models

    public class TestModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Number { get; set; }
    }

    public class TestModelWithReadOnly
    {
        public TestModelWithReadOnly(string readOnlyValue)
        {
            ReadOnlyProperty = readOnlyValue;
        }

        public string ReadOnlyProperty { get; }
        public string? WritableProperty { get; set; }
    }

    public class TestModelWithList
    {
        public List<string> StringList { get; set; } = [];
    }

    public class TestModelWithObjectList
    {
        public List<TestModel> ObjectList { get; set; } = [];
    }

    public class TestModelWithNested
    {
        public string? Name { get; set; }
        public TestModel? Nested { get; set; }
    }

    public class TestModelWithComplexNesting
    {
        public string? Name { get; set; }
        public List<TestModelWithNested> NestedList { get; set; } = [];
    }

    public class TestModelWithSimpleTypes
    {
        public string? StringProperty { get; set; }
        public bool BoolProperty { get; set; }
        public byte ByteProperty { get; set; }
        public char CharProperty { get; set; }
        public short ShortProperty { get; set; }
        public int IntProperty { get; set; }
        public long LongProperty { get; set; }
        public float FloatProperty { get; set; }
        public double DoubleProperty { get; set; }
        public decimal DecimalProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public DateTimeOffset DateTimeOffsetProperty { get; set; }
        public TimeSpan TimeSpanProperty { get; set; }
        public Guid GuidProperty { get; set; }
        public Uri? UriProperty { get; set; }
        // Nullable wrappers 
        public bool? NullableBoolProperty { get; set; }
        public byte? NullableByteProperty { get; set; }
        public char? NullableCharProperty { get; set; }
        public short? NullableShortProperty { get; set; }
        public int? NullableIntProperty { get; set; }
        public long? NullableLongProperty { get; set; }
        public float? NullableFloatProperty { get; set; }
        public double? NullableDoubleProperty { get; set; }
        public decimal? NullableDecimalProperty { get; set; }
        public DateTime? NullableDateTimeProperty { get; set; }
        public DateTimeOffset? NullableDateTimeOffsetProperty { get; set; }
        public TimeSpan? NullableTimeSpanProperty { get; set; }
        public Guid? NullableGuidProperty { get; set; }
    }

    public class TestModelWithEnum
    {
        public string? Name { get; set; }
        public TestStatus Status { get; set; }
    }

    public enum TestStatus
    {
        Active,
        Inactive,
        Pending
    }

    #endregion
} 