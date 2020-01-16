﻿
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace ILRT {
	class Program {
		static Stack<object> stack = new Stack<object> ();
		static Dictionary<int, object> localVars = new Dictionary<int, object> ();//局部变量
		static Stack<Dictionary<int, object>> localVarsStack = new Stack<Dictionary<int, object>> ();//局部变量栈
		static object [] localArgs = null;//函数参数
		static Stack<object []> localArgsStack = new Stack<object []> ();//函数参数栈

		static Dictionary<TypeReference, Dictionary<string, object>> staticFields = new Dictionary<TypeReference, Dictionary<string, object>> ();//静态字段

		static Type GetType (TypeReference typeReference)
		{
			var typeName = typeReference.FullName;

			if (typeReference is GenericInstanceType) {
				typeName = (typeReference as TypeSpecification).ElementType.FullName;
			}

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

		static int Compare (object a, object b)
		{
			if (a is double || b is double || a is float || b is float) {
				var result = Convert.ToDouble (a) - Convert.ToDouble (b);
				return Convert.ToInt32 (result);
			}

			return Convert.ToInt32 (a) - Convert.ToInt32 (b);
		}

		static void Main (string [] args)
		{
			var module = ModuleDefinition.ReadModule (@"F:\workspace\GameCenter\HotFix_DLL\HotFix_Project.dll");
			foreach (var type in module.Types) {
				//if (!type.IsPublic)
				//	continue;

				//Console.WriteLine (type.FullName + " " + type.Methods.Count);

				if (type.FullName != "GameCenter.Program")
					continue;

				foreach (var methodDefinition in type.Methods) {

					if (methodDefinition.Name == "Main") {
						Console.WriteLine (methodDefinition);

						Execute (methodDefinition);

						break;
					}
				}

				break;
			}


		}

		static void Execute (MethodReference methodReference)
		{
			//if(methodReference.FullName == "System.Void System.Object::.ctor()")
			//	return;

			if (methodReference.FullName == "System.Void System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(System.Array,System.RuntimeFieldHandle)") {
				var bytes = stack.Pop () as Array;
				var array = stack.Pop () as Array;

				Buffer.BlockCopy (bytes, 0, array, 0, bytes.Length);
				return;
			}

			object obj = null;

			if (methodReference.HasThis)
				obj = stack.Pop ();

			var parameters = methodReference.Parameters;

			var types = new Type [parameters.Count];
			var objects = new object [parameters.Count];

			for (var i = 0; i < parameters.Count; i++) {

				var type = GetType (parameters [i].ParameterType);

				if (type == null)
					type = typeof (object);

				types [i] = type;

				var value = stack.Pop ();

				switch (type.ToString ()) {
				case "System.Boolean":
					value = Convert.ToBoolean (value);
					break;
				case "System.Int32":
					value = Convert.ToInt32 (value);
					break;
				case "System.Int64":
					value = Convert.ToInt64 (value);
					break;
				case "System.Single":
					value = Convert.ToSingle (value);
					break;
				case "System.Double":
					value = Convert.ToDouble (value);
					break;
				}

				objects [parameters.Count - 1 - i] = value;
			}

			var classType = GetType (methodReference.DeclaringType);

			MethodBase method = null;

			if (methodReference.Name == ".ctor")
				method = classType.GetConstructor (types);//, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
			else
				method = classType.GetMethod (methodReference.Name, types);//, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic

			var resultValue = method.Invoke (obj, objects);

			var methodInfo = method as MethodInfo;
			if (methodInfo == null)
				stack.Push (obj);
			else if (methodInfo.ReturnType.FullName != "System.Void")
				stack.Push (resultValue);
		}

		static void Execute (MethodDefinition methodDefinition)
		{
			if (!staticFields.ContainsKey (methodDefinition.DeclaringType)) {
				var dictionary = new Dictionary<string, object> ();

				staticFields [methodDefinition.DeclaringType] = dictionary;

				foreach (var method in methodDefinition.DeclaringType.Methods) {
					if (method.Name == ".cctor")//静态构造函数
					{
						Execute (method);
						break;
					}
				}
			}

			var parameters = methodDefinition.Parameters;

			localArgsStack.Push (localArgs);
			localArgs = new object [parameters.Count + (methodDefinition.HasThis ? 1 : 0)];

			if (methodDefinition.HasThis)
				localArgs [0] = stack.Pop ();

			for (var i = 0; i < parameters.Count; i++) {
				localArgs [localArgs.Length - 1 - i] = stack.Pop ();
			}

			localVarsStack.Push (localVars);
			localVars = new Dictionary<int, object> ();

			if (methodDefinition.HasBody) {
				var instruction = methodDefinition.Body.Instructions [0]; ;

				ExceptionHandler finallyHandler = null;

				do {
					try {
						instruction = Execute (instruction);

						if (finallyHandler != null && instruction == null) {
							instruction = finallyHandler.HandlerStart;
							finallyHandler = null;
						}
					}
					catch (Exception e) {

						var exceptionHandlers = methodDefinition.Body.ExceptionHandlers;

						if (exceptionHandlers == null || exceptionHandlers.Count == 0) {
							Console.WriteLine (e);
							break;
						}

						foreach (var exceptionHandler in exceptionHandlers) {

							if (exceptionHandler.HandlerType == ExceptionHandlerType.Catch && GetType (exceptionHandler.CatchType).IsAssignableFrom (e.GetType ())) {
								stack.Push (e);
								instruction = exceptionHandler.HandlerStart;
								break;
							}
						}

						var lastExceptionHandler = exceptionHandlers [exceptionHandlers.Count - 1];

						if (lastExceptionHandler.HandlerType == ExceptionHandlerType.Finally)
							finallyHandler = lastExceptionHandler;
					}
				} while (instruction != null);
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

		static Instruction Execute (Instruction instruction)
		{
			//Console.WriteLine (instruction);

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
			case Code.Ldloc_3:
				stack.Push (localVars [instruction.OpCode.Code - Code.Ldloc_0]);
				break;

			//将指定索引处的局部变量加载到计算堆栈上。
			case Code.Ldloc_S:
			case Code.Ldloc:
			//将位于特定索引处的局部变量的地址加载到计算堆栈上。
			case Code.Ldloca_S:
			case Code.Ldloca:
				stack.Push (localVars [((VariableReference)instruction.Operand).Index]);
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

			//case Code.And:
			//case Code.Or:
			//case Code.Xor:
			//case Code.Shl:
			//case Code.Shr:
			//case Code.Shr_Un:
			//case Code.Neg:
			//case Code.Not:

			#region 比较结果
			//比较两个值。如果这两个值相等，则将整数值 1 (int32) 推送到计算堆栈上；否则，将 0 (int32) 推送到计算堆栈上。
			case Code.Ceq: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (a.Equals (b) ? 1 : 0);
				}
				break;

			//比较两个值。如果第一个值大于第二个值，则将整数值 1 (int32) 推送到计算堆栈上；反之，将 0 (int32) 推送到计算堆栈上。
			case Code.Cgt:
			case Code.Cgt_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (Convert.ToDouble (a) > Convert.ToDouble (b) ? 1 : 0);
				}
				break;

			//比较两个值。如果第一个值小于第二个值，则将整数值 1 (int32) 推送到计算堆栈上；反之，将 0 (int32) 推送到计算堆栈上。
			case Code.Clt:
			case Code.Clt_Un: {
					var b = stack.Pop ();
					var a = stack.Pop ();

					stack.Push (Convert.ToDouble (a) < Convert.ToDouble (b) ? 1 : 0);
				}
				break;
			#endregion

			//推送对元数据中存储的字符串的新对象引用。
			case Code.Ldstr:
				stack.Push (instruction.Operand);
				break;

			case Code.Box:
				Console.WriteLine (instruction);
				break;
			case Code.Unbox:
			case Code.Unbox_Any:
				Console.WriteLine (instruction);
				break;

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
					if (a.Equals (b))
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

			//将位于计算堆栈顶部的值转换为 int32。
			case Code.Conv_I1:
			case Code.Conv_I2:
			case Code.Conv_I4: {
					var value = stack.Pop ();
					if (value is float || value is double)
						stack.Push ((int)Convert.ToDouble (value));
					else
						stack.Push ((int)value);
				}
				break;

			case Code.Conv_I8: {
					var value = stack.Pop ();
					if (value is float || value is double)
						stack.Push ((Int64)Convert.ToDouble (value));
					else
						stack.Push ((Int64)value);
				}
				break;

			case Code.Conv_R4:
				stack.Push (Convert.ToSingle (stack.Pop ()));
				break;

			case Code.Conv_R8:
				stack.Push (Convert.ToDouble (stack.Pop ()));
				break;

			#region 函数和委托
			case Code.Call:
			case Code.Callvirt: {

					if (instruction.Operand is MethodDefinition) {

						Execute (instruction.Operand as MethodDefinition);
						break;
					}

					if (instruction.Operand is GenericInstanceMethod) {
						var methodReference = instruction.Operand as GenericInstanceMethod;

						var parameters = methodReference.Parameters;

						var types = new Type [parameters.Count];
						var objects = new object [parameters.Count];

						for (var i = 0; i < parameters.Count; i++) {

							var type = GetType (parameters [i].ParameterType);

							if (type == null)
								type = typeof (object);

							types [i] = type;

							var value = stack.Pop ();

							switch (type.ToString ()) {
							case "System.Boolean":
								value = Convert.ToBoolean (value);
								break;
							case "System.Int32":
								value = Convert.ToInt32 (value);
								break;
							case "System.Int64":
								value = Convert.ToInt64 (value);
								break;
							case "System.Single":
								value = Convert.ToSingle (value);
								break;
							case "System.Double":
								value = Convert.ToDouble (value);
								break;
							}

							objects [parameters.Count - 1 - i] = value;
						}

						var classType = GetType (methodReference.DeclaringType);

						var methodInfo = classType.GetMethod (methodReference.Name, types);//, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic

						//if (methodInfo == null) {
						//	var methods = classType.GetMethods ();
						//	Console.WriteLine (methods);
						//}
						methodInfo = classType.GetMethod (methodReference.Name);

						object obj = null;

						if (methodReference.HasThis)
							obj = stack.Pop ();

						var resultValue = methodInfo.Invoke (obj, objects);

						if (methodInfo.ReturnType.FullName != "System.Void")
							stack.Push (resultValue);
						break;
					}

					if (instruction.Operand is MethodReference) {
						Execute (instruction.Operand as MethodReference);
						break;
					}
				}
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
				if (instruction.Operand is MethodDefinition) {
					stack.Push (new Dictionary<string, object> ());
					Execute (instruction.Operand as MethodDefinition);//构造函数
					break;
				}

				if (instruction.Operand is MethodReference) {
					var methodReference = instruction.Operand as MethodReference;
					var type = GetType (methodReference.DeclaringType);
					var obj = type.Assembly.CreateInstance (type.FullName);
					stack.Push (obj);
					Execute (methodReference);//构造函数
					break;
				}

				throw new NotSupportedException ("Not supported " + instruction);

			//用新值替换在对象引用或指针的字段中存储的值。
			case Code.Stfld: {
					var key = (instruction.Operand as MemberReference).Name;//FieldDefinition
					var value = stack.Pop ();
					var dictionary = stack.Pop () as Dictionary<string, object>;
					dictionary [key] = value;
				}
				break;

			//查找对象中其引用当前位于计算堆栈的字段的值。
			case Code.Ldfld:
			case Code.Ldflda: {
					var key = (instruction.Operand as MemberReference).Name;//FieldDefinition
					var dictionary = stack.Pop () as Dictionary<string, object>;
					dictionary.TryGetValue (key, out object value);
					stack.Push (value); ;
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

					if (type == null)
						type = typeof (object);

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
