//#define ILCORE_DEBUG

using Mono.Cecil;
using System.Collections.Generic;

namespace ILCore {
	class ILObject {
		public TypeDefinition type;//内部定义类型
		public object baseInstance;//继承外部引用类型
		private Dictionary<string, object> fields = new Dictionary<string, object> ();
		private Dictionary<string, MethodDefinition> methods = new Dictionary<string, MethodDefinition> ();

		public void SetValue (FieldReference fieldReference, object value)
		{
			if(fieldReference is FieldDefinition) {
				var key = fieldReference.DeclaringType.FullName + "::" + fieldReference.Name;
				fields [key] = value;
			} else {
				baseInstance.SetFieldValue (fieldReference.Name, value);
			}
		}

		public object GetValue (FieldReference fieldReference)
		{
			if (fieldReference is FieldDefinition) {
				var key = fieldReference.DeclaringType.FullName + "::" + fieldReference.Name;
				fields.TryGetValue (key, out object value);
				return value;
			} else {
				return baseInstance.GetFieldValue (fieldReference.Name);
			}
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

		public override string ToString ()
		{
			return type.FullName;
		}
	}
}
