﻿namespace Spindler.Utilities
{
    public class Shared
    {

        public readonly string userAgent;

        private static readonly Random randomNumberGenerator = new();
        private static readonly string[] possibleUserAgents = {
            "Mozilla/5.0 (Linux; Android 10; SM-G980F Build/QP1A.190711.020; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.96 Mobile Safari/537.36",
            "Mozilla/5.0 (Linux; Android 10; SM-G973U Build/PPR1.180610.011) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Mobile Safari/537.36",
            "Mozilla/5.0 (Linux; Android 11; Pixel 6 Build/SD1A.210817.023; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/94.0.4606.71 Mobile Safari/537.36",
            "Mozilla/5.0 (Linux; Android 11; Pixel 5 Build/RQ3A.210805.001.A1; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/92.0.4515.159 Mobile Safari/537.36"
        };

        public Shared()
        {
            userAgent = possibleUserAgents[randomNumberGenerator.Next(possibleUserAgents.Length)];
        }
    }
}