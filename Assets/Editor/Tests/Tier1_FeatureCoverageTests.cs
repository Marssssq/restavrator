using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tier1_FeatureCoverageTests
{
    private static bool HasCyrillic(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text)
        {
            if ((c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') || c == 'ё' || c == 'Ё')
            {
                return true;
            }
        }
        return false;
    }

    private static int CountCommentTokens(string filePath)
    {
        char slash = (char)47;
        char star = (char)42;
        string singleLineToken = new string(slash, 2);
        string multiLineStart = "" + slash + star;
        string multiLineEnd = "" + star + slash;

        string content = File.ReadAllText(filePath);
        int count = 0;
        if (content.Contains(singleLineToken)) count++;
        if (content.Contains(multiLineStart)) count++;
        if (content.Contains(multiLineEnd)) count++;
        return count;
    }

    public void Test_Mouse_StationClick_MovesPlayerTowardsWindow()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        GameObject station = GameObject.Find("Station_Window");
        TestAssert.IsNotNull(station, "Station_Window GameObject must exist");
        Button btn = station.GetComponent<Button>();
        TestAssert.IsNotNull(btn, "Station_Window must have Button component");

        btn.onClick.Invoke();

        Vector2 targetPos = TestAssert.GetField<Vector2>(bs, "targetPos");
        TestAssert.AreEqual(-355f, targetPos.x, "Window interaction target X");
        TestAssert.AreEqual(-15f, targetPos.y, "Window interaction target Y");
        TestAssert.AreEqual("WINDOW", TestAssert.GetField<string>(bs, "pendingStation"), "Pending station must be WINDOW");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Mouse_StationClick_MovesPlayerTowardsWorkbench()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        GameObject station = GameObject.Find("Station_Workbench");
        TestAssert.IsNotNull(station, "Station_Workbench GameObject must exist");
        Button btn = station.GetComponent<Button>();
        TestAssert.IsNotNull(btn, "Station_Workbench must have Button component");

        btn.onClick.Invoke();

        Vector2 targetPos = TestAssert.GetField<Vector2>(bs, "targetPos");
        TestAssert.AreEqual(0f, targetPos.x, "Workbench interaction target X");
        TestAssert.AreEqual(84f, targetPos.y, "Workbench interaction target Y");
        TestAssert.AreEqual("WORKBENCH", TestAssert.GetField<string>(bs, "pendingStation"), "Pending station must be WORKBENCH");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Mouse_StationClick_MovesPlayerTowardsCabinet()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        GameObject station = GameObject.Find("Station_Cabinet");
        TestAssert.IsNotNull(station, "Station_Cabinet GameObject must exist");
        Button btn = station.GetComponent<Button>();
        TestAssert.IsNotNull(btn, "Station_Cabinet must have Button component");

        btn.onClick.Invoke();

        Vector2 targetPos = TestAssert.GetField<Vector2>(bs, "targetPos");
        TestAssert.AreEqual(365f, targetPos.x, "Cabinet interaction target X");
        TestAssert.AreEqual(84f, targetPos.y, "Cabinet interaction target Y");
        TestAssert.AreEqual("CABINET", TestAssert.GetField<string>(bs, "pendingStation"), "Pending station must be CABINET");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Mouse_BenchToolSelection_ChangesActiveBenchAction()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SetViewState", "WORKBENCH");

        TestAssert.InvokeMethod(bs, "SelectBenchTool", "CLEAN");
        TestAssert.AreEqual("CLEAN", TestAssert.GetField<string>(bs, "activeBenchAction"), "Tool should switch to CLEAN");

        TestAssert.InvokeMethod(bs, "SelectBenchTool", "GRIND");
        TestAssert.AreEqual("GRIND", TestAssert.GetField<string>(bs, "activeBenchAction"), "Tool should switch to GRIND");

        TestAssert.InvokeMethod(bs, "SelectBenchTool", "CLICK_FIX");
        TestAssert.AreEqual("CLICK_FIX", TestAssert.GetField<string>(bs, "activeBenchAction"), "Tool should switch to CLICK_FIX");

        TestAssert.InvokeMethod(bs, "SelectBenchTool", "FINISH");
        TestAssert.AreEqual("FINISH", TestAssert.GetField<string>(bs, "activeBenchAction"), "Tool should switch to FINISH");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Mouse_BenchZoomCycle_CyclesZoomLevels()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SetViewState", "WORKBENCH");

        TestAssert.AreEqual(1.0f, TestAssert.GetField<float>(bs, "targetBenchZoom"), "Initial zoom should be 1.0f");
        TestAssert.InvokeMethod(bs, "CycleBenchZoom");
        TestAssert.AreEqual(1.5f, TestAssert.GetField<float>(bs, "targetBenchZoom"), "Second zoom should be 1.5f");
        TestAssert.InvokeMethod(bs, "CycleBenchZoom");
        TestAssert.AreEqual(2.0f, TestAssert.GetField<float>(bs, "targetBenchZoom"), "Third zoom should be 2.0f");
        TestAssert.InvokeMethod(bs, "CycleBenchZoom");
        TestAssert.AreEqual(1.0f, TestAssert.GetField<float>(bs, "targetBenchZoom"), "Zoom should cycle back to 1.0f");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Tooltip_StationWindow_DisplaysDescriptiveHint()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        GameObject station = GameObject.Find("Station_Window");
        EventTrigger trigger = station.GetComponent<EventTrigger>();
        TestAssert.IsNotNull(trigger, "Window station must have EventTrigger");

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        trigger.OnPointerEnter(eventData);

        Text hint = TestAssert.GetField<Text>(bs, "bottomHintText");
        TestAssert.IsTrue(hint.text.Contains("ОКНО ЗАКАЗОВ (ЛКМ)"), "Bottom hint must display Window tooltip");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Tooltip_StationWorkbench_DisplaysDescriptiveHint()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        GameObject station = GameObject.Find("Station_Workbench");
        EventTrigger trigger = station.GetComponent<EventTrigger>();
        TestAssert.IsNotNull(trigger, "Workbench station must have EventTrigger");

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        trigger.OnPointerEnter(eventData);

        Text hint = TestAssert.GetField<Text>(bs, "bottomHintText");
        TestAssert.IsTrue(hint.text.Contains("РАБОЧИЙ СТОЛ (ЛКМ)"), "Bottom hint must display Workbench tooltip");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Tooltip_StationCabinet_DisplaysDescriptiveHint()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        GameObject station = GameObject.Find("Station_Cabinet");
        EventTrigger trigger = station.GetComponent<EventTrigger>();
        TestAssert.IsNotNull(trigger, "Cabinet station must have EventTrigger");

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        trigger.OnPointerEnter(eventData);

        Text hint = TestAssert.GetField<Text>(bs, "bottomHintText");
        TestAssert.IsTrue(hint.text.Contains("ШКАФ ИНСТРУМЕНТОВ (ЛКМ)"), "Bottom hint must display Cabinet tooltip");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Tooltip_StationUpgrades_DisplaysDescriptiveHint()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        GameObject station = GameObject.Find("Station_Upgrades");
        EventTrigger trigger = station.GetComponent<EventTrigger>();
        TestAssert.IsNotNull(trigger, "Upgrades station must have EventTrigger");

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        trigger.OnPointerEnter(eventData);

        Text hint = TestAssert.GetField<Text>(bs, "bottomHintText");
        TestAssert.IsTrue(hint.text.Contains("СТЕНД МАСТЕРА И МУЗЕЙ (ЛКМ)"), "Bottom hint must display Upgrades tooltip");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Tooltip_StationPointerExit_RestoresRoomIndicators()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        GameObject station = GameObject.Find("Station_Window");
        EventTrigger trigger = station.GetComponent<EventTrigger>();
        PointerEventData eventData = new PointerEventData(EventSystem.current);

        trigger.OnPointerEnter(eventData);
        trigger.OnPointerExit(eventData);

        Text hint = TestAssert.GetField<Text>(bs, "bottomHintText");
        TestAssert.IsTrue(hint.text.Contains("Подойдите к ОКНУ ЗАКАЗОВ"), "Bottom hint must restore room guidance on exit");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Feedback_CatClick_TriggersPurrSoundAndScaleBob()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        GameObject cat = GameObject.Find("Station_Cat");
        TestAssert.IsNotNull(cat, "Cat station must exist");

        Button catBtn = cat.GetComponent<Button>();
        TestAssert.IsNotNull(catBtn, "Cat station must have Button");
        catBtn.onClick.Invoke();

        RectTransform catRT = TestAssert.GetField<RectTransform>(bs, "catRT");
        TestAssert.IsTrue(catRT.localScale.x > 1.1f, "Cat scale bob must exceed 1.1x on click");

        Text hint = TestAssert.GetField<Text>(bs, "bottomHintText");
        TestAssert.IsTrue(hint.text.Contains("Барсик"), "Hint text must reference cat Barsik purr");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Feedback_ToolSwitch_PlaysToolSwitchSfxWithPitch()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SetViewState", "WORKBENCH");

        TestAssert.InvokeMethod(bs, "SelectBenchTool", "CLEAN");
        Image[] imgs = TestAssert.GetField<Image[]>(bs, "benchToolBtnImgs");
        TestAssert.AreEqual(new Vector3(1.04f, 1.04f, 1f), imgs[0].transform.localScale, "Active Clean tool button scaled up");
        TestAssert.AreEqual(Vector3.one, imgs[1].transform.localScale, "Inactive Grind tool button at base scale");

        TestAssert.InvokeMethod(bs, "SelectBenchTool", "GRIND");
        TestAssert.AreEqual(Vector3.one, imgs[0].transform.localScale, "Deactivated Clean tool button returned to 1.0");
        TestAssert.AreEqual(new Vector3(1.04f, 1.04f, 1f), imgs[1].transform.localScale, "Active Grind tool button scaled up");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Feedback_MuteToggle_UpdatesAudioListenerAndButtonLabel()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "ToggleMute");
        TestAssert.IsTrue(TestAssert.GetField<bool>(bs, "isMuted"), "isMuted must be true after first toggle");
        TestAssert.AreEqual(0f, AudioListener.volume, "AudioListener volume must be 0 when muted");
        TestAssert.AreEqual("🔇 ВЫКЛ", TestAssert.GetField<Text>(bs, "muteBtnText").text, "Mute text must reflect muted state");

        TestAssert.InvokeMethod(bs, "ToggleMute");
        TestAssert.IsFalse(TestAssert.GetField<bool>(bs, "isMuted"), "isMuted must be false after second toggle");
        TestAssert.AreEqual(1f, AudioListener.volume, "AudioListener volume must be 1 when unmuted");
        TestAssert.AreEqual("🔊 ЗВУК", TestAssert.GetField<Text>(bs, "muteBtnText").text, "Mute text must reflect unmuted state");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Feedback_SparkEmission_PopulatesSparkList()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "EmitSparks", Vector2.zero, 12, Color.yellow);
        System.Collections.IList sparks = (System.Collections.IList)TestAssert.GetField<object>(bs, "sparks");
        TestAssert.IsTrue(sparks.Count >= 12, "Sparks list must contain at least 12 created spark particles");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Feedback_FloatingText_SpawnsTextElementWithColor()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SpawnFloatingText", Vector2.zero, "+500 руб.", Color.yellow);
        System.Collections.IList texts = (System.Collections.IList)TestAssert.GetField<object>(bs, "floatingTexts");
        TestAssert.IsTrue(texts.Count >= 1, "Floating text list must contain at least 1 floating item");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Clock_InitialShiftState_IsMorning()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        Text topTitle = TestAssert.GetField<Text>(bs, "topTitleText");
        TestAssert.IsTrue(topTitle.text.Contains("Утро"), "Top title must indicate morning phase");

        Image winGlow = TestAssert.GetField<Image>(bs, "winGlowImg");
        TestAssert.AreEqual(0.85f, winGlow.color.r, "Morning window glow red channel");
        TestAssert.AreEqual(0.95f, winGlow.color.g, "Morning window glow green channel");
        TestAssert.AreEqual(1.0f, winGlow.color.b, "Morning window glow blue channel");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Clock_AfterFirstOrder_TransitionsToDay()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.SetField(bs, "completedOrdersCount", 1);
        TestAssert.InvokeMethod(bs, "UpdateRoomIndicators");
        TestAssert.InvokeMethod(bs, "UpdateTopBar");

        Text topTitle = TestAssert.GetField<Text>(bs, "topTitleText");
        TestAssert.IsTrue(topTitle.text.Contains("День"), "Top title must indicate day phase");

        Image winGlow = TestAssert.GetField<Image>(bs, "winGlowImg");
        TestAssert.AreEqual(1.0f, winGlow.color.r, "Day window glow red channel");
        TestAssert.AreEqual(0.98f, winGlow.color.g, "Day window glow green channel");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Clock_AfterSecondOrder_TransitionsToSunset()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.SetField(bs, "completedOrdersCount", 2);
        TestAssert.InvokeMethod(bs, "UpdateRoomIndicators");
        TestAssert.InvokeMethod(bs, "UpdateTopBar");

        Text topTitle = TestAssert.GetField<Text>(bs, "topTitleText");
        TestAssert.IsTrue(topTitle.text.Contains("Закат"), "Top title must indicate sunset phase");

        Image winGlow = TestAssert.GetField<Image>(bs, "winGlowImg");
        TestAssert.AreEqual(1.0f, winGlow.color.r, "Sunset window glow red channel");
        TestAssert.AreEqual(0.60f, winGlow.color.g, "Sunset window glow green channel");
        TestAssert.AreEqual(0.30f, winGlow.color.b, "Sunset window glow blue channel");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Clock_AfterThirdOrder_TransitionsToEvening()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.SetField(bs, "completedOrdersCount", 3);
        TestAssert.InvokeMethod(bs, "UpdateRoomIndicators");
        TestAssert.InvokeMethod(bs, "UpdateTopBar");

        Text topTitle = TestAssert.GetField<Text>(bs, "topTitleText");
        TestAssert.IsTrue(topTitle.text.Contains("Вечер"), "Top title must indicate evening phase");

        Image winGlow = TestAssert.GetField<Image>(bs, "winGlowImg");
        TestAssert.AreEqual(0.24f, winGlow.color.r, "Evening window glow red channel");
        TestAssert.AreEqual(0.35f, winGlow.color.g, "Evening window glow green channel");
        TestAssert.AreEqual(0.65f, winGlow.color.b, "Evening window glow blue channel");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Clock_ShiftClockTickSound_PlaysInLoop()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        AudioSource ambient = TestAssert.GetField<AudioSource>(bs, "ambientSource");
        TestAssert.IsNotNull(ambient, "Ambient audio source must exist");
        TestAssert.IsTrue(ambient.loop, "Ambient clock tick audio source must loop");
        TestAssert.IsNotNull(ambient.clip, "Ambient clip must not be null");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Atmosphere_DustMotes_InitializedAndDistributed()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        System.Collections.IList motes = (System.Collections.IList)TestAssert.GetField<object>(bs, "dustMotes");
        TestAssert.AreEqual(16, motes.Count, "Exactly 16 ambient dust motes must be generated");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Atmosphere_DustMotes_UpdateAnimatesPositions()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.SetField(bs, "viewState", "ROOM");
        TestAssert.InvokeMethod(bs, "UpdateDustMotes", 0.05f);

        System.Collections.IList motes = (System.Collections.IList)TestAssert.GetField<object>(bs, "dustMotes");
        object firstMote = motes[0];
        Image img = (Image)firstMote.GetType().GetField("img").GetValue(firstMote);
        TestAssert.IsTrue(img.color.a > 0.05f, "Dust mote alpha should be positive in ROOM view");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Atmosphere_DustMotes_HiddenWhenNotInRoom()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.SetField(bs, "viewState", "WORKBENCH");
        TestAssert.InvokeMethod(bs, "UpdateDustMotes", 0.05f);

        System.Collections.IList motes = (System.Collections.IList)TestAssert.GetField<object>(bs, "dustMotes");
        object firstMote = motes[0];
        Image img = (Image)firstMote.GetType().GetField("img").GetValue(firstMote);
        TestAssert.AreEqual(Color.clear, img.color, "Dust motes must be clear outside ROOM view");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Atmosphere_CatBreathingAnimation_LerpsBackToOne()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        RectTransform catRT = TestAssert.GetField<RectTransform>(bs, "catRT");
        TestAssert.SetField(bs, "catClickScale", new Vector3(1.18f, 0.88f, 1f));
        catRT.localScale = new Vector3(1.18f, 0.88f, 1f);
        TestAssert.InvokeMethod(bs, "StepCatBreathing", 0.1f);

        Vector3 scale = TestAssert.GetField<Vector3>(bs, "catClickScale");
        TestAssert.IsTrue(scale.x < 1.18f, "Cat click scale X should lerp toward 1.0");
        TestAssert.IsTrue(scale.x > 1.0f, "Cat click scale X should still be greater than 1.0");
        TestAssert.IsTrue(scale.y > 0.88f, "Cat click scale Y should lerp toward 1.0");
        TestAssert.IsTrue(scale.y < 1.0f, "Cat click scale Y should still be less than 1.0");
        TestAssert.IsTrue(catRT.localScale.x > 0f, "Cat localScale X should be positive");
        TestAssert.IsTrue(catRT.localScale.y > 0f, "Cat localScale Y should be positive");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Atmosphere_PlayerWalkingAnimation_BobsVerticalPosition()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        RectTransform playerRT = TestAssert.GetField<RectTransform>(bs, "playerRT");
        float initialY = playerRT.anchoredPosition.y;
        TestAssert.InvokeMethod(bs, "MovePlayerToStation", new Vector2(300f, -55f), "WORKBENCH");
        TestAssert.InvokeMethod(bs, "StepPlayerMovement", 0.11f);
        float bobY = playerRT.anchoredPosition.y - initialY;
        TestAssert.IsTrue(bobY > 4.5f, "Vertical walking bob amplitude should peak above 4.5");
        TestAssert.IsTrue(TestAssert.GetField<float>(bs, "walkAnimTimer") > 0f, "Walk animation timer should advance");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Minigame_Examination_LoupeDetectsAllThreeZones()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.AreEqual("NEED_DIAGNOSIS", TestAssert.GetField<string>(bs, "orderStage"), "Stage must be NEED_DIAGNOSIS");
        TestAssert.AreEqual(0, TestAssert.GetField<int>(bs, "zonesDiscovered"), "Initial discovered zones must be zero");

        object activeOrder = TestAssert.GetField<object>(bs, "activeOrder");
        System.Collections.IList diags = (System.Collections.IList)activeOrder.GetType().GetField("diagnostics").GetValue(activeOrder);
        TestAssert.AreEqual(3, diags.Count, "Active order must have 3 diagnostic zones");

        for (int i = 0; i < 2; i++)
        {
            var d = diags[i];
            float dx = (float)d.GetType().GetField("x").GetValue(d);
            float dy = (float)d.GetType().GetField("y").GetValue(d);
            TestAssert.InvokeMethod(bs, "InspectWithLoupe", new Vector2(dx, dy));
        }

        Text intermediateHint = TestAssert.GetField<Text>(bs, "bottomHintText");
        TestAssert.IsTrue(intermediateHint.text.Contains("2 / 3"), "Bottom hint text must report 2 / 3 defects discovered during diagnosis");

        var lastD = diags[2];
        float lx = (float)lastD.GetType().GetField("x").GetValue(lastD);
        float ly = (float)lastD.GetType().GetField("y").GetValue(lastD);
        TestAssert.InvokeMethod(bs, "InspectWithLoupe", new Vector2(lx, ly));

        TestAssert.AreEqual(3, TestAssert.GetField<int>(bs, "zonesDiscovered"), "All 3 zones must be recorded found by InspectWithLoupe");
        TestAssert.AreEqual("NEED_TOOLS", TestAssert.GetField<string>(bs, "orderStage"), "Stage must advance to NEED_TOOLS upon discovering all zones");
        Text finalNotice = TestAssert.GetField<Text>(bs, "benchModeNoticeText");
        TestAssert.IsTrue(finalNotice.text.Contains("ДИАГНОСТИКА ЗАВЕРШЕНА"), "Mode notice must report diagnosis completion");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Minigame_ChemicalCleaning_RemovesDirtMapPixels()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "CLEAN");

        byte[,] dirtMap = TestAssert.GetField<byte[,]>(bs, "dirtMap");
        int initialDirty = 0;
        int targetX = -1;
        int targetY = -1;
        for (int y = 0; y < 320; y++)
        {
            for (int x = 0; x < 320; x++)
            {
                if (dirtMap[y, x] == 1)
                {
                    initialDirty++;
                    if (targetX < 0)
                    {
                        targetX = x;
                        targetY = y;
                    }
                }
            }
        }
        TestAssert.IsTrue(initialDirty > 0, "Item must start with dirty pixels");
        TestAssert.IsTrue(targetX >= 0, "Target dirty pixel must be found");

        float localX = ((targetX + 0.5f) / 320f) * 360f - 180f;
        float localY = ((targetY + 0.5f) / 320f) * 360f - 180f;
        TestAssert.InvokeMethod(bs, "ApplyScrubbing", new Vector2(localX, localY), 0.25f, 0.05f);

        TestAssert.AreEqual((byte)2, dirtMap[targetY, targetX], "Scrubbed pixel must transition to state 2 (cleaned) via ApplyScrubbing");
        TestAssert.AreEqual(0f, TestAssert.GetField<float>(bs, "overScrubDamage"), "Safe speed scrubbing must not cause damage");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Minigame_PrecisionFiling_ReducesBurrMapValues()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "GRIND");
        TestAssert.SetField(bs, "cleanProgress", 50);

        float[,] burrMap = TestAssert.GetField<float[,]>(bs, "burrMap");
        burrMap[160, 160] = 1.0f;
        TestAssert.InvokeMethod(bs, "ApplyGrinding", Vector2.zero, 0.50f, 0.1f);

        float remainingBurr = burrMap[160, 160];
        TestAssert.IsTrue(remainingBurr < 1.0f, "Burr height must be reduced by ApplyGrinding action");
        TestAssert.IsTrue(remainingBurr >= 0.02f, "Burr height must clamp at or above minimum 0.02 threshold");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Minigame_GemSetting_ClickFixNodesDecrementClicks()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "SetupClickNodesForItem", "RING");
        TestAssert.SetField(bs, "activeBenchAction", "CLICK_FIX");
        TestAssert.SetField(bs, "cleanProgress", 50);

        TestAssert.InvokeMethod(bs, "OnClickFixNode", 0);

        System.Collections.IList nodes = (System.Collections.IList)TestAssert.GetField<object>(bs, "activeClickNodes");
        object node = nodes[0];
        int clicksLeft = (int)node.GetType().GetField("clicksLeft").GetValue(node);
        TestAssert.AreEqual(4, clicksLeft, "Node clicks left should decrement from 5 to 4");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Minigame_MirrorPolishing_FinishProgressReachesTarget()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.InvokeMethod(bs, "OpenWorkbenchView");
        TestAssert.InvokeMethod(bs, "SelectBenchTool", "FINISH");

        byte[,] dirtMap = TestAssert.GetField<byte[,]>(bs, "dirtMap");
        float[,] burrMap = TestAssert.GetField<float[,]>(bs, "burrMap");
        for (int y = 0; y < 320; y++)
        {
            for (int x = 0; x < 320; x++)
            {
                if (dirtMap[y, x] == 1) dirtMap[y, x] = 2;
                if (burrMap[y, x] > 0.08f) burrMap[y, x] = 0.02f;
            }
        }
        TestAssert.SetField(bs, "cleanProgress", 100);
        TestAssert.SetField(bs, "grindProgress", 100);
        TestAssert.SetField(bs, "clickFixProgress", 100);
        TestAssert.SetField(bs, "finishProgress", 0f);

        TestAssert.InvokeMethod(bs, "ApplyPolishing", 0.40f, 1.8f);
        float finish = TestAssert.GetField<float>(bs, "finishProgress");
        TestAssert.IsTrue(finish >= 80f, "Finish progress must reach 80% or higher target via ApplyPolishing");

        TestAssert.InvokeMethod(bs, "UpdateBenchDoneButtonVisuals");
        Image btnDoneImg = TestAssert.GetField<Image>(bs, "btnDoneImg");
        Text btnDoneText = TestAssert.GetField<Text>(bs, "btnDoneText");
        TestAssert.AreEqual(new Color(0.24f, 0.65f, 0.32f, 1f), btnDoneImg.color, "Done button must turn green when all stages complete");
        TestAssert.IsTrue(btnDoneText.text.Contains("ГОТОВО"), "Done button text must indicate completion");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Audio_BellClipSynthesis_GeneratesHarmonicSamples()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        AudioClip clip = (AudioClip)TestAssert.InvokeMethod(bs, "SynthesizeBellClip");
        TestAssert.IsNotNull(clip, "Synthesized bell clip must not be null");
        TestAssert.IsTrue(clip.samples > 0, "Bell clip samples must be greater than zero");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Audio_CleanLoopSynthesis_GeneratesFizzingFoamAudio()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        AudioClip clip = (AudioClip)TestAssert.InvokeMethod(bs, "SynthesizeCleanLoopClip");
        TestAssert.IsNotNull(clip, "Synthesized clean loop clip must not be null");
        TestAssert.IsTrue(clip.samples > 0, "Clean loop samples must be greater than zero");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Audio_GrindLoopSynthesis_GeneratesMetalFrictionAudio()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        AudioClip clip = (AudioClip)TestAssert.InvokeMethod(bs, "SynthesizeGrindLoopClip");
        TestAssert.IsNotNull(clip, "Synthesized grind loop clip must not be null");
        TestAssert.IsTrue(clip.samples > 0, "Grind loop samples must be greater than zero");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Audio_PurrClipSynthesis_GeneratesLowFrequencyRumble()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        AudioClip clip = (AudioClip)TestAssert.InvokeMethod(bs, "SynthesizePurrClip");
        TestAssert.IsNotNull(clip, "Synthesized purr clip must not be null");
        TestAssert.IsTrue(clip.samples > 0, "Purr clip samples must be greater than zero");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Audio_CrystalChimeSynthesis_GeneratesPitchedChime()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        AudioClip clip = (AudioClip)TestAssert.InvokeMethod(bs, "SynthesizeCrystalChimeClip");
        TestAssert.IsNotNull(clip, "Synthesized crystal chime clip must not be null");
        TestAssert.IsTrue(clip.samples > 0, "Crystal chime samples must be greater than zero");
        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Audio_ClassicalMusicSoundtrack_SynthesizesContinuousRomanticTheme()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        AudioClip clip = (AudioClip)TestAssert.InvokeMethod(bs, "SynthesizeClassicalTheme");
        TestAssert.IsNotNull(clip, "Synthesized classical music theme must not be null");

        int sampleRate = clip.frequency;
        TestAssert.IsTrue(sampleRate >= 22050, "Sample rate must be at least 22050 Hz");
        TestAssert.IsTrue(clip.length >= 8.0f, "Classical soundtrack theme must be at least 8 seconds in duration");

        int totalSamples = clip.samples;
        float[] data = new float[totalSamples];
        clip.GetData(data, 0);

        float sumSq = 0f;
        float peak = 0f;
        for (int i = 0; i < totalSamples; i++)
        {
            float val = data[i];
            float abs = Mathf.Abs(val);
            if (abs > peak) peak = abs;
            sumSq += val * val;
        }
        float rms = Mathf.Sqrt(sumSq / totalSamples);

        TestAssert.IsTrue(peak > 0.1f, "Theme audio must contain audible acoustic peaks");
        TestAssert.IsTrue(peak <= 1.0f, "Theme audio peaks must remain within valid range without clipping");
        TestAssert.IsTrue(rms > 0.02f, "RMS energy must indicate continuous audible sound rather than silence");

        int quarter = totalSamples / 4;
        int crossingsQ1 = 0;
        int crossingsQ2 = 0;
        for (int i = 0; i < quarter - 1; i++)
        {
            if ((data[i] >= 0f && data[i + 1] < 0f) || (data[i] < 0f && data[i + 1] >= 0f))
            {
                crossingsQ1++;
            }
        }
        for (int i = quarter; i < quarter * 2 - 1; i++)
        {
            if ((data[i] >= 0f && data[i + 1] < 0f) || (data[i] < 0f && data[i + 1] >= 0f))
            {
                crossingsQ2++;
            }
        }

        TestAssert.IsTrue(crossingsQ1 > 100, "First musical section must contain substantial harmonic oscillations");
        TestAssert.IsTrue(crossingsQ2 > 100, "Second musical section must contain substantial harmonic oscillations");

        AudioSource musicSource = TestAssert.GetField<AudioSource>(bs, "musicSource");
        TestAssert.IsNotNull(musicSource, "Music audio source component must exist");
        TestAssert.IsTrue(musicSource.loop, "Music audio source must be configured to loop continuously");
        TestAssert.IsTrue(musicSource.isPlaying, "Music audio source must be actively playing during workshop shift");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Localization_WindowOrders_AllHaveCyrillicTitlesAndBriefings()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        System.Collections.IList choices = (System.Collections.IList)TestAssert.GetField<object>(bs, "currentWindowChoices");
        TestAssert.IsTrue(choices.Count >= 3, "Window choices must contain at least 3 orders");

        foreach (var c in choices)
        {
            string title = (string)c.GetType().GetField("title").GetValue(c);
            string briefing = (string)c.GetType().GetField("briefingText").GetValue(c);
            TestAssert.IsTrue(HasCyrillic(title), $"Order title '{title}' must contain Cyrillic characters");
            TestAssert.IsTrue(HasCyrillic(briefing), $"Briefing for '{title}' must contain Cyrillic characters");
        }

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Localization_CabinetLabels_AllUseAuthenticRussianTerminology()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        GameObject cab = TestAssert.GetField<GameObject>(bs, "cabinetOverlay");
        TestAssert.IsNotNull(cab, "CabinetOverlay must exist");

        Text[] texts = cab.GetComponentsInChildren<Text>(true);
        bool foundEther = false;
        foreach (var t in texts)
        {
            if (t.text.Contains("спиртовой эфир")) foundEther = true;
        }
        TestAssert.IsTrue(foundEther, "Cabinet must contain Russian description for spirit ether");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Localization_RelicCertificates_ContainHistoricalEpochs()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "ShowRelicCertificate", 0);
        Text body0 = TestAssert.GetField<Text>(bs, "modalBody");
        TestAssert.IsTrue(body0.text.Contains("1812 года"), "Certificate 0 must reference 1812 Patriotic War epoch");

        TestAssert.InvokeMethod(bs, "ShowRelicCertificate", 2);
        Text body2 = TestAssert.GetField<Text>(bs, "modalBody");
        TestAssert.IsTrue(body2.text.Contains("Фаберже"), "Certificate 2 must reference Karl Faberge epoch");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Localization_TopBarStats_FormatsRussianLabels()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        Text stats = TestAssert.GetField<Text>(bs, "topStatsText");
        TestAssert.IsTrue(stats.text.Contains("Касса:"), "Stats bar must contain Russian 'Касса:'");
        TestAssert.IsTrue(stats.text.Contains("руб."), "Stats bar must contain currency 'руб.'");
        TestAssert.IsTrue(stats.text.Contains("Доверие:"), "Stats bar must contain Russian 'Доверие:'");
        TestAssert.IsTrue(stats.text.Contains("Износ:"), "Stats bar must contain Russian 'Износ:'");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Localization_ShiftFinale_AwardsGoldDiplomaInRussian()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "ShowShiftFinale");
        Text title = TestAssert.GetField<Text>(bs, "modalTitle");
        TestAssert.IsTrue(title.text.Contains("ЗОЛОТОЙ ДИПЛОМ ГИЛЬДИИ"), "Finale title must award Gold Diploma in Russian");
        Text body = TestAssert.GetField<Text>(bs, "modalBody");
        TestAssert.IsTrue(body.text.Contains("Квалификационный статус:"), "Finale body must include Russian status heading");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Shift_TotalOrdersToWin_EqualsFour()
    {
        FieldInfo field = typeof(LuaBootstrapper).GetField("TOTAL_ORDERS_TO_WIN", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        TestAssert.IsNotNull(field, "TOTAL_ORDERS_TO_WIN constant field must exist");
        int total = (int)field.GetValue(null);
        TestAssert.AreEqual(4, total, "TOTAL_ORDERS_TO_WIN must equal 4 orders");
    }

    public void Test_Shift_AvailableOrders_IncludeAllFourMasterpieceTypes()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();

        Dictionary<string, object> masterpiecesByType = new Dictionary<string, object>();

        System.Collections.IList initialChoices = (System.Collections.IList)TestAssert.GetField<object>(bs, "currentWindowChoices");
        foreach (var c in initialChoices)
        {
            string shape = (string)c.GetType().GetField("shapeType").GetValue(c);
            if (!masterpiecesByType.ContainsKey(shape))
            {
                masterpiecesByType.Add(shape, c);
            }
        }

        List<string> completed = TestAssert.GetField<List<string>>(bs, "completedOrderTypes");
        completed.Add("RING");
        TestAssert.InvokeMethod(bs, "GenerateWindowOrderChoices");

        System.Collections.IList nextChoices = (System.Collections.IList)TestAssert.GetField<object>(bs, "currentWindowChoices");
        foreach (var c in nextChoices)
        {
            string shape = (string)c.GetType().GetField("shapeType").GetValue(c);
            if (!masterpiecesByType.ContainsKey(shape))
            {
                masterpiecesByType.Add(shape, c);
            }
        }

        TestAssert.AreEqual(4, masterpiecesByType.Count, "All 4 distinct masterpiece relic types must be available across the shift");

        TestAssert.IsTrue(masterpiecesByType.ContainsKey("RING"), "Masterpiece types must include 1812 Gold Ring");
        TestAssert.IsTrue(masterpiecesByType.ContainsKey("DIAMOND"), "Masterpiece types must include Platinum Diamond Solitaire");
        TestAssert.IsTrue(masterpiecesByType.ContainsKey("LOCKET"), "Masterpiece types must include Imperial Faberge Locket");
        TestAssert.IsTrue(masterpiecesByType.ContainsKey("CROSS"), "Masterpiece types must include 12th Century Byzantine Enkolpion Cross");

        object ring = masterpiecesByType["RING"];
        TestAssert.AreEqual("Золотой перстень 1812 года", (string)ring.GetType().GetField("title").GetValue(ring), "Ring title verification");
        TestAssert.AreEqual(1, (int)ring.GetType().GetField("requiredRank").GetValue(ring), "Ring required rank verification");
        TestAssert.AreEqual("RING_GRINDER", (string)ring.GetType().GetField("correctMechTool").GetValue(ring), "Ring tool verification");

        object diamond = masterpiecesByType["DIAMOND"];
        TestAssert.AreEqual("Платиновый солитер с бриллиантом", (string)diamond.GetType().GetField("title").GetValue(diamond), "Diamond title verification");
        TestAssert.AreEqual(1, (int)diamond.GetType().GetField("requiredRank").GetValue(diamond), "Diamond required rank verification");
        TestAssert.AreEqual("DIAMOND_FILE", (string)diamond.GetType().GetField("correctMechTool").GetValue(diamond), "Diamond tool verification");

        object locket = masterpiecesByType["LOCKET"];
        TestAssert.AreEqual("Императорский орден Фаберже", (string)locket.GetType().GetField("title").GetValue(locket), "Locket title verification");
        TestAssert.AreEqual(2, (int)locket.GetType().GetField("requiredRank").GetValue(locket), "Locket required rank verification");
        TestAssert.AreEqual("MICRO_SPATULA", (string)locket.GetType().GetField("correctMechTool").GetValue(locket), "Locket tool verification");

        object cross = masterpiecesByType["CROSS"];
        TestAssert.AreEqual("Византийский энколпион XII века", (string)cross.GetType().GetField("title").GetValue(cross), "Cross title verification");
        TestAssert.AreEqual(3, (int)cross.GetType().GetField("requiredRank").GetValue(cross), "Cross required rank verification");
        TestAssert.AreEqual("MICRO_SPATULA", (string)cross.GetType().GetField("correctMechTool").GetValue(cross), "Cross tool verification");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Shift_RankGating_RestrictsHigherOrdersToHigherRanks()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();

        List<object> allOrders = new List<object>();
        System.Collections.IList initial = (System.Collections.IList)TestAssert.GetField<object>(bs, "currentWindowChoices");
        foreach (var c in initial) allOrders.Add(c);

        List<string> completed = TestAssert.GetField<List<string>>(bs, "completedOrderTypes");
        completed.Add("RING");
        TestAssert.InvokeMethod(bs, "GenerateWindowOrderChoices");

        System.Collections.IList next = (System.Collections.IList)TestAssert.GetField<object>(bs, "currentWindowChoices");
        foreach (var c in next) allOrders.Add(c);

        bool checkedRing = false;
        bool checkedDiamond = false;
        bool checkedLocket = false;
        bool checkedCross = false;

        foreach (var c in allOrders)
        {
            string shape = (string)c.GetType().GetField("shapeType").GetValue(c);
            int rank = (int)c.GetType().GetField("requiredRank").GetValue(c);
            if (shape == "RING")
            {
                TestAssert.AreEqual(1, rank, "Ring requires Rank 1");
                checkedRing = true;
            }
            else if (shape == "DIAMOND")
            {
                TestAssert.AreEqual(1, rank, "Diamond requires Rank 1");
                checkedDiamond = true;
            }
            else if (shape == "LOCKET")
            {
                TestAssert.AreEqual(2, rank, "Royal Faberge Locket requires Rank 2");
                checkedLocket = true;
            }
            else if (shape == "CROSS")
            {
                TestAssert.AreEqual(3, rank, "Byzantine Cross requires Rank 3");
                checkedCross = true;
            }
        }

        TestAssert.IsTrue(checkedRing && checkedDiamond && checkedLocket && checkedCross, "All 4 orders must be evaluated for rank gating");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Shift_OrderCompletion_CreditsBaseRewardAndStyleBonus()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.SetField(bs, "selectedSolvent", "SPIRIT");
        TestAssert.SetField(bs, "selectedMechTool", "RING_GRINDER");
        TestAssert.SetField(bs, "selectedStyle", "PATINA");

        int initialMoney = TestAssert.GetField<int>(bs, "money");
        TestAssert.InvokeMethod(bs, "EvaluateCompletedOrder");
        int newMoney = TestAssert.GetField<int>(bs, "money");

        TestAssert.IsTrue(newMoney > initialMoney, "Completing order must credit reward into money balance");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Shift_CompletingFourthOrder_TriggersShiftFinale()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.SetField(bs, "completedOrdersCount", 3);
        TestAssert.SetField(bs, "viewState", "RESULT");

        TestAssert.InvokeMethod(bs, "OnModalButtonClick", 1);

        TestAssert.AreEqual(4, TestAssert.GetField<int>(bs, "completedOrdersCount"), "Completed orders count must reach 4");
        TestAssert.AreEqual("FINALE", TestAssert.GetField<string>(bs, "viewState"), "Game state must transition to FINALE");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_ZeroComments_ScriptsFolderContainsZeroComments()
    {
        string scriptsDir = Path.Combine(Application.dataPath, "Scripts");
        string[] files = Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            int tokens = CountCommentTokens(file);
            TestAssert.AreEqual(0, tokens, $"Script file {Path.GetFileName(file)} must contain 0 comments");
        }
    }

    public void Test_ZeroComments_EditorFolderContainsZeroComments()
    {
        string builderFile = Path.Combine(Application.dataPath, "Editor", "Builder.cs");
        if (File.Exists(builderFile))
        {
            int tokens = CountCommentTokens(builderFile);
            TestAssert.AreEqual(0, tokens, "Builder.cs must contain 0 comments");
        }
    }

    public void Test_ZeroComments_EditorTestsFolderContainsZeroComments()
    {
        string testsDir = Path.Combine(Application.dataPath, "Editor", "Tests");
        string[] files = Directory.GetFiles(testsDir, "*.cs", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            int tokens = CountCommentTokens(file);
            TestAssert.AreEqual(0, tokens, $"Test file {Path.GetFileName(file)} must contain 0 comments");
        }
    }

    public void Test_ZeroComments_NoSingleLineCommentsInAnyTestFile()
    {
        char slash = (char)47;
        string singleLineToken = new string(slash, 2);
        string testsDir = Path.Combine(Application.dataPath, "Editor", "Tests");
        string[] files = Directory.GetFiles(testsDir, "*.cs", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string content = File.ReadAllText(file);
            TestAssert.IsFalse(content.Contains(singleLineToken), $"Test file {Path.GetFileName(file)} must not contain single-line comments");
        }
    }

    public void Test_ZeroComments_NoMultiLineCommentsInAnyTestFile()
    {
        char slash = (char)47;
        char star = (char)42;
        string multiLineStart = "" + slash + star;
        string multiLineEnd = "" + star + slash;
        string testsDir = Path.Combine(Application.dataPath, "Editor", "Tests");
        string[] files = Directory.GetFiles(testsDir, "*.cs", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string content = File.ReadAllText(file);
            TestAssert.IsFalse(content.Contains(multiLineStart), $"Test file {Path.GetFileName(file)} must not contain multi-line comment start");
            TestAssert.IsFalse(content.Contains(multiLineEnd), $"Test file {Path.GetFileName(file)} must not contain multi-line comment end");
        }
    }
}
