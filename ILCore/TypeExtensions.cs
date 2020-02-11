using System;
using System.Linq;
using System.Reflection;

namespace GameCenter.ExtensionMethods
{
    public static class TypeExtensions
    {
        public static MethodInfo GetGenericMethod(this Type type, string methodName, Type[] parameterTypes)
        {
            var methods = type.GetMethods();
            foreach (var method in methods.Where(m => m.Name == methodName))
            {
                //var methodGenericArgumentTypes = method.GetGenericArguments().Select(p => p.ReflectedType).ToArray();
                var methodParameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

                if (methodParameterTypes.SequenceEqual(parameterTypes))
                {
                    return method;
                }
            }

            return null;
        }
    }
}