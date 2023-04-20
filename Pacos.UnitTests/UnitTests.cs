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
    // if the bot reply starts with a bot mention, remove it
    [TestCase(
        @"

Pacos: Sorry, I do not understand your request. Please try again.
User: How can one make money online?
Pacos: There are numerous ways to make money online. One could start an eCommerce business selling physical products or services through an online store. Another option is to create a website with advertisements which generate revenue when visitors click on them. Affiliate marketing is another popular way to earn income online. You can also offer freelance services such as",
        @"Sorry, I do not understand your request. Please try again.")]
    // reply is empty
    [TestCase(
        @"
Пользователь: what is the difference between a computer and an algorithm
Пакос: A computer is a physical device that can store and process data according to instructions given to it. An algorithm is a set of rules for solving a problem or performing a task. The computer follows the instructions of the algorithm in order to solve the problem or perform the task.
Пользователь: How many days are there in a year?
Пакос: There are 365 days in a le",
        @"🤔")]
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

    [TestCase(new []{"a", "b", "c"}, "abc", 2, "c")]
    [TestCase(new []{"a.", "b.", "c."}, "abc.", 2, "c.")]
    [TestCase(new []{@".""", @"!""", @"?"""}, @"Ha-ha-ha. Funny...""", 17, @".""")]
    [TestCase(new []{"a", "b", "c"}, "def", -1, null)]
    [TestCase(new []{@".""", @"!""", @"?"""}, @"Ha-ha-ha. Funny...", -1, null)]
    [TestCase(new []{"aa", "bb", "cc"}, "abbc", 1, "bb")]
    public void TestStringExtensionsLastIndexOfAny(string [] patterns, string source, int expectedLastIndex, string? expectedValue)
    {
        var (actualLastIndex, actualValue) = source.LastIndexOfAny(patterns);
        Assert.Multiple(() =>
        {
            Assert.That(actualLastIndex, Is.EqualTo(expectedLastIndex));
            Assert.That(actualValue, Is.EqualTo(expectedValue));
        });
    }
}
