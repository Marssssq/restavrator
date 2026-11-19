using System;
using System.Reflection;
using UnityEngine;

public class AssertionException : Exception
{
    public AssertionException(string message) : base(message)
    {
    }
}

public static class TestAssert
{
    public static void IsTrue(bool condition, string message = "")
    {
        if (!condition)
        {
            throw new AssertionException(string.IsNullOrEmpty(message) ? "Expected true but was false." : message);
        }
    }

    public static void IsFalse(bool condition, string message = "")
    {
        if (condition)
        {
            throw new AssertionException(string.IsNullOrEmpty(message) ? "Expected false but was true." : message);
        }
    }

    public static void AreEqual<T>(T expected, T actual, string message = "")
    {
        if (!object.Equals(expected, actual))
        {
            string detail = $"Expected: <{expected}>, Actual: <{actual}>. " + message;
            throw new AssertionException(detail);
        }
    }

    public static void AreNotEqual<T>(T expected, T actual, string message = "")
    {
        if (object.Equals(expected, actual))
        {
            string detail = $"Expected not equal to <{expected}>, but values were equal. " + message;
            throw new AssertionException(detail);
        }
    }

    public static void IsNotNull(object obj, string message = "")
    {
        if (obj == null)
        {
            throw new AssertionException(string.IsNullOrEmpty(message) ? "Expected non-null object reference." : message);
        }
    }

    public static void IsNull(object obj, string message = "")
    {
        if (obj != null)
        {
            throw new AssertionException(string.IsNullOrEmpty(message) ? $"Expected null but was <{obj}>." : message);
        }
    }

    public static void IsInRange(float value, float min, float max, string message = "")
    {
        if (value < min || value > max)
        {
            string detail = $"Value {value} out of range [{min}, {max}]. " + message;
            throw new AssertionException(detail);
        }
    }

    public static void IsInRange(int value, int min, int max, string message = "")
    {
        if (value < min || value > max)
        {
            string detail = $"Value {value} out of range [{min}, {max}]. " + message;
            throw new AssertionException(detail);
        }
    }

    public static void Fail(string message)
    {
        throw new AssertionException(message);
    }

    public static T GetField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            throw new AssertionException($"Field '{fieldName}' not found on type {target.GetType().FullName}.");
        }
        return (T)field.GetValue(target);
    }

    public static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            throw new AssertionException($"Field '{fieldName}' not found on type {target.GetType().FullName}.");
        }
        field.SetValue(target, value);
    }

    public static object InvokeMethod(object target, string methodName, params object[] parameters)
    {
        Type targetType = target.GetType();
        Type[] paramTypes = parameters != null ? Array.ConvertAll(parameters, p => p != null ? p.GetType() : typeof(object)) : Type.EmptyTypes;
        MethodInfo method = null;
        try
        {
            method = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, paramTypes, null);
        }
        catch (AmbiguousMatchException)
        {
        }
        if (method == null)
        {
            MethodInfo[] allMethods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            for (int i = 0; i < allMethods.Length; i++)
            {
                if (allMethods[i].Name == methodName)
                {
                    ParameterInfo[] pis = allMethods[i].GetParameters();
                    if (parameters == null && pis.Length == 0) { method = allMethods[i]; break; }
                    if (parameters != null && pis.Length == parameters.Length) { method = allMethods[i]; break; }
                }
            }
        }
        if (method == null)
        {
            throw new AssertionException($"Method '{methodName}' not found on type {target.GetType().FullName}.");
        }
        return method.Invoke(target, parameters);
    }

    public static LuaBootstrapper CreateBootstrapper()
    {
        GameObject go = new GameObject("TestBootstrapper");
        LuaBootstrapper bootstrapper = go.AddComponent<LuaBootstrapper>();
        MethodInfo startMethod = typeof(LuaBootstrapper).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (startMethod != null)
        {
            startMethod.Invoke(bootstrapper, null);
        }
        return bootstrapper;
    }

    public static void DestroyBootstrapper(LuaBootstrapper bootstrapper)
    {
        if (bootstrapper != null && bootstrapper.gameObject != null)
        {
            UnityEngine.Object.DestroyImmediate(bootstrapper.gameObject);
        }
        GameObject canvas = GameObject.Find("RestorerWorkshopCanvas");
        if (canvas != null)
        {
            UnityEngine.Object.DestroyImmediate(canvas);
        }
        GameObject es = GameObject.Find("EventSystem");
        if (es != null)
        {
            UnityEngine.Object.DestroyImmediate(es);
        }
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(rootObjects[i]);
            }
        }
        AudioListener.volume = 1f;
        AudioListener.pause = false;
        Time.timeScale = 1f;
    }
}
