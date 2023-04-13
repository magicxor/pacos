namespace Pacos.Enums;

public static class LocalizationKeys
{
    public static class Errors
    {
        public static class Configuration
        {
            public const string KoboldApiAddressMissing = "Errors.Configuration.KoboldApiAddressMissing";
            public const string TelegramBotApiKeyMissing = "Errors.Configuration.TelegramBotApiKeyMissing";
        }

        public static class Telegram
        {
            public const string ErrorSendingMessage = "Errors.Telegram.ErrorSendingMessage";
            public const string ErrorObtainingPhotoId = "Errors.Telegram.ErrorObtainingPhotoId";
            public const string LoadingProgressPictureIdIsNull = "Errors.Telegram.LoadingProgressPictureIdIsNull";
        }

        public static class Gpt
        {
            public const string PresetFactoryNotFound = "Errors.Txt2Img.PresetFactoryNotFound";
            public const string ErrorQueryingKobold = "Errors.Txt2Img.ErrorQueryingKobold";
            public const string NoImagesReturned = "Errors.Txt2Img.NoImagesReturned";
        }
    }
}
