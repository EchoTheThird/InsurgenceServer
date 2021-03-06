﻿using System;

namespace InsurgenceServerCore.Logger
{
    public static class Logger
    {
        public static void Log(string message)
        {
            if (message == null) return;
            Console.WriteLine($"{DateTime.UtcNow:O} - {message}");
        }
    }
}