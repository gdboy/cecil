using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEngine;
#endif


public static class AccessExtensions
{
    public static T InvokeConstructor<T>(this Type type, Type[] paramTypes = null, object[] paramValues = null)
    {
        return (T)type.InvokeConstructor(paramTypes, paramValues);
    }

    public static object InvokeConstructor(this Type type, Type[] paramTypes = null, object[] paramValues = null)
    {
        if (paramTypes == null || paramValues == null)
        {
            paramTypes = new Type[] { };
            paramValues = new object[] { };
        }

        var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, paramTypes, null);

        return constructor.Invoke(paramValues);
    }

    public static MethodInfo GetMethod(this object o, string methodName)
    {
        var type = o.GetType();

        MethodInfo method = null;

        try
        {
            do
            {
                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method != null)
                    break;

                type = type.BaseType;
            } while (type != null);
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogError(string.Format("GetMethod {0}, {1}", o, methodName));
            Debug.LogException(e);
#else
			Console.WriteLine (string.Format ("GetMethod {0}, {1}", o, methodName));
			Console.WriteLine (e);
#endif
		}

        return method;
    }

    public static T Invoke<T>(this object o, string methodName, params object[] args)
    {
        var value = o.Invoke(methodName, args);
        if (value != null)
        {
            return (T)value;
        }

        return default(T);
    }

    public static object Invoke(this object o, string methodName, params object[] args)
    {
        var type = o.GetType();

        var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        return method?.Invoke(o, args);
    }

    public static T GetFieldValue<T>(this object o, string name)
    {
        var value = o.GetFieldValue(name);
        if (value != null)
        {
            return (T)value;
        }

        return default(T);
    }

    public static object GetFieldValue(this object o, string name)
    {
        var field = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            return field.GetValue(o);
        }

        return null;
    }

    public static void SetFieldValue(this object o, string name, object value)
    {
        var field = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(o, value);
        }
    }

    public static T GetPropertyValue<T>(this object o, string name)
    {
        var value = o.GetPropertyValue(name);
        if (value != null)
        {
            return (T)value;
        }

        return default(T);
    }

    public static object GetPropertyValue(this object o, string name)
    {
        var property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null)
        {
            return property.GetValue(o, null);
        }

        return null;
    }

    public static void SetPropertyValue(this object o, string name, object value)
    {
        var property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null)
        {
            property.SetValue(o, value, null);
        }
    }
}