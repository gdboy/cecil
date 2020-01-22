using System;

namespace ExampleDLL
{
	class Animal  // Base class (parent) 
  {
		private string name = "123";


		public virtual void animalSound ()
		{
			Console.WriteLine ("The animal makes a sound");
		}
	}

	class Pig : Animal  // Derived class (child) 
	{
		public override void animalSound ()
		{
			Console.WriteLine ("The pig says: wee wee");
		}
	}

	class Dog : Animal  // Derived class (child) 
	{
		public override void animalSound ()
		{
			Console.WriteLine ("The dog says: bow wow");
		}
	}

	class Polymorphism {
		static void Main ()
		{
			//var myAnimal = new Animal ();  // Create a Animal object
			var myPig = new Pig ();  // Create a Pig object
			//var myDog = new Dog ();  // Create a Dog object

			//myAnimal.animalSound ();
			//Console.WriteLine (myPig is Animal);
			myPig.animalSound ();
			//Console.WriteLine (myDog is Animal);
			//myDog.animalSound ();
		}
	}
}
