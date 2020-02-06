#define ILCORE_DEBUG

using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

//指令大全 https://docs.microsoft.com/zh-cn/dotnet/api/system.reflection.emit.opcodes?view=netframework-4.8

namespace ILCore {

	class Program {
		static Stack<object> stack = new Stack<object> ();
		static object [] localVars = null;//局部变量
		static Stack<object []> localVarsStack = new Stack<object []> ();//局部变量栈
		static object [] localArgs = null;//函数参数
		static Stack<object []> localArgsStack = new Stack<object []> ();//函数参数栈

		static Dictionary<TypeReference, Dictionary<string, object>> staticFields = new Dictionary<TypeReference, Dictionary<string, object>> ();//静态字段

#if ILCORE_DEBUG
		static List<KeyValuePair<Instruction, Stack<object>>> stackInfo = new List<KeyValuePair<Instruction, Stack<object>>>();//每条指令执行时的栈信息
#endif

		static Type GetType (string typeName)
		{
			var type = Type.GetType (typeName);

			if (type != null)
				return type;

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
				type = assembly.GetType (typeName);

				if (type != null)
					return type;
			}

			return null;
		}

		static string [] SplitTypeName (string typeName)
		{
			var types = new List<string> ();

			var startIndex = 0;

			var open = 0;

			for (var i = 0; i < typeName.Length; i++) {
				var c = typeName [i];

				switch (c) {
				case '<':
					open++;
					break;

				case '>':
					open--;
					break;

				case ',':
					if (open == 0) {
						types.Add (typeName.Substring (startIndex, i - startIndex));
						startIndex = i + 1;
					}
					break;
				}
			}

			types.Add (typeName.Substring (startIndex));

			return types.ToArray ();
		}

		static string ConvertTypeName (string typeName)
		{
			//var array = "";
			//while (typeName.EndsWith ("[]")) {
			//	typeName = typeName.Substring (0, typeName.Length - 2);
			//	array += "[]";
			//}

			//if (array != "")
			//	return ConvertTypeName (typeName) + array;

			var start = typeName.IndexOf ("<");
			var end = typeName.LastIndexOf (">");

			if (start == end)
				return GetType (typeName).AssemblyQualifiedName;

			var subTypeName = typeName.Substring (start + 1, end - 1 - start);

			var types = SplitTypeName (subTypeName);

			for (var i = 0; i < types.Length; i++) {
				var type = ConvertTypeName (types [i]);
				types [i] = "[" + GetType (type).AssemblyQualifiedName + "]";
			}

			subTypeName = string.Join (",", types);

			return typeName.Substring (0, start) + "[" + subTypeName + "]" + typeName.Substring (end, typeName.Length - 1 - end);
		}

		//泛型需要结合MethodReference的参数得到具体类型信息
		static Type GetType (TypeReference typeReference, MethodReference methodReference = null)
		{
			if (typeReference is TypeDefinition)
				return typeof(ILObject);

			var typeName = typeReference.FullName;

			if (typeReference.IsGenericParameter) {
				var genericArguments = ((GenericInstanceType)methodReference.DeclaringType).GenericArguments;
				for (var i = 0; i < genericArguments.Count; i++) {
					typeName = typeName.Replace ("!" + i, genericArguments [i].FullName);
				}
			}

			if (methodReference != null && methodReference.IsGenericInstance) {
				var genericArguments = ((GenericInstanceMethod)methodReference).GenericArguments;
				for (var i = 0; i < genericArguments.Count; i++) {
					typeName = typeName.Replace ("!!" + i, genericArguments [i].FullName);
				}
			}

			if (typeReference.IsGenericInstance || typeReference.IsArray)
				typeName = ConvertTypeName (typeName);

			return GetType (typeName);
		}

		static MethodInfo GetMethod (MethodReference methodReference)
		{
			if(!methodReference.ContainsGenericParameter) {

				var classType = GetType (methodReference.DeclaringType);

				var types = GetParameters(methodReference);

				return classType.GetMethod (methodReference.Name, types);
			}

			var methods = GetType (methodReference.DeclaringType, methodReference).GetMethods (BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			foreach (var method in methods) {
				if (method.Name != methodReference.Name || method.IsGenericMethod != methodReference.IsGenericInstance)
					continue;

				var parameters = method.GetParameters ();
				if (parameters.Length != methodReference.Parameters.Count)
					continue;

				var find = true;

				for(var i = 0; i < parameters.Length; i++) {
					var type = GetType (methodReference.Parameters [i].ParameterType, methodReference);
					if (parameters [i].ParameterType.FullName != null && parameters [i].ParameterType.Name != type.Name) {
						find = false;
						break;
					}
				}

				if (!find)
					continue;

				if (!method.ContainsGenericParameters)
					return method;

				var types = GetGenericArguments (methodReference);

				return method.MakeGenericMethod (types);
			}

			return null;
		}
		
		static Type[] GetGenericArguments(MethodReference methodReference)
		{
			if (!methodReference.IsGenericInstance)
				return null;

			var genericArguments = ((GenericInstanceMethod)methodReference).GenericArguments;

			var types = new Type [genericArguments.Count];

			for (var i = 0; i < genericArguments.Count; i++) {
				types [i] = GetType(genericArguments [i], methodReference);
			}

			return types;
		}

		static Type[] GetParameters(MethodReference methodReference)
		{
			var parameters = methodReference.Parameters;

			var types = new Type [parameters.Count];

			for (var i = 0; i < parameters.Count; i++) {
				types [i] = GetType (parameters [i].ParameterType, methodReference);
			}

			return types;
		}

		static long Compare (object a, object b)
		{
			if (a == null && b == null)
				return 0;

			if (a == null && b != null)
				return -1;

			if (a != null && b == null)
				return 1;

			if (a is double || b is double || a is float || b is float) {
				var result = Convert.ToDouble (a) - Convert.ToDouble (b);
				return Convert.ToInt32 (result);
			}

			return Convert.ToInt64 (a) - Convert.ToInt64 (b);
		}

		static void Main (string [] args)
		{
			foreach (var item in new int [] { 1, 3, 2, 5, 4 }.ToList ())
				Console.WriteLine (item);

			Assembly.LoadFrom (@"F:\workspace\GameCenter\UnityAssemblies\UnityEngine.CoreModule.dll");
			Assembly.LoadFrom (@"F:\workspace\cecil\ExampleDLL2\bin\Debug\ExampleDLL2.dll");

#if DEBUG
			var module = ModuleDefinition.ReadModule ("../../../ExampleDll/bin/Debug/ExampleDll.dll");
#else
			var module = ModuleDefinition.ReadModule ("../../../ExampleDll/bin/Release/ExampleDll.dll");
#endif
			foreach (var type in module.Types) {

				foreach (var methodDefinition in type.Methods) {

					if (methodDefinition.IsStatic && methodDefinition.Name == "Main") {

						foreach (var p in methodDefinition.Parameters)
							stack.Push (null);

						Execute (methodDefinition);

						break;
					}
				}
			}
		}

		static void Execute (MethodReference methodReference, Code code = Code.Call)
		{
			switch(methodReference.FullName) {
			case "System.Void System.Object::.ctor()":
				return;

			case "System.Void System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(System.Array,System.RuntimeFieldHandle)":
				{
					var bytes = stack.Pop () as Array;
					var array = stack.Pop () as Array;

					Buffer.BlockCopy (bytes, 0, array, 0, bytes.Length);
				}
				return;
			}
			
			var parameters = methodReference.Parameters;

			var objects = new object [parameters.Count];

			for (var i = 0; i < parameters.Count; i++) {

				var value = stack.Pop ();

				switch (GetType(parameters[i].ParameterType, methodReference).Name) {
				case "Boolean":
					value = Convert.ToBoolean (value);
					break;

				case "Int16":
					value = Convert.ToInt16 (value);
					break;

				case "UInt16":
					value = Convert.ToUInt16 (value);
					break;

				case "Int32":
					value = Convert.ToInt32 (value);
					break;

				case "UInt32":
					value = Convert.ToUInt32 (value);
					break;

				case "Int64":
					value = Convert.ToInt64 (value);
					break;

				case "UInt64":
					value = Convert.ToUInt64 (value);
					break;
				}

				objects [parameters.Count - 1 - i] = value;
			}

			if (methodReference.Name == ".ctor") {
				if (methodReference.IsGenericInstance) {
					var classType = GetType (methodReference.DeclaringType.GetElementType (), methodReference);
					var types = GetGenericArguments (methodReference);
					var genericType = classType.MakeGenericType (types);
					var instance = Activator.CreateInstance (genericType, objects);
					stack.Push (instance);
				} else {
					var classType = GetType (methodReference.DeclaringType, methodReference);
					var instance = Activator.CreateInstance (classType, objects);
					if (code == Code.Newobj)
						stack.Push (instance);
					else
						(stack.Peek () as ILObject).baseInstance = instance;
				}
				return;
			}

			object obj = null;

			if (methodReference.HasThis)
				obj = stack.Pop ();

			var method = GetMethod (methodReference);

			var resultValue = method.Invoke (obj, objects);

			if (method.ReturnType.FullName != "System.Void")
				stack.Push (resultValue);
		}

		static void Execute (MethodDefinition methodDefinition, Code code = Code.Call)
		{
			if(code == Code.Callvirt && methodDefinition.IsVirtual) {
				var parameterStack = new Stack<object> ();
				for (var i = 0; i < methodDefinition.Parameters.Count; i++)
					parameterStack.Push (stack.Pop ());

				methodDefinition = (stack.Peek () as ILObject).GetMethod (methodDefinition);
				
				for (var i = 0; i < methodDefinition.Parameters.Count; i++)
					stack.Push (parameterStack.Pop ());
			}

			//Console.WriteLine (methodDefinition);

			if (!staticFields.ContainsKey (methodDefinition.DeclaringType)) {
				var dictionary = new Dictionary<string, object> ();

				staticFields [methodDefinition.DeclaringType] = dictionary;

				foreach (var method in methodDefinition.DeclaringType.Methods) {
					if (method.IsStatic && method.IsConstructor)//静态构造函数
					{
						Execute (method);
						break;
					}
				}
			}

			var parameters = methodDefinition.Parameters;

			localArgsStack.Push (localArgs);
			localArgs = new object [parameters.Count + (methodDefinition.HasThis ? 1 : 0)];

			for (var i = 0; i < parameters.Count; i++) {
				localArgs [localArgs.Length - 1 - i] = stack.Pop ();
			}

			if (!methodDefinition.IsStatic && methodDefinition.IsConstructor) {
				var ctorType = methodDefinition.DeclaringType;
				
				if(code == Code.Newobj) {
					var obj = new ILObject {
						type = ctorType
					};

					stack.Push (obj);
					
					while (ctorType != null) {

						for (var i = 0; i < ctorType.Fields.Count; i++) {
							var field = ctorType.Fields [i];
							var fieldType = field.FieldType;
							if (fieldType is TypeDefinition || !fieldType.IsValueType)
								continue;

							var type = GetType (fieldType, methodDefinition);
							obj.SetValue (field, Activator.CreateInstance (type));
						}

						ctorType = ctorType.BaseType as TypeDefinition;
					}
				}
			}

			if (methodDefinition.HasThis)
				localArgs [0] = stack.Pop ();

			localVarsStack.Push (localVars);
			localVars = new object [methodDefinition.Body.Variables.Count];

			for(var i=0;i<methodDefinition.Body.Variables.Count;i++) {
				var variableType = methodDefinition.Body.Variables [i].VariableType;
				if (variableType is TypeDefinition || !variableType.IsValueType)
					continue;

				var type = GetType (variableType, methodDefinition);
				localVars [i] = Activator.CreateInstance (type);
			}

			if (methodDefinition.HasBody) {
				var nextInstruction = methodDefinition.Body.Instructions [0];

				ExceptionHandler finallyHandler = null;

				do {
					//try {
						nextInstruction = Execute (nextInstruction, methodDefinition);

						if (finallyHandler != null && nextInstruction == null) {
							nextInstruction = finallyHandler.HandlerStart;
							finallyHandler = null;
						}
					//}
					//catch (Exception e) {

					//	var exceptionHandlers = methodDefinition.Body.ExceptionHandlers;

					//	if (exceptionHandlers == null || exceptionHandlers.Count == 0) {
					//		Console.WriteLine (e);
					//		break;
					//	}

					//	foreach (var exceptionHandler in exceptionHandlers) {

					//		if (exceptionHandler.HandlerType == ExceptionHandlerType.Catch && GetType (exceptionHandler.CatchType).IsAssignableFrom (e.GetType ())) {
					//			stack.Push (e);
					//			instruction = exceptionHandler.HandlerStart;
					//			break;
					//		}
					//	}

					//	var lastExceptionHandler = exceptionHandlers [exceptionHandlers.Count - 1];

					//	if (lastExceptionHandler.HandlerType == ExceptionHandlerType.Finally)
					//		finallyHandler = lastExceptionHandler;
					//}
				} while (nextInstruction != null);
			} else {//委托
				switch (methodDefinition.Name) {
				case ".ctor":
					stack.Push (localArgs [localArgs.Length - 1]);
					break;

				case "Invoke":
					stack.Push (localArgs [0]);
					Execute (localArgs [1] as MethodDefinition);
					break;
				}
			}

			localArgs = localArgsStack.Pop ();

			localVars = localVarsStack.Pop ();
		}

		static Instruction Execute (Instruction instruction, MethodDefinition methodDefinition)
		{
			
#if ILCORE_DEBUG
			Console.WriteLine (instruction);
			stackInfo.Add (new KeyValuePair<Instruction, Stack<object>> (instruction, new Stack<object> (stack)));
#endif

			var next = instruction.Next;

			switch (instruction.OpCode.Code) {

			case Code.Nop:
			case Code.Castclass:
			case Code.Volatile:
			case Code.Readonly:
				break;

#region 栈操作
			case Code.Dup:
				stack.Push (stack.Peek ());
				break;

			case Code.Pop:
				stack.Pop ();
				break;
#endregion

			case Code.Ldc_I4_M1:
			case Code.Ldc_I4_0:
			case Code.Ldc_I4_1:
			case Code.Ldc_I4_2:
			case Code.Ldc_I4_3:
			case Code.Ldc_I4_4:
			case Code.Ldc_I4_5:
			case Code.Ldc_I4_6:
			case Code.Ldc_I4_7:
			case Code.Ldc_I4_8:
				stack.Push (instruction.OpCode.Code - Code.Ldc_I4_0);
				break;

			//将所提供的 xx 类型的值作为 xx 推送到计算堆栈上。
			case Code.Ldc_I4_S:
			case Code.Ldc_I4:
			case Code.Ldc_I8:
			case Code.Ldc_R4:
			case Code.Ldc_R8:
				stack.Push (instruction.Operand);
				break;

#region 局部变量
			case Code.Stloc_0:
			case Code.Stloc_1:
			case Code.Stloc_2:
			case Code.Stloc_3:
				localVars [instruction.OpCode.Code - Code.Stloc_0] = stack.Pop ();
				break;

			//从计算堆栈的顶部弹出当前值并将其存储到指定索引处的局部变量列表中。
			case Code.Stloc_S:
			case Code.Stloc:
				localVars [((VariableReference)instruction.Operand).Index] = stack.Pop ();
				break;

			case Code.Ldloc_0:
			case Code.Ldloc_1:
			case Code.Ldloc_2:
			case Code.Ldloc_3: {
					var index = instruction.OpCode.Code - Code.Ldloc_0;
					stack.Push (localVars[index]);
				}
				break;

			//将指定索引处的局部变量加载到计算堆栈上。
			case Code.Ldloc_S:
			case Code.Ldloc:
			//将位于特定索引处的局部变量的地址加载到计算堆栈上。
			case Code.Ldloca_S:
			case Code.Ldloca: {
					var index = (instruction.Operand as VariableReference).Index;
					stack.Push (localVars [index]);
				}
				break;
#endregion

#region 函数参数
			case Code.Ldarg_0:
			case Code.Ldarg_1:
			case Code.Ldarg_2:
			case Code.Ldarg_3:
				stack.Push (localArgs [instruction.OpCode.Code - Code.Ldarg_0]);
				break;

			//将参数（由指定索引值引用）加载到堆栈上。
			case Code.Ldarg_S:
			case Code.Ldarg:
			//将参数地址加载到计算堆栈上。
			case Code.Ldarga_S:
			case Code.Ldarga:
				stack.Push (localArgs [((VariableReference)instruction.Operand).Index]);
				break;
#endregion

#region 加减乘除求余
			case Code.Add: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					if (a is double || b is double)
						stack.Push (Convert.ToDouble (a) + Convert.ToDouble (b));
					else if (a is float || b is float)
						stack.Push (Convert.ToSingle (a) + Convert.ToSingle (b));
					else if (a is long || b is long)
						stack.Push (Convert.ToInt64 (a) + Convert.ToInt64 (b));
					else
						stack.Push (Convert.ToInt32 (a) + Convert.ToInt32 (b));
				}
				break;

			case Code.Sub: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					if (a is double || b is double)
						stack.Push (Convert.ToDouble (a) - Convert.ToDouble (b));
					else if (a is float || b is float)
						stack.Push (Convert.ToSingle (a) - Convert.ToSingle (b));
					else if (a is long || b is long)
						stack.Push (Convert.ToInt64 (a) - Convert.ToInt64 (b));
					else
						stack.Push (Convert.ToInt32 (a) - Convert.ToInt32 (b));
				}
				break;

			case Code.Mul: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					if (a is double || b is double)
						stack.Push (Convert.ToDouble (a) * Convert.ToDouble (b));
					else if (a is float || b is float)
						stack.Push (Convert.ToSingle (a) * Convert.ToSingle (b));
					else if (a is long || b is long)
						stack.Push (Convert.ToInt64 (a) * Convert.ToInt64 (b));
					else
						stack.Push (Convert.ToInt32 (a) * Convert.ToInt32 (b));
				}
				break;

			case Code.Div: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					if (a is double || b is double)
						stack.Push (Convert.ToDouble (a) / Convert.ToDouble (b));
					else if (a is float || b is float)
						stack.Push (Convert.ToSingle (a) / Convert.ToSingle (b));
					else if (a is long || b is long)
						stack.Push (Convert.ToInt64 (a) / Convert.ToInt64 (b));
					else
						stack.Push (Convert.ToInt32 (a) / Convert.ToInt32 (b));
				}
				break;

			case Code.Div_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					if (a is double || b is double)
						throw new NotImplementedException ();
					else if (a is float || b is float)
						throw new NotImplementedException ();
					else if (a is long || b is long)
						stack.Push (Convert.ToUInt64 (a) / Convert.ToUInt64 (b));
					else
						stack.Push (Convert.ToUInt32 (a) / Convert.ToUInt32 (b));
				}
				break;

			case Code.Rem: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					if (a is double || b is double)
						stack.Push (Convert.ToDouble (a) % Convert.ToDouble (b));
					else if (a is float || b is float)
						stack.Push (Convert.ToSingle (a) % Convert.ToSingle (b));
					else if (a is long || b is long)
						stack.Push (Convert.ToInt64 (a) % Convert.ToInt64 (b));
					else
						stack.Push (Convert.ToInt32 (a) % Convert.ToInt32 (b));
				}
				break;

			case Code.Rem_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					if (a is double || b is double)
						throw new NotImplementedException ();
					else if (a is float || b is float)
						throw new NotImplementedException ();
					else if (a is long || b is long)
						stack.Push (Convert.ToUInt64 (a) % Convert.ToUInt64 (b));
					else
						stack.Push (Convert.ToUInt32 (a) % Convert.ToUInt32 (b));
				}
				break;
			#endregion

			#region 位操作
			//计算两个值的按位“与”并将结果推送到计算堆栈上。
			case Code.And: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (Convert.ToInt64 (a) & Convert.ToInt64 (b));
				}
				break;

			//计算位于堆栈顶部的两个整数值的按位求补并将结果推送到计算堆栈上。
			case Code.Or: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (Convert.ToInt64 (a) | Convert.ToInt64 (b));
				}
				break;

			//计算位于计算堆栈顶部的两个值的按位异或，并且将结果推送到计算堆栈上。
			case Code.Xor: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (Convert.ToInt64 (a) ^ Convert.ToInt64 (b));
				}
				break;

			//将整数值左移（用零填充）指定的位数，并将结果推送到计算堆栈上。
			case Code.Shl: {
					var b = stack.Pop ();
					var a = stack.Pop ();
					
					stack.Push (Convert.ToInt64(a) << Convert.ToInt32(b));
				}
				break;

			//将整数值右移（保留符号）指定的位数，并将结果推送到计算堆栈上。
			case Code.Shr: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (Convert.ToInt64 (a) >> Convert.ToInt32 (b));
				}
				break;

			//将无符号整数值右移（用零填充）指定的位数，并将结果推送到计算堆栈上。
			case Code.Shr_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (Convert.ToUInt64 (a) >> Convert.ToInt32 (b));
				}
				break;

			//对一个值执行求反并将结果推送到计算堆栈上。
			case Code.Neg:
				stack.Push (-Convert.ToInt64 (stack.Pop ()));
				break;

			//计算堆栈顶部整数值的按位求补并将结果作为相同的类型推送到计算堆栈上。
			case Code.Not:
				stack.Push (~Convert.ToInt64(stack.Pop ()));
				break;
			#endregion

			#region 比较结果
			//比较两个值。如果这两个值相等，则将整数值 1 (int32) 推送到计算堆栈上；否则，将 0 (int32) 推送到计算堆栈上。
			case Code.Ceq: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (object.Equals (a, b) ? 1 : 0);
				}
				break;

			//比较两个值。如果第一个值大于第二个值，则将整数值 1 (int32) 推送到计算堆栈上；反之，将 0 (int32) 推送到计算堆栈上。
			case Code.Cgt:
			case Code.Cgt_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (Compare(a, b) > 0 ? 1 : 0);
				}
				break;

			//比较两个值。如果第一个值小于第二个值，则将整数值 1 (int32) 推送到计算堆栈上；反之，将 0 (int32) 推送到计算堆栈上。
			case Code.Clt:
			case Code.Clt_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (Compare(a, b) < 0 ? 1 : 0);
				}
				break;
#endregion

#region 控制转移
			//无条件地将控制转移到目标指令。
			case Code.Br_S:
			case Code.Br:
				next = (Instruction)instruction.Operand;
				break;

			//如果 value 为 false、空引用（Visual Basic 中的 Nothing）或零，则将控制转移到目标指令。
			case Code.Brfalse_S:
			case Code.Brfalse: {
					var value = stack.Pop ();
					if (value == null || (value.GetType ().IsValueType && Convert.ToInt64 (value) == 0))
						next = (Instruction)instruction.Operand;
				}
				break;

			//如果 value 为 true、非空或非零，则将控制转移到目标指令。
			case Code.Brtrue_S:
			case Code.Brtrue: {
					var value = stack.Pop ();
					if (value != null && (!value.GetType ().IsValueType || Convert.ToInt64 (value) != 0))
						next = (Instruction)instruction.Operand;
				}
				break;

			//如果两个值相等，则将控制转移到目标指令。
			case Code.Beq_S:
			case Code.Beq: {
					var b = stack.Pop ();
					var a = stack.Pop ();
					if (object.Equals (a, b))
						next = (Instruction)instruction.Operand;
				}
				break;
			//如果第一个值大于或等于第二个值，则将控制转移到目标指令。
			case Code.Bge_S:
			case Code.Bge:
			case Code.Bge_Un_S:
			case Code.Bge_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();
					if (Compare (a, b) >= 0)
						next = (Instruction)instruction.Operand;
				}
				break;
			//如果第一个值大于第二个值，则将控制转移到目标指令。
			case Code.Bgt_S:
			case Code.Bgt:
			case Code.Bgt_Un_S:
			case Code.Bgt_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();
					if (Compare (a, b) > 0)
						next = (Instruction)instruction.Operand;
				}
				break;
			//如果第一个值小于或等于第二个值，则将控制转移到目标指令。
			case Code.Ble_S:
			case Code.Ble:
			case Code.Ble_Un_S:
			case Code.Ble_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();
					if (Compare (a, b) <= 0)
						next = (Instruction)instruction.Operand;
				}
				break;
			//如果第一个值小于第二个值，则将控制转移到目标指令。
			case Code.Blt_S:
			case Code.Blt:
			case Code.Blt_Un_S:
			case Code.Blt_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();
					if (Compare (a, b) < 0)
						next = (Instruction)instruction.Operand;
				}
				break;

			case Code.Leave_S:
			case Code.Leave:
				next = (Instruction)instruction.Operand;
				break;
			#endregion

			#region 类型转换
			//将位于计算堆栈顶部的值转换为 int32。
			case Code.Conv_I1:
			case Code.Conv_I2:
			case Code.Conv_I4: {
					var value = stack.Pop ();
					if (value is float || value is double)
						stack.Push ((int)Convert.ToDouble (value));
					else
						stack.Push (Convert.ToInt32(value));
				}
				break;

			case Code.Conv_I8: {
					var value = stack.Pop ();
					if (value is float || value is double)
						stack.Push ((long)Convert.ToDouble (value));
					else
						stack.Push (Convert.ToInt64 (value));
				}
				break;

			case Code.Conv_R4:
				stack.Push (Convert.ToSingle (stack.Pop ()));
				break;

			case Code.Conv_R8:
				stack.Push (Convert.ToDouble (stack.Pop ()));
				break;
			#endregion

			//推送对元数据中存储的字符串的新对象引用。
			case Code.Ldstr:
				stack.Push (instruction.Operand);
				break;

			case Code.Box:
			case Code.Unbox:
			case Code.Unbox_Any:
				break;

			#region 函数和委托
			case Code.Call:
			case Code.Callvirt:
				if (instruction.Operand is MethodDefinition)
					Execute (instruction.Operand as MethodDefinition, instruction.OpCode.Code);
				else
					Execute (instruction.Operand as MethodReference, instruction.OpCode.Code);
				break;

			//从当前方法返回，并将返回值（如果存在）从调用方的计算堆栈推送到被调用方的计算堆栈上。
			case Code.Ret:
				next = null;
				break;

			case Code.Ldftn:
			case Code.Ldvirtftn:
				stack.Push (instruction.Operand);
				break;
#endregion

			case Code.Ldtoken:
				stack.Push (((FieldDefinition)instruction.Operand).InitialValue);
				break;

			#region 类和对象
			//创建一个值类型的新对象或新实例，并将对象引用（O 类型）推送到计算堆栈上。
			case Code.Newobj:
				if (instruction.Operand is MethodDefinition)
					Execute (instruction.Operand as MethodDefinition, instruction.OpCode.Code);//构造函数
				else
					Execute (instruction.Operand as MethodReference, instruction.OpCode.Code);//构造函数
				break;
				
			//将位于指定地址的值类型的每个字段初始化为空引用或适当的基元类型的 0。
			case Code.Initobj:{
					var typeReference = instruction.Operand as TypeReference;
					var classType = GetType (typeReference, methodDefinition);
			
					var objects = new object [] { 0 };

					var methodReference = methodDefinition;

					if (methodReference.IsGenericInstance) {
						classType = GetType (methodReference.DeclaringType.GetElementType (), methodReference);
						var types = GetParameters (methodReference);
						var genericType = classType.MakeGenericType (types);
						var instance = Activator.CreateInstance (genericType, objects);
						stack.Push (instance);
					} else {
						var instance = Activator.CreateInstance (classType, objects);
						stack.Push (instance);
					}
				}
				break;

			//测试对象引用（O 类型）是否为特定类的实例。
			case Code.Isinst: {
					var typeName = (instruction.Operand as TypeReference).FullName;
					var value = stack.Pop ();
					if(value == null) {
						stack.Push (null);
						break;
					}

					if (value is ILObject)
						stack.Push ((value as ILObject).type.FullName == typeName ? value : null);
					else
						stack.Push (value.GetType().FullName == typeName ? value : null);
				}
				break;

			//用新值替换在对象引用或指针的字段中存储的值。
			case Code.Stfld: {
					var fieldReference = (instruction.Operand as FieldReference);
					var value = stack.Pop ();
					var obj = stack.Pop ();
					if(obj is ILObject)
						(obj as ILObject).SetValue(fieldReference, value);
					else
						obj.SetFieldValue (fieldReference.Name, value);
				}
				break;

			//查找对象中其引用当前位于计算堆栈的字段的值。
			case Code.Ldfld:
			case Code.Ldflda: {
					var fieldReference = (instruction.Operand as FieldReference);
					var obj = stack.Pop ();
					object value = null;
					if (obj is ILObject)
						value = (obj as ILObject).GetValue (fieldReference);
					else
						value = obj.GetFieldValue (fieldReference.Name);
					stack.Push (value);
				}
				break;

			//用来自计算堆栈的值替换静态字段的值。
			case Code.Stsfld: {
					var memberReference = instruction.Operand as MemberReference;

					var value = stack.Pop ();
					staticFields [memberReference.DeclaringType] [memberReference.Name] = value;
				}
				break;

			//将静态字段的值推送到计算堆栈上。
			case Code.Ldsfld:
			case Code.Ldsflda: {
					var memberReference = instruction.Operand as MemberReference;

					staticFields [memberReference.DeclaringType].TryGetValue (memberReference.Name, out object value);
					stack.Push (value); ;
				}
				break;

			//将空引用（O 类型）推送到计算堆栈上。
			case Code.Ldnull:
				stack.Push (null);
				break;
#endregion

#region 数组
			//将对新的从零开始的一维数组（其元素属于特定类型）的对象引用推送到计算堆栈上。
			case Code.Newarr: {
					var type = GetType (instruction.Operand as TypeReference);

					var length = Convert.ToInt32 (stack.Pop ());
					var array = Array.CreateInstance (type, length);
					stack.Push (array);
				}
				break;

			//将从零开始的、一维数组的元素的数目推送到计算堆栈上。
			case Code.Ldlen:
				stack.Push ((stack.Pop () as Array).Length);
				break;

			//将位于指定数组索引处的 xx 类型的元素作为 xx 加载到计算堆栈的顶部。
			case Code.Ldelem_I1:
			case Code.Ldelem_U1:
			case Code.Ldelem_I2:
			case Code.Ldelem_U2:
			case Code.Ldelem_I4:
			case Code.Ldelem_U4:
			case Code.Ldelem_I8:
			case Code.Ldelem_R4:
			case Code.Ldelem_R8:
			case Code.Ldelem_Ref: {
					var index = Convert.ToInt32 (stack.Pop ());
					var array = stack.Pop () as Array;
					stack.Push (array.GetValue (index));
				}
				break;

			//用计算堆栈上的 xx 值替换给定索引处的数组元素。
			case Code.Stelem_I1:
			case Code.Stelem_I2:
			case Code.Stelem_I4:
			case Code.Stelem_I8:
			case Code.Stelem_R4:
			case Code.Stelem_R8:
			case Code.Stelem_Ref: {
					var value = stack.Pop ();
					var index = Convert.ToInt32 (stack.Pop ());
					var array = stack.Pop () as Array;
					array.SetValue (value, index);
				}
				break;
#endregion

#region 异常处理
			case Code.Endfinally:
			case Code.Endfilter:
				break;

			case Code.Throw:
			case Code.Rethrow:
				Console.WriteLine ((stack.Pop () as Exception).Message);
				next = null;
				break;
#endregion

			default:
				throw new NotSupportedException ("Not supported " + instruction);
			}

			return next;
		}
	}
}
