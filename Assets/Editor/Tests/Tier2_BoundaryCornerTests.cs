using System;
using UnityEngine;
using UnityEngine.UI;

public class Tier2_BoundaryCornerTests
{
    public void Test_Boundary_RapidStationClicking_DoesNotDesyncPlayerTarget()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        string[] stationNames = { "Station_Window", "Station_Workbench", "Station_Cabinet", "Station_Upgrades" };

        for (int i = 0; i < 50; i++)
        {
            string sName = stationNames[i % stationNames.Length];
            GameObject sGO = GameObject.Find(sName);
            TestAssert.IsNotNull(sGO, $"Station {sName} must exist");
            sGO.GetComponent<Button>().onClick.Invoke();
        }

        Vector2 targetPos = TestAssert.GetField<Vector2>(bs, "targetPos");
        TestAssert.IsInRange(targetPos.x, -365f, 520f, "Target X must stay within valid room boundaries");
        TestAssert.IsInRange(targetPos.y, -230f, 100f, "Target Y must stay within valid room boundaries");
        TestAssert.IsNotNull(TestAssert.GetField<string>(bs, "pendingStation"), "Pending station must not be null");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Boundary_ScrubbingSpeedViolation_InflictsOverScrubDamage()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "CLEAN");

        byte[,] dirtMap = TestAssert.GetField<byte[,]>(bs, "dirtMap");
        dirtMap[160, 160] = 1;
        TestAssert.SetField(bs, "overScrubDamage", 0f);

        TestAssert.InvokeMethod(bs, "ApplyScrubbing", Vector2.zero, 0.95f, 0.5f);

        TestAssert.AreEqual((byte)3, dirtMap[160, 160], "Pixel marked with chemical damage code 3 by ApplyScrubbing");
        float damage = TestAssert.GetField<float>(bs, "overScrubDamage");
        TestAssert.IsTrue(damage > 0f, "Over-scrub damage must be inflicted when speedRatio exceeds 0.72 threshold");
        TestAssert.AreEqual(2.5f, damage, "Damage amount should equal dt * 5.0 = 0.5 * 5.0 = 2.5");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Boundary_LowSpeedScrubbing_LeavesBaseLayerIntact()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "CLEAN");

        byte[,] dirtMap = TestAssert.GetField<byte[,]>(bs, "dirtMap");
        dirtMap[160, 160] = 1;
        TestAssert.SetField(bs, "overScrubDamage", 0f);

        TestAssert.InvokeMethod(bs, "ApplyScrubbing", Vector2.zero, 0.20f, 0.5f);

        TestAssert.AreEqual((byte)2, dirtMap[160, 160], "Pixel cleaned to code 2 without damage at safe speed");
        TestAssert.AreEqual(0f, TestAssert.GetField<float>(bs, "overScrubDamage"), "Damage must remain zero during gentle cleaning");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Boundary_GrindingBeforeCleanThreshold_TriggersOrderViolationError()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "GRIND");

        TestAssert.SetField(bs, "cleanProgress", 34);
        TestAssert.InvokeMethod(bs, "ApplyGrinding", Vector2.zero, 0.50f, 0.1f);
        Text hint = TestAssert.GetField<Text>(bs, "bottomHintText");
        TestAssert.IsTrue(hint.text.Contains("НАРУШЕН ПОРЯДОК") && hint.text.Contains("35%"), "Order violation warning displayed when cleanProgress is 34%");

        TestAssert.SetField(bs, "cleanProgress", 35);
        float[,] burrMap = TestAssert.GetField<float[,]>(bs, "burrMap");
        burrMap[160, 160] = 0.9f;
        TestAssert.InvokeMethod(bs, "ApplyGrinding", Vector2.zero, 0.50f, 0.1f);
        TestAssert.IsTrue(burrMap[160, 160] < 0.9f, "Grinding should successfully reduce burr when cleanProgress is 35%");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Boundary_GrindingUnderSpeed_ShowsUnderSpeedWarning()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "GRIND");
        TestAssert.SetField(bs, "cleanProgress", 50);

        Text hint = TestAssert.GetField<Text>(bs, "bottomHintText");
        hint.text = "";
        float[,] burrMap = TestAssert.GetField<float[,]>(bs, "burrMap");
        burrMap[160, 160] = 0.8f;

        TestAssert.InvokeMethod(bs, "ApplyGrinding", Vector2.zero, 0.15f, 0.1f);

        TestAssert.IsTrue(hint.text.Contains("Слишком медленно для шлифовки"), "Under-speed warning must be displayed by ApplyGrinding when speedRatio is 0.15");
        TestAssert.AreEqual(0.8f, burrMap[160, 160], "Burr must not be reduced when grinding under-speed");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Boundary_GemSettingDecayTimeout_ResetsClicksAfter065Seconds()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "SetupClickNodesForItem", "RING");
        TestAssert.SetField(bs, "viewState", "WORKBENCH");
        TestAssert.SetField(bs, "activeBenchAction", "CLICK_FIX");

        System.Collections.IList nodes = (System.Collections.IList)TestAssert.GetField<object>(bs, "activeClickNodes");
        object node = nodes[0];
        node.GetType().GetField("clicksLeft").SetValue(node, 2);
        node.GetType().GetField("resetTimer").SetValue(node, 0.65f);

        TestAssert.InvokeMethod(bs, "UpdateClickNodesDecay", 0.70f);

        int resetClicks = (int)node.GetType().GetField("clicksLeft").GetValue(node);
        TestAssert.AreEqual(5, resetClicks, "Clicks left must reset to 5 after timeout exceeds 0.65s");

        Text hint = TestAssert.GetField<Text>(bs, "bottomHintText");
        TestAssert.IsTrue(hint.text.Contains("СЛИШКОМ МЕДЛЕННО"), "Decay timeout must show speed warning");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Boundary_GemSettingRapidClicks_CompletesNodeBeforeDecay()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "SetupClickNodesForItem", "RING");
        TestAssert.SetField(bs, "viewState", "WORKBENCH");
        TestAssert.SetField(bs, "activeBenchAction", "CLICK_FIX");

        byte[,] dirtMap = TestAssert.GetField<byte[,]>(bs, "dirtMap");
        for (int y = 0; y < 320; y++)
        {
            for (int x = 0; x < 320; x++)
            {
                if (dirtMap[y, x] == 1) dirtMap[y, x] = 2;
            }
        }
        TestAssert.SetField(bs, "cleanProgress", 100);

        System.Collections.IList nodes = (System.Collections.IList)TestAssert.GetField<object>(bs, "activeClickNodes");
        object node = nodes[0];

        for (int i = 0; i < 5; i++)
        {
            TestAssert.InvokeMethod(bs, "OnClickFixNode", 0);
            TestAssert.InvokeMethod(bs, "UpdateClickNodesDecay", 0.05f);
        }

        int remaining = (int)node.GetType().GetField("clicksLeft").GetValue(node);
        TestAssert.AreEqual(0, remaining, "Node clicks must reach 0 when clicked rapidly");

        RectTransform uiRT = (RectTransform)node.GetType().GetField("uiRT").GetValue(node);
        TestAssert.IsFalse(uiRT.gameObject.activeSelf, "Completed node must be deactivated");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Boundary_PrematurePolishing_RejectsFinishBeforePrerequisites()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "FINISH");

        TestAssert.SetField(bs, "cleanProgress", 64);
        TestAssert.SetField(bs, "grindProgress", 80);
        TestAssert.SetField(bs, "clickFixProgress", 100);
        TestAssert.SetField(bs, "finishProgress", 0f);

        TestAssert.InvokeMethod(bs, "ApplyPolishing", 0.40f, 0.5f);
        Text hint = TestAssert.GetField<Text>(bs, "bottomHintText");
        TestAssert.IsTrue(hint.text.Contains("НАРУШЕН ПОРЯДОК"), "Order violation warning displayed when cleanProgress is 64%");
        TestAssert.AreEqual(0f, TestAssert.GetField<float>(bs, "finishProgress"), "Finish progress must stay 0 when cleanProgress is below 65%");

        TestAssert.SetField(bs, "cleanProgress", 80);
        TestAssert.SetField(bs, "grindProgress", 64);
        TestAssert.InvokeMethod(bs, "ApplyPolishing", 0.40f, 0.5f);
        TestAssert.AreEqual(0f, TestAssert.GetField<float>(bs, "finishProgress"), "Finish progress must stay 0 when grindProgress is below 65%");

        TestAssert.SetField(bs, "grindProgress", 80);
        TestAssert.SetField(bs, "clickFixProgress", 99);
        TestAssert.InvokeMethod(bs, "ApplyPolishing", 0.40f, 0.5f);
        TestAssert.AreEqual(0f, TestAssert.GetField<float>(bs, "finishProgress"), "Finish progress must stay 0 when clickFixProgress is below 100%");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Boundary_AudioVolumeClamping_StaysStrictlyWithinZeroAndOne()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();

        for (int i = 0; i < 10; i++)
        {
            TestAssert.InvokeMethod(bs, "ToggleMute");
            TestAssert.IsInRange(AudioListener.volume, 0f, 1f, "AudioListener volume must always be clamped to [0, 1]");
        }

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Boundary_ZoomClamping_StaysStrictlyWithinOneAndTwo()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SetViewState", "WORKBENCH");

        TestAssert.InvokeMethod(bs, "StepBenchZoom", -50.0f);
        float zoomMin = TestAssert.GetField<float>(bs, "targetBenchZoom");
        TestAssert.AreEqual(1.0f, zoomMin, "Negative zoom scroll delta must clamp targetBenchZoom to 1.0f");

        TestAssert.InvokeMethod(bs, "StepBenchZoom", 50.0f);
        float zoomMax = TestAssert.GetField<float>(bs, "targetBenchZoom");
        TestAssert.AreEqual(2.0f, zoomMax, "Positive zoom scroll delta must clamp targetBenchZoom to 2.0f");

        TestAssert.InvokeMethod(bs, "StepBenchZoom", -0.5f);
        float zoomMid = TestAssert.GetField<float>(bs, "targetBenchZoom");
        TestAssert.AreEqual(1.5f, zoomMid, "Zoom delta of -0.5 from 2.0 must adjust targetBenchZoom to 1.5f");

        TestAssert.DestroyBootstrapper(bs);
    }
}
