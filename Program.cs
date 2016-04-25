using System;
using System.Numerics;


    class Program
    {
        static void Main()
        {

            BigInteger number = 4;
            BigInteger modulus = 55;

            for (int exponent = 1; exponent <= 100; exponent++)
                Console.WriteLine("({0}^{1}) Mod {2} = {3}",
                                number, exponent, modulus,
                                BigInteger.ModPow(number, exponent, modulus));

        }
    }
