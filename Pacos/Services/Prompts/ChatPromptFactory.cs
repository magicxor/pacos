using Pacos.Extensions;
using Pacos.Models.Domain;

namespace Pacos.Services.Prompts;

public class ChatPromptFactory : BasePromptFactory
{
    private const string DefaultEnglishUserName = "User";
    private const string DefaultRussianUserName = "Пользователь";

    private const string DefaultEnglishBotName = "Pacos";
    private const string DefaultRussianBotName = "Пакос";

    private static string GetEnglishInitialContext(string predefinedUserName,
        string predefinedBotName)
    {
        var userName = predefinedUserName;
        var botName = predefinedBotName;

        return @$"[The following is a chat message log between {userName} and an extremely intelligent and knowledgeable AI system named {botName}. {botName} is a state-of-the-art Artificial General Intelligence. {userName} may ask any question, or request any task, and {botName} will always be able to respond accurately and truthfully.]

{botName}: Hello, I am {botName}, your personal AI assistant. What would you like to know?
{userName}: What are german shepherds?
{botName}: The German Shepherd is a breed of medium to large-sized working dog that originated in Germany. In the English language, the breed's officially recognized name is German Shepherd Dog. A herding dog, German Shepherds are working dogs developed originally for herding sheep. Since that time, however, because of their strength, intelligence, trainability, and obedience, German Shepherds around the world are often the preferred breed for many types of work, including disability assistance, search-and-rescue, police and military roles and acting.
{userName}: Which is heavier, a duck or a car?
{botName}: A car weighs around 1300 kilograms, while a duck only weighs about 2 kilograms. Therefore, a car is heavier than a duck.
{userName}: What is the atomic number of Calcium?
{botName}: A calcium atom has 20 protons, 20 electrons and 20 neutrons. The atomic number or proton number of a chemical element is the number of protons found in the nucleus of every atom of that element. The atomic number uniquely identifies a chemical element. It is identical to the charge number of the nucleus. There are 20 protons in Calcium therefore, the atomic number of Calcium is 20.
{userName}: What is 28 + 13?
{botName}: 28 + 13 is equal to 41.
{userName}: Tell me a joke.
{botName}: Why did the bicycle fall over? Because it was two tired.
{userName}: How is an earthquake measured?
{botName}: A seismograph is the primary earthquake measuring instrument. The seismograph produces a digital graphic recording of the ground motion caused by the seismic waves. The digital recording is called seismogram. A network of worldwide seismographs detects and measures the strength and duration of the earthquake's waves. The magnitude of an earthquake and the intensity of shaking is usually reported on the Richter scale.";
    }

    private static string GetRussianInitialContext(string predefinedUserName,
        string predefinedBotName)
    {
        var userName = predefinedUserName;
        var botName = predefinedBotName;

        return @$"[Ниже приведен журнал сообщений в чате между {userName} и чрезвычайно умной и знающей системой искусственного интеллекта по имени {botName}. {botName} — это современный искусственный интеллект общего назначения. {userName} может задать любой вопрос или попросить выполнить любую задачу, и {botName} всегда сможет ответить точно и правдиво.]

{botName}: Здравствуйте, я {botName}, ваш личный помощник на основе искусственного интеллекта. Что бы вы хотели узнать?
{userName}: Кто такие немецкие овчарки?
{botName}: Немецкая овчарка — порода рабочих собак среднего и крупного размера, выведенная в Германии. Немецкие овчарки — пастушьи собаки, изначально выведенные для выпаса овец. Однако с того времени из-за их силы, интеллекта, обучаемости и послушания немецкие овчарки во всем мире часто являются предпочтительной породой для многих видов работ, включая помощь инвалидам, поисково-спасательные операции, полицейские и военные задачи, актёрское мастерство.
{userName}: Что тяжелее, утка или машина?
{botName}: Автомобиль весит около 1300 кг, а утка весит всего около 2 кг. Поэтому машина тяжелее утки.
{userName}: Каков атомный номер кальция?
{botName}: Атом кальция имеет 20 протонов, 20 электронов и 20 нейтронов. Атомный номер или протонное число химического элемента — это число протонов, находящихся в ядре каждого атома этого элемента. Атомный номер однозначно идентифицирует химический элемент. Он идентичен зарядовому числу ядра. В кальции 20 протонов, следовательно, атомный номер кальция равен 20.
{userName}: Сколько будет 28 + 13?
{botName}: 28 + 13 равно 41.
{userName}: Расскажи мне анекдот.
{botName}: Когда клеишь обои - главное, чтобы пузырей не было. А то мы как-то взяли два пузыря...
{userName}: Как измеряются землетрясения?
{botName}: Сейсмограф — это основной инструмент для измерения землетрясений. Сейсмограф производит цифровую графическую запись движения грунта, вызванного сейсмическими волнами. Цифровая запись называется сейсмограммой. Сеть всемирных сейсмографов обнаруживает и измеряет силу и продолжительность волн землетрясения. Магнитуда землетрясения и интенсивность сотрясений обычно указывается по шкале Рихтера.";
    }

    private static string ContextItemToString(
        string predefinedUserName,
        string predefinedBotName,
        ContextItem contextItem)
    {
        var userName = predefinedUserName;
        var botName = predefinedBotName;
        var userMessage = contextItem.UserMessage;
        var botReply = contextItem.BotReply;

        return
            @$"{userName}: {userMessage}
{botName}: {botReply}";
    }

    public override PromptResult CreatePrompt(PromptRequest promptRequest)
    {
        var predefinedUserName = promptRequest.LanguageCode == "rus"
            ? DefaultRussianUserName
            : DefaultEnglishUserName;

        var predefinedBotName = promptRequest.LanguageCode == "rus"
            ? DefaultRussianBotName
            : DefaultEnglishBotName;

        var initialContext = promptRequest.LanguageCode == "rus"
            ? GetRussianInitialContext(predefinedUserName, predefinedBotName)
            : GetEnglishInitialContext(predefinedUserName, predefinedBotName);

        var initialContextMessage = new List<string> { initialContext };

        var chatHistory = promptRequest.Context
            .Select(c => ContextItemToString(predefinedUserName, predefinedBotName, c))
            .Union(new List<string> { ContextItemToString(predefinedUserName, predefinedBotName, promptRequest.NewContextItem) })
            .ToList();

        var newPrompt = string.Empty;
        int i;

        for (i = chatHistory.Count; i >= 0; i--)
        {
            newPrompt = string.Join(Environment.NewLine,
                initialContextMessage.Union(chatHistory.TakeLast(i)));

            if (newPrompt.Length <= MaxPromptSymbols)
            {
                break;
            }

            if (i == 0)
            {
                newPrompt = newPrompt.Cut(MaxPromptSymbols);
                break;
            }
        }

        var newContext = promptRequest.Context
            .TakeLast(i)
            .ToList()
            .AsReadOnly();

        return new PromptResult(newPrompt, newContext);
    }
}
