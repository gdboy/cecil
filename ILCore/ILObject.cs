//#define ILCORE_DEBUG

using GameCenter.ExtensionMethods;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace ILCore {
	public class ILObject {
		public TypeDefinition type;//内部定义类型
		public object baseInstance;//继承外部引用类型
		private Dictionary<string, object> fields = new Dictionary<string, object> ();
		private static Dictionary<string, TypeDefinition> types = new Dictionary<string, TypeDefinition> ();
		private static Dictionary<string, Dictionary<string, MethodDefinition>> methods = new Dictionary<string, Dictionary<string, MethodDefinition>>();
		
		public static void LoadAssembly (string path)
		{
			var module = ModuleDefinition.ReadModule (path);

			foreach (var type in module.Types) {

				if (type.FullName.StartsWith ("<") && type.FullName.EndsWith (">"))
					continue;

				types [type.FullName] = type;
			}
		}

		public static ILObject CreateInstance (string typeName, params object[] objects)
		{
			return CreateInstance(typeName, ".ctor", objects);
		}

		public static ILObject CreateInstance (string typeName, string methodName, params object [] objects)
		{
			return Execute (typeName, methodName, Code.Newobj, objects) as ILObject;
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

		private static Dictionary<string, MethodDefinition> GetMethodDictionary (string typeName)
		{
			var type = types [typeName];

			if (!methods.TryGetValue (typeName, out Dictionary<string, MethodDefinition> methodDictionary)) {

				methods [typeName] = methodDictionary = new Dictionary<string, MethodDefinition> ();

				foreach (var method in type.Methods) {
					var methodName = method.FullName.Substring (method.FullName.IndexOf ("::") + 2);

					methodDictionary [methodName] = method;
				}
			}

			return methodDictionary;
		}

		public static List<string> GetMethods(string typeName)
		{
			var methodDictionary = GetMethodDictionary(typeName);

			var methodList = new List<string>(methodDictionary.Count);

			foreach (var item in methodDictionary)
				methodList.Add (item.Key);

			return methodList;
		}

		public object InvokeMethod(string methodName, params object[] objects)
		{
			Interpreter.stack.Push (this);
			return Execute (type.FullName, methodName, Code.Callvirt, objects);
		}

		public static object InvokeMethod (string typeName, string methodName, params object [] objects)
		{
			return Execute (typeName, methodName, Code.Call, objects);
		}

		private static object Execute(string typeName, string methodName, Code code, object[] objects)
		{
			Interpreter.PushParameters (objects);

			var methodDictionary = GetMethodDictionary (typeName);

			if (methodDictionary.TryGetValue (methodName, out MethodDefinition methodDefinition))
				return Interpreter.Execute (methodDefinition, code);

			var parameters = "";

			for (var i = 0; i < objects.Length; i++) {
				parameters += objects [i].GetType ().FullName;
				if (i != objects.Length - 1)
					parameters += ",";
			}

			methodDictionary.TryGetValue (methodName + "(" + parameters + ")", out methodDefinition);
			
			return Interpreter.Execute (methodDefinition, code);
		}

		public override string ToString ()
		{
			var methodDictionary = GetMethodDictionary (type.FullName);

			if (methodDictionary.TryGetValue ("ToString()", out MethodDefinition methodDefinition)) {
				Interpreter.stack.Push (this);
				return Interpreter.Execute (methodDefinition, Code.Callvirt) as string;
			}
			
			return type.FullName;
		}
	}
}
