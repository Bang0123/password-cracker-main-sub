﻿using static System.Console;

namespace SlaveCrack
{
    public class Program
    {
        static void Main(string[] args)
        {
            Slave slave = new Slave();
            slave.BeginWork();

            ReadLine();
        }
    }
}
