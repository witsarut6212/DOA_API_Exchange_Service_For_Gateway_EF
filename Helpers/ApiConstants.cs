namespace DOA_API_Exchange_Service_For_Gateway.Helpers
{
    public static class ApiConstants
    {
        public static class PayloadStatus
        {
            public const string Wait = "WAIT";
            public const string Processing = "PROCESSING";
            public const string Success = "SUCCESS";
            public const string Fail = "FAIL";
        }

        public static class QueueStatus
        {
            public const string Wait = "WAIT";
            public const string InQueue = "IN-QUEUE";
            public const string Success = "SUCCESS";
            public const string Fail = "FAIL";
        }

        public static class CommonStatus
        {
            public const string Yes = "Y";
            public const string No = "N";
            public const string Active = "Y";
            public const string Inactive = "N";
        }

        public static class TxnType
        {
            public const string EPhytoASW = "EPC-0101";
            public const string EPhytoIPPC = "EPC-0102";
            public const string EPhytoProgress = "EPC-0201";
            public const string EPhytoCertificate = "EPC-0202";
        }
    }
}
