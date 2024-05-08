using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using NUnit.Framework;
using Testura.Code.Builders;
using Testura.Code.Models.Options;
using Testura.Code.Saver;

namespace Testura.Code.Tests.Saver;

[TestFixture]
public class CodeSaverTests
{
    private CodeSaver _coderSaver;

    [OneTimeSetUp]
    public void SetUp()
    {
        _coderSaver = new CodeSaver();
    }

    [Test]
    public void SaveCodeAsString_WhenSavingCodeAsString_ShouldGetString()
    {
        var expected = """
                       namespace test {
                           public class TestClass {
                           }
                       }
                       """;
        var actual = _coderSaver.SaveCodeAsString(new ClassBuilder("TestClass", "test").Build());
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.RemoveWhitespace(), actual.RemoveWhitespace());
    }

    [Test]
    public void SaveCodeAsString_WhenSavingCodeAsStringAndOptions_ShouldGetString()
    {
        var expected = """
                       namespace test {
                           public class TestClass {
                               void MyMethod() {
                               }
                           }
                       }
                       """;
        var codeSaver = new CodeSaver(new List<OptionKeyValue> { new OptionKeyValue(CSharpFormattingOptions.NewLinesForBracesInMethods, false) });
        var actual = codeSaver.SaveCodeAsString(
            new ClassBuilder("TestClass", "test")
                .WithMethods(
                    new MethodBuilder("MyMethod")
                        .Build())
                .Build());
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.RemoveWhitespace(), actual.RemoveWhitespace());
    }
}
