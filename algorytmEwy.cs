using Quantum;
using Quantum.Operations;
using System;
using System.Numerics;
using System.Collections.Generic;

namespace QuantumConsole
{
	public class QuantumTest
	{	
		public static Tuple<int, int> FractionalApproximation(int a, int b, int width)
        {
            double f = (double)a / (double)b;
            double g = f;
            int i, num2 = 0, den2 = 1, num1 = 1, den1 = 0, num = 0, den = 0;
            int max = 1 << width;

            do
            {
                i = (int)g;  // integer part
                g = 1.0 / (g - i);  // reciprocal of the fractional part

                if (i * den1 + den2 > max) // if denominator is too big
                {
                    break;
                }

                // new numerator and denominator
                num = i * num1 + num2;
                den = i * den1 + den2;

                // previous nominators and denominators are memorized
                num2 = num1;
                den2 = den1;
                num1 = num;
                den1 = den;

            }
            while (Math.Abs(((double)num / (double)den) - f) > 1.0 / (2 * max));
            // this condition is from Shor algorithm

            return new Tuple<int, int>(num, den);
        }
        
        public static int FindPeriod(int N, int a) {
			ulong ulongN = (ulong)N;
			int width = (int)Math.Ceiling(Math.Log(N, 2));
 
			// Console.WriteLine("Width for N: {0}", width);
			// Console.WriteLine("Total register width (7 * w + 2) : {0}", 7 * width + 2);
			
			QuantumComputer comp = QuantumComputer.GetInstance();
			
			//input register
			Register regX = comp.NewRegister(0, 2 * width);
			
			// output register (must contain 1):
			Register regX1 = comp.NewRegister(1, width + 1);
			
			// perform Walsh-Hadamard transform on the input register
			// input register can contains N^2 so it is 2*width long
			// Console.WriteLine("Applying Walsh-Hadamard transform on the input register...");
			comp.Walsh(regX);
			
			// perform exp_mod_N
			// Console.WriteLine("Applying f(x) = a^x mod N ...");
			comp.ExpModulo(regX, regX1, a, N);
			
			// output register is no longer needed
			regX1.Measure();
			
			// perform Quantum Fourier Transform on the input register
			// Console.WriteLine("Applying QFT on the input register...");
			comp.QFT(regX);
			
			comp.Reverse(regX);
			
			// getting the input register
			int Q = (int)(1 << 2 * width);
			int inputMeasured = (int)regX.Measure();
			// Console.WriteLine("Input measured = {0}", inputMeasured);
			// Console.WriteLine("Q = {0}", Q);
			
			Tuple<int, int> result = FractionalApproximation(inputMeasured, Q, 2 * width - 1);
 
			// Console.WriteLine("Fractional approximation:  {0} / {1}", result.Item1, result.Item2);
			
			int period = result.Item2;
			
			if(BigInteger.ModPow(a, period, N) == 1) {
				Console.WriteLine("Period = {0}", period);
				return period;
			}
			
			int maxMult = (int)(Math.Sqrt(N)) + 1;
			int mult = 2;
			while(mult < maxMult) 
			{
				Console.WriteLine("Trying multiply by {0} ...", mult);
				period = result.Item2 * mult;
				if(BigInteger.ModPow(a, period, N) == 1) 
				{
					Console.WriteLine("Success !!!    period = {0}", period);
					return period;
				}
				else 
				{		
					mult++;
				}
			}
			
			Console.WriteLine("Failure !!!    Period not found, try again.");
			return -1;
		}
		
		public static int CalculateModulo(int N, int a, ulong x)
		{
        	// obliczamy ile bitow potrzeba na zapamiętanie N
			ulong ulongN = (ulong)N;
			int width = (int)Math.Ceiling(Math.Log(N, 2));
 	
			QuantumComputer comp = QuantumComputer.GetInstance();
			
			Register regX = comp.NewRegister(0, 2 * width);
			Register regY = comp.NewRegister(1, width + 1);
 
			regX.Reset(x);
			regY.Reset(1);
 
        	// obliczamy a^x mod N
 			comp.ExpModulo(regX, regY, a, N);
 
 			int valueMeasured = (int)regY.Measure();

        	 Console.WriteLine ("Dla {0} reszta to {1}",x, valueMeasured);
        	
        	return valueMeasured;
		}
		
		/*
			a - liczba ktorej odwrotnosc modulo obliczamy
			b - modul odwrotnosci
			
			Szukamy: ax + by = NWD(a, b) -> jesli a i b sa wzglednie pierwsze to  jest odwrotnoscia modulo b liczby a
				- au + bv = w
				- ax + by = z
				- Warunek: NWD(a, b) = NWD(w, z)
		*/
		public static int ComputeModuleReverse(int a, int b) {
			int u = 1, w = a;
			int x = 0, z = b;
			int q; 
			while(w != 0) 
			{
				if(w < z) 
				{
					q = u; u = x; x = q;
					q = w; w = z; z = q;
				}
				q = w / z;
				u -= q*x;
				w -= q*z;
			}
			if(z == 1) 
			{
				if(x < 0)
				{
					x += b;
				}
				Console.WriteLine("ComputeModuloReverse: Znalezinono odwrtonosc modulo liczb {0} i {1} rowna {2}", a, b, x);
			}
			else 
			{
				Console.WriteLine("ComputeModuloReverse: Nie znaleziono odwrotnosci modulo.");
			}
			return x;
		}
		
		public static void Main()
		{
			int N = 55;		// N = pq
			int c = 17;		// Public key
			int a = 9;		// Alice's message
		
			Console.WriteLine("a = {0}", a);
			Console.WriteLine("N = {0}", N);
			Console.WriteLine("c = {0}", c);		
			
			int b = (int) BigInteger.ModPow(a, c, N);	// Alice's ciphered message
			
			if(BigInteger.GreatestCommonDivisor(N, b) != 1) // Sprawdzamy warunek, że b jest bezwględnie pierwsza z N
			{
				findPeriodAndPerformFactoring(N, c, a, b);
			}
			else
			{
				eveAlgorithm(N, c, b);
			}
			
		}
		
		private static void findPeriodAndPerformFactoring(int N, int c, int a, int b) {
			Console.WriteLine("Period and Factoring.");
			int randomDivisor = new Random().Next(0, N);	// Look for period from 0..N
			int r = FindPeriod(N, randomDivisor);
			
			if(r % 2 == 0)
			{
				Console.WriteLine("Algorytm lamiacy RSA.");
				int x = CalculateModulo(N, a, (ulong) r/2);
				int p = (int) BigInteger.GreatestCommonDivisor(N, x-1);
				int q = (int) BigInteger.GreatestCommonDivisor(N, x+1);
				Console.WriteLine("Klucze prywatne: {0}, {1}", p, q);
				
				int d = ComputeModuleReverse((p - 1) * (q - 1), c);
				int decryptedMessage = (int) BigInteger.ModPow(b, d, N);
				Console.WriteLine("Wiadomosc: {0}.", decryptedMessage);
			}
			else
			{
				Console.WriteLine("Nie udalo sie odszyfrowac wiadomosci.");
			}
		}
		
		private static void eveAlgorithm(int N, int c, int b) 
		{
			Console.WriteLine("Algorytm Eve.");
			int r = FindPeriod(N, b);		// znajdujemy okres r = b^x mod N
			int d = ComputeModuleReverse(c, r);			// liczymy odwrotnosc modulo c w G_r
			int decryptedMessage = (int) BigInteger.ModPow(b, d, N);		// wyliczamy wiadomosc ze wzoru b^d mod N
			Console.WriteLine("Wiadomosc: {0}.", decryptedMessage);
		}
	}
}