using Mono.Cecil;
using System.Collections.Generic;

namespace ILCore {
	class DelegateHandler {
		private List<MethodDefinition> methods = new List<MethodDefinition> ();

		public DelegateHandler (MethodDefinition methodDefinition)
		{
			methods.Add (methodDefinition);
		}

		public static DelegateHandler operator + (DelegateHandler a, DelegateHandler b)
		{
			a.methods.AddRange (b.methods);

			return a;
		}

		public static DelegateHandler operator - (DelegateHandler a, DelegateHandler b)
		{
			for(var i = 0; i < b.methods.Count; i++) {
				a.methods.Remove (b.methods[i]);
			}

			return a;
		}

		public void Invoke(object[] objects)
		{
			foreach (var method in methods) {
				Interpreter.PushParameters (objects);
				Interpreter.Execute (method);
			}
		}
	}
}
