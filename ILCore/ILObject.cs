//#define ILCORE_DEBUG

using Mono.Cecil;
using System.Collections.Generic;

namespace ILCore {
	class ILObject {
		public TypeDefinition type;
		private Dictionary<string, object> fields = new Dictionary<string, object> ();
		private Dictionary<string, MethodDefinition> methods = new Dictionary<string, MethodDefinition> ();

		public void SetValue (string key, object value)
		{
			fields [key] = value;
		}

		public object GetValue (string key)
		{
			fields.TryGetValue (key, out object value);
			return value;
		}

		#region 多态

		public TypeDefinition GetType (TypeDefinition typeDefinition)
		{

			return typeDefinition;
		}

		public MethodDefinition GetMethod (MethodDefinition methodDefinition)
		{
			foreach (var method in type.Methods) {
				if (!method.IsVirtual)
					continue;

				var methodName = method.FullName.Substring (method.FullName.IndexOf ("::"));

				if (methodDefinition.FullName.Substring (methodDefinition.FullName.IndexOf ("::")) == methodName)
					return method;
			}

			return methodDefinition;
		}

		#endregion
	}
}
