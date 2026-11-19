using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method)]
public class E2ETestAttribute : Attribute
{
}

public class TestRunResult
{
    public string TierName;
    public string TestName;
    public bool Passed;
    public string ErrorMessage;
    public double DurationMs;
}

public static class E2ETestRunner
{
    public static void RunAllTests()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Type[] tierTypes = new Type[]
        {
            typeof(Tier1_FeatureCoverageTests),
            typeof(Tier2_BoundaryCornerTests),
            typeof(Tier3_CrossFeatureTests),
            typeof(Tier4_ApplicationScenarioTests)
        };

        List<TestRunResult> results = new List<TestRunResult>();
        int passCount = 0;
        int failCount = 0;
        Stopwatch totalWatch = Stopwatch.StartNew();

        UnityEngine.Debug.Log("==================================================");
        UnityEngine.Debug.Log("Starting E2E Automated Test Suite Execution");
        UnityEngine.Debug.Log("==================================================");

        foreach (Type tierType in tierTypes)
        {
            string tierName = tierType.Name;
            UnityEngine.Debug.Log($"--- Running Tier: {tierName} ---");

            object tierInstance = Activator.CreateInstance(tierType);
            MethodInfo[] methods = tierType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                bool isTest = method.Name.StartsWith("Test_") || method.GetCustomAttribute<E2ETestAttribute>() != null;
                if (!isTest) continue;

                CleanupWorkshopArtifacts();
                Stopwatch testWatch = Stopwatch.StartNew();
                bool passed = false;
                string errorMessage = null;

                try
                {
                    method.Invoke(tierInstance, null);
                    passed = true;
                    passCount++;
                    UnityEngine.Debug.Log($"[PASS] {tierName}.{method.Name} ({testWatch.ElapsedMilliseconds} ms)");
                }
                catch (TargetInvocationException tie)
                {
                    failCount++;
                    Exception inner = tie.InnerException ?? tie;
                    errorMessage = inner.Message;
                    UnityEngine.Debug.LogError($"[FAIL] {tierName}.{method.Name}: {inner.Message}\n{inner.StackTrace}");
                }
                catch (Exception ex)
                {
                    failCount++;
                    errorMessage = ex.Message;
                    UnityEngine.Debug.LogError($"[FAIL] {tierName}.{method.Name}: {ex.Message}\n{ex.StackTrace}");
                }
                finally
                {
                    testWatch.Stop();
                    CleanupWorkshopArtifacts();
                }

                results.Add(new TestRunResult
                {
                    TierName = tierName,
                    TestName = method.Name,
                    Passed = passed,
                    ErrorMessage = errorMessage,
                    DurationMs = testWatch.ElapsedMilliseconds
                });
            }
        }

        totalWatch.Stop();
        double totalSeconds = totalWatch.Elapsed.TotalSeconds;

        UnityEngine.Debug.Log("==================================================");
        UnityEngine.Debug.Log($"E2E Test Suite Completed in {totalSeconds.ToString("F2", CultureInfo.InvariantCulture)} seconds");
        UnityEngine.Debug.Log($"Total Tests: {results.Count} | Passed: {passCount} | Failed: {failCount}");
        UnityEngine.Debug.Log("==================================================");

        WriteJsonReport(results, passCount, failCount, totalSeconds);

        EditorApplication.Exit(failCount == 0 ? 0 : 1);
    }

    private static void CleanupWorkshopArtifacts()
    {
        LuaBootstrapper[] allBootstrappers = UnityEngine.Object.FindObjectsByType<LuaBootstrapper>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allBootstrappers.Length; i++)
        {
            if (allBootstrappers[i] != null && allBootstrappers[i].gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(allBootstrappers[i].gameObject);
            }
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

    private static void WriteJsonReport(List<TestRunResult> results, int passCount, int failCount, double durationSeconds)
    {
        Directory.CreateDirectory("Build");
        string reportPath = Path.Combine("Build", "test_results.json");

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"totalTests\": {results.Count},");
        sb.AppendLine($"  \"passed\": {passCount},");
        sb.AppendLine($"  \"failed\": {failCount},");
        sb.AppendLine($"  \"durationSeconds\": {durationSeconds.ToString("F2", CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"timestampUtc\": \"{DateTime.UtcNow:O}\",");
        sb.AppendLine("  \"results\": [");

        for (int i = 0; i < results.Count; i++)
        {
            TestRunResult r = results[i];
            sb.AppendLine("    {");
            sb.AppendLine($"      \"tier\": \"{EscapeJson(r.TierName)}\",");
            sb.AppendLine($"      \"name\": \"{EscapeJson(r.TestName)}\",");
            sb.AppendLine($"      \"passed\": {(r.Passed ? "true" : "false")},");
            sb.AppendLine($"      \"durationMs\": {r.DurationMs.ToString("F1", CultureInfo.InvariantCulture)},");
            sb.AppendLine($"      \"errorMessage\": {(r.ErrorMessage != null ? "\"" + EscapeJson(r.ErrorMessage) + "\"" : "null")}");
            sb.Append("    }");
            if (i < results.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");

        File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
        UnityEngine.Debug.Log($"Saved E2E test results report to {reportPath}");
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}
