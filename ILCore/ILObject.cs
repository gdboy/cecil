//#define ILCORE_DEBUG

using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace ILCore {
	public class ILObject {
		public TypeDefinition type;//内部定义类型
		public object baseInstance;//继承外部引用类型
		private Dictionary<string, object> fields = new Dictionary<string, object> ();
		//private Dictionary<string, MethodDefinition> methods = new Dictionary<string, MethodDefinition> ();

		static Dictionary<string, TypeDefinition> types = new Dictionary<string, TypeDefinition> ();

		public static void LoadAssembly (string path)
		{
			var module = ModuleDefinition.ReadModule (path);

			foreach (var type in module.Types) {

				if (type.FullName.StartsWith ("<") && type.FullName.EndsWith (">"))
					continue;

				types [type.FullName] = type;

				//foreach (var methodDefinition in type.Methods) {

				//	if (methodDefinition.IsStatic && methodDefinition.Name == "Main") {

				//		foreach (var p in methodDefinition.Parameters)
				//			Interpreter.stack.Push (null);

				//		Interpreter.Execute (methodDefinition);

				//		break;
				//	}
				//}
			}
		}

		public static ILObject CreateInstance (string typeName, params object[] objects)
		{
			var type = types [typeName];

			Interpreter.PushParameters (objects);

			foreach(var method in type.Methods) {
				if(!method.IsStatic && method.IsConstructor) {
					return Interpreter.Execute (method, Code.Newobj) as ILObject;
				}
			}

			return null;
		}

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

		public object InvokeMethod(string methodName, params object[] objects)
		{
			Interpreter.stack.Push (this);
			Interpreter.PushParameters (objects);

			foreach (var method in type.Methods) {
				if (method.Name.IndexOf(methodName) != -1) {
					return Interpreter.Execute (method);
				}
			}

			return null;
		}

		public override string ToString ()
		{
			return type.FullName;
		}
	}
}
