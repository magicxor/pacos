using Pacos.Extensions;
using Pacos.Services;

namespace Pacos.UnitTests;

public class UnitTests
{
    [SetUp]
    public void Setup()
    {
    }

    // remove <br>
    [TestCase(" No no, I will not do that! <br>", "No no, I will not do that!")]
    // remove hashtags after the message
    [TestCase(
@" Blah blah...

#Closing chat",
"Blah blah...")]
    // !", .", ?", !), .), ?) should remain intact in the end of the message
    [TestCase(@"He said: ""Hey!""", @"He said: ""Hey!""")]
    [TestCase(@"Blah blah. He said: ""Hey!""", @"Blah blah. He said: ""Hey!""")]
    [TestCase(@"Blah blah. He said: ""Hey!"" (Sorry, this is a joke.)", @"Blah blah. He said: ""Hey!"" (Sorry, this is a joke.)")]
    // remove hashtags after the message; trim the message
    [TestCase(
@"
Blah blah.
Blah blah.
#1_Users #1_Pacos",
@"Blah blah.
Blah blah.")]
    // code snippets should remain intact
    [TestCase(
@"class Program
{
    static void Main()
    {
        do
        {

        } while (true);
    }
}",
@"class Program
{
    static void Main()
    {
        do
        {

        } while (true);
    }
}")]
    // "Q:", "A:", "Question:", "Answer:" should remain intact
    [TestCase(
@"Q: How are you?
A: Fine, thanks.",
@"Q: How are you?
A: Fine, thanks.")]
    // remove "/*"
    [TestCase(
@"

/*
This is a conversation between User and Pacos.",
@"This is a conversation between User and Pacos.")]
    public void OutputTransformationTest(string source, string expectedResult)
    {
        var actualResult = OutputTransformation.Transform(source);
        Assert.That(actualResult, Is.EqualTo(expectedResult));
    }

    [TestCase("", 1, "")]
    [TestCase("a", 1, "a")]
    [TestCase("a", 2, "a")]
    [TestCase("a", 3, "a")]
    [TestCase("abc", 1, "a")]
    [TestCase("abc", 2, "ab")]
    [TestCase("abc", 3, "abc")]
    [TestCase("abc", 4, "abc")]
    [TestCase("abc", 999, "abc")]
    // throw ArgumentOutOfRangeException:
    [TestCase("a", -2, "a")]
    [TestCase("", -1, "")]
    [TestCase("a", 0, "a")]
    public void TestStringExtensionsCut(string source, int maxLength, string expectedResult)
    {
        var func = () => source.Cut(maxLength);
        if (maxLength <= 0)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => func());
        }
        else
        {
            var actualResult = func();
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }
    }

    /*
    public void TestStringExtensionsLastIndexOfAny(string source, string expectedResult)
    {

    }
    */
}
