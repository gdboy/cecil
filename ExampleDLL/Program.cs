using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleDLL
{
	delegate int NumberChanger (int a);
	delegate int NumberChanger2 (int a, int b);

	class Program {

		static int num = 10;

		public static int AddNum (int a)
		{
			num += a;
			return num;
		}
		public static int AddNum (int a, int b)
		{
			num += a;
			num += b;
			return num;
		}
		public static int MultNum (int a)
		{
			num *= a;
			return num;
		}
		public static int MultNum (int a, int b)
		{
			num *= a;
			num *= b;
			return num;
		}
		public static int getNum ()
		{
			return num;
		}


		static void Main (string [] args)
		{
			//create delegate instances
			//NumberChanger2 nc;
			//var nc1 = new NumberChanger2(AddNum);
			//var nc2 = new NumberChanger2(MultNum);

			//nc = nc1;
			////nc += nc2;

			////calling multicast
			//nc(5, 2);
			//Console.WriteLine("Value of Num: {0}", getNum());

			//nc2(10, 3);
			//Console.WriteLine("Value of Num: {0}", getNum());








			var array = new int [] { 1, 3, 2, 5, 4, 6, 7, 1, 2, 3, 4 };

			Array.Sort (array);

			for (var i = 0; i < array.Length; i++) {
				Console.WriteLine (array [i]);
			}
		}

		//static void Main()
		//{
		//    //var sum = 0;
		//    //for (var i = 1; i <= 100; i++)
		//    //    sum += i;

		//    //Console.WriteLine(sum);


		//    //int[] arr = new int[6] { -1, 0, 1, 2, -1, -4 };

		//    //IList<IList<int>> triplets = threeSumClose(arr);

		//    //foreach(var a in triplets)
		//    //{
		//    //    foreach(var b in a)
		//    //    {
		//    //        Console.WriteLine(b);
		//    //    }
		//    //}




		//}


		public static IList<IList<int>> threeSumClose (int [] nums)
		{
			int target = 0;
			IList<IList<int>> triplets = new List<IList<int>> ();
			HashSet<string> keys = new HashSet<string> (); // -1, 0, 1 -> key string" "-1,0,1,"

			if (nums == null || nums.Length == 0)
				return triplets;

			Array.Sort (nums);

			int len = nums.Length;

			for (int i = 0; i < len - 2; i++)  // len = 3, test case passes! 
			{
				int [] trialTriplet = new int [3];
				trialTriplet [0] = nums [i];

				// call two sum function 
				int newTarget = target - trialTriplet [0];
				int head = i + 1;
				int tail = len - 1;

				while (head < tail) {
					trialTriplet [1] = nums [head];
					trialTriplet [2] = nums [tail];

					int twoSumValue = trialTriplet [1] + trialTriplet [2];

					if (twoSumValue == newTarget)   // newTarget, not target 
					{
						// check if the key is in key hashset or not, 
						// if yes, then skip it; otherwise, add it to result 
						string key = getKey (trialTriplet, 3);

						if (!keys.Contains (key)) {
							keys.Add (key);

							IList<int> triplet = new List<int> ();

							for (int j = 0; j < 3; j++)
								triplet.Add (trialTriplet [j]);

							triplets.Add (triplet);
						}

						// continue to search 
						head++;
						tail--;

					} else if (twoSumValue > newTarget) {
						tail--;
					} else if (twoSumValue < newTarget) {
						head++;
					}
				}
			}

			return triplets;
		}

		private static string getKey (int [] arr, int len)
		{
			string key = "";

			for (int j = 0; j < 3; j++) {
				key += arr [j].ToString ();
				key += ",";
			}

			return key;
		}



	}
}
