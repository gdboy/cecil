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

		private static int Sum(int a, int b)
		{
			var sum = 0;
			for (var i = a; i <= b; i++)
				sum += i;

			return sum;
		}

		class TreeNode {
			public TreeNode left;
			public TreeNode right;
			public int value;

			public TreeNode(int v)
			{
				value += v;
			}
		}

		static void Main2 ()
		{
			//Console.WriteLine ("3333333333333333333333333333333333333");

			//int? a = 3;

			//Console.WriteLine (a.Value);

			//a = null;

			//Console.WriteLine (a.Value);

			//var tree = new TreeNode (5);
			//tree.left = new TreeNode (4);
			//tree.right = new TreeNode (1);

			//Console.WriteLine ("==============================================");

			//Console.WriteLine (tree.value);

			//Console.WriteLine (tree.right.value);

			//Console.WriteLine (tree.left.value);

			//Console.WriteLine (tree.left.value);
			//Console.WriteLine (tree.left.a);
			//Console.WriteLine (tree.left.b);

			//Console.WriteLine (tree.right.value);
			//Console.WriteLine (tree.right.a);
			//Console.WriteLine (tree.right.b);











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




			//var array = new int [] { 1, 3, 2, 5, 4, 6, 7, 1, 2, 3, 4 };

			//var xx = new List<List<int>> ();

			//Console.WriteLine (xx);

			////var list = array.ToList ();
			//var list = new List<int []> (3);
			//list.Add (array);

			//Array.Sort (array);

			//for (var i = 0; i < array.Length; i++) {
			//	Console.WriteLine (array [i]);
			//}
		}

	}
}
