using System;

namespace VATyakov.Dev
{
    public static class VatStressMessages
    {
        public const char Separator = '\n';
        public const char KeySeparator = '=';
        public const string SuccessReply = "ok";

        public static readonly Guid List = new("6b1d2f40-8e3c-4a57-b0d9-2c7e5f1a9d01");
        public static readonly Guid Select = new("6b1d2f40-8e3c-4a57-b0d9-2c7e5f1a9d02");
        public static readonly Guid Info = new("6b1d2f40-8e3c-4a57-b0d9-2c7e5f1a9d03");
        public static readonly Guid Reply = new("6b1d2f40-8e3c-4a57-b0d9-2c7e5f1a9d04");
    }
}
