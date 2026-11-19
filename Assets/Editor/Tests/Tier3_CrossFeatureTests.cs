using System;
using UnityEngine;
using UnityEngine.UI;

public class Tier3_CrossFeatureTests
{
    public void Test_CrossFeature_DayNightTransitionDuringActiveMinigame_PreservesItemProgress()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");

        TestAssert.SetField(bs, "cleanProgress", 65);
        TestAssert.SetField(bs, "grindProgress", 70);
        TestAssert.SetField(bs, "clickFixProgress", 80);

        TestAssert.SetField(bs, "completedOrdersCount", 2);
        TestAssert.InvokeMethod(bs, "UpdateRoomIndicators");
        TestAssert.InvokeMethod(bs, "UpdateTopBar");

        Text topTitle = TestAssert.GetField<Text>(bs, "topTitleText");
        TestAssert.IsTrue(topTitle.text.Contains("Закат"), "Day/night phase transitioned to Sunset");

        TestAssert.AreEqual(65, TestAssert.GetField<int>(bs, "cleanProgress"), "Clean progress preserved across phase shift");
        TestAssert.AreEqual(70, TestAssert.GetField<int>(bs, "grindProgress"), "Grind progress preserved across phase shift");
        TestAssert.AreEqual(80, TestAssert.GetField<int>(bs, "clickFixProgress"), "Click fix progress preserved across phase shift");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_CrossFeature_AudioMuteToggledDuringActiveLoop_SilencesWithoutStoppingLogic()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "GRIND");

        AudioSource toolSource = TestAssert.GetField<AudioSource>(bs, "toolLoopSource");
        toolSource.clip = TestAssert.GetField<AudioClip>(bs, "clipGrindLoop");
        toolSource.Play();
        TestAssert.IsTrue(toolSource.isPlaying, "Tool loop audio source should be playing");

        TestAssert.InvokeMethod(bs, "ToggleMute");
        TestAssert.AreEqual(0f, AudioListener.volume, "Volume must be silenced upon mute toggle");
        TestAssert.IsTrue(toolSource.isPlaying, "AudioSource still active in logic despite silence");

        TestAssert.SetField(bs, "cleanProgress", 50);
        float[,] burrMap = TestAssert.GetField<float[,]>(bs, "burrMap");
        burrMap[160, 160] = 0.8f;
        TestAssert.InvokeMethod(bs, "ApplyGrinding", Vector2.zero, 0.50f, 0.1f);
        TestAssert.IsTrue(burrMap[160, 160] < 0.8f, "Grinding logic continues updating state while muted via ApplyGrinding");

        TestAssert.InvokeMethod(bs, "ToggleMute");
        TestAssert.AreEqual(1f, AudioListener.volume, "Volume must be restored upon unmuting to avoid test isolation leakage");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_CrossFeature_CatPurrRelaxationBuff_ReducesAccumulatedOverScrubDamage()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.SetField(bs, "overScrubDamage", 18f);

        GameObject cat = GameObject.Find("Station_Cat");
        Button catBtn = cat.GetComponent<Button>();

        catBtn.onClick.Invoke();
        catBtn.onClick.Invoke();
        catBtn.onClick.Invoke();

        float remainingDamage = TestAssert.GetField<float>(bs, "overScrubDamage");
        TestAssert.AreEqual(9f, remainingDamage, "Three cat petting actions must reduce over-scrub damage from 18% to 9%");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_CrossFeature_ToolSwitching_ImmediatelyStopsPreviousLoopAudioSource()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "CLEAN");

        AudioSource toolSource = TestAssert.GetField<AudioSource>(bs, "toolLoopSource");
        toolSource.clip = TestAssert.GetField<AudioClip>(bs, "clipCleanLoop");
        toolSource.Play();
        TestAssert.IsTrue(toolSource.isPlaying, "Clean loop audio is active");

        TestAssert.InvokeMethod(bs, "SelectBenchTool", "CLICK_FIX");

        TestAssert.IsFalse(toolSource.isPlaying, "Tool loop audio source stopped immediately upon tool change");
        GameObject clickNodes = TestAssert.GetField<GameObject>(bs, "clickNodesContainer");
        TestAssert.IsTrue(clickNodes.activeSelf, "Click nodes container activated for ClickFix mode");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_CrossFeature_OrderSelection_DynamicallyUpdatesWorkbenchPreviewAndDiagnostics()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 1);

        object activeOrder = TestAssert.GetField<object>(bs, "activeOrder");
        TestAssert.IsNotNull(activeOrder, "Active order must not be null");

        string shapeType = (string)activeOrder.GetType().GetField("shapeType").GetValue(activeOrder);
        TestAssert.AreEqual("DIAMOND", shapeType, "Selected order shape type must be DIAMOND");

        System.Collections.IList diags = (System.Collections.IList)activeOrder.GetType().GetField("diagnostics").GetValue(activeOrder);
        TestAssert.AreEqual(3, diags.Count, "Diamond order must have 3 diagnostic zones");

        string diagText0 = (string)diags[0].GetType().GetField("text").GetValue(diags[0]);
        TestAssert.IsTrue(diagText0.Contains("Ювелирным ультра-гелем"), "Diamond diagnostic must specify ultra-gel");

        TestAssert.InvokeMethod(bs, "GenerateItemTexture", "DIAMOND");
        Texture2D itemTex = TestAssert.GetField<Texture2D>(bs, "itemTex");
        TestAssert.IsNotNull(itemTex, "Synthesized item texture must not be null");
        TestAssert.AreEqual(320, itemTex.width, "Texture width must equal 320");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_CrossFeature_EquipmentUpgrades_ModifiesToolCursorDiameterAndStability()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "CLEAN");

        TestAssert.IsFalse(TestAssert.GetField<bool>(bs, "upgPrecisionBits"), "Precision bits initially false");
        TestAssert.IsFalse(TestAssert.GetField<bool>(bs, "upgStabilizer"), "Stabilizer initially false");

        TestAssert.InvokeMethod(bs, "ApplyScrubbing", Vector2.zero, 0.80f, 0.5f);
        float baseDamage = TestAssert.GetField<float>(bs, "overScrubDamage");
        TestAssert.IsTrue(baseDamage > 0f, "Scrubbing at speedRatio 0.80 must inflict damage without stabilizer");

        TestAssert.SetField(bs, "overScrubDamage", 0f);
        TestAssert.SetField(bs, "money", 5000);
        TestAssert.InvokeMethod(bs, "BuyUpgrade", 0);
        TestAssert.InvokeMethod(bs, "BuyUpgrade", 1);

        TestAssert.IsTrue(TestAssert.GetField<bool>(bs, "upgPrecisionBits"), "Precision bits purchased");
        TestAssert.IsTrue(TestAssert.GetField<bool>(bs, "upgStabilizer"), "Stabilizer purchased");

        TestAssert.InvokeMethod(bs, "ApplyScrubbing", Vector2.zero, 0.80f, 0.5f);
        float upgradedDamage = TestAssert.GetField<float>(bs, "overScrubDamage");
        TestAssert.AreEqual(0f, upgradedDamage, "Scrubbing at speedRatio 0.80 must inflict zero damage with stabilizer threshold 0.86");

        TestAssert.InvokeMethod(bs, "ApplyScrubbing", Vector2.zero, 0.95f, 0.5f);
        float damageWithStabilizer = TestAssert.GetField<float>(bs, "overScrubDamage");
        TestAssert.AreEqual(1.0f, damageWithStabilizer, "Stabilizer reduces damage accumulation rate by 60 percent");

        byte[,] dirtMap = TestAssert.GetField<byte[,]>(bs, "dirtMap");
        dirtMap[160, 179] = 1;
        TestAssert.InvokeMethod(bs, "ApplyScrubbing", Vector2.zero, 0.20f, 0.1f);
        TestAssert.AreEqual((byte)2, dirtMap[160, 179], "Pixel at radius 19 from center cleaned due to expanded precision bits brush radius");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_TestInfrastructure_BootstrapperTeardown_LeavesZeroRootGameObjectsAndDefaultAudioState()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "ToggleMute");
        TestAssert.AreEqual(0f, AudioListener.volume, "AudioListener volume muted");

        TestAssert.DestroyBootstrapper(bs);

        TestAssert.AreEqual(1f, AudioListener.volume, "AudioListener volume must be restored to 1.0f");
        TestAssert.IsFalse(AudioListener.pause, "AudioListener pause must be restored to false");
        TestAssert.AreEqual(1f, Time.timeScale, "Time timescale must be restored to 1.0f");

        GameObject[] remainingRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        TestAssert.AreEqual(0, remainingRoots.Length, "Teardown must leave zero leftover root GameObjects in scene");
    }
}
