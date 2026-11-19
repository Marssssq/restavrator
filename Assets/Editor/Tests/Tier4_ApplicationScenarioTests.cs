using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tier4_ApplicationScenarioTests
{
    public void Test_Scenario1_FlawlessFullShift_LeadsToGuildGoldDiploma()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();

        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.SetField(bs, "selectedSolvent", "SPIRIT");
        TestAssert.SetField(bs, "selectedMechTool", "RING_GRINDER");
        TestAssert.SetField(bs, "selectedStyle", "PATINA");
        TestAssert.SetField(bs, "cleanProgress", 85);
        TestAssert.SetField(bs, "grindProgress", 85);
        TestAssert.SetField(bs, "clickFixProgress", 100);
        TestAssert.SetField(bs, "finishProgress", 100f);
        TestAssert.InvokeMethod(bs, "EvaluateCompletedOrder");
        TestAssert.InvokeMethod(bs, "OnModalButtonClick", 1);

        TestAssert.SetField(bs, "money", 3500);
        TestAssert.InvokeMethod(bs, "BuyUpgrade", 2);
        TestAssert.AreEqual(2, TestAssert.GetField<int>(bs, "qualificationRank"), "Rank II certification obtained");

        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.SetField(bs, "selectedSolvent", "ACID_GEL");
        TestAssert.SetField(bs, "selectedMechTool", "DIAMOND_FILE");
        TestAssert.SetField(bs, "selectedStyle", "NEW");
        TestAssert.SetField(bs, "cleanProgress", 90);
        TestAssert.SetField(bs, "grindProgress", 90);
        TestAssert.SetField(bs, "clickFixProgress", 100);
        TestAssert.SetField(bs, "finishProgress", 100f);
        TestAssert.InvokeMethod(bs, "EvaluateCompletedOrder");
        TestAssert.InvokeMethod(bs, "OnModalButtonClick", 1);

        TestAssert.SetField(bs, "money", 4000);
        TestAssert.InvokeMethod(bs, "BuyUpgrade", 3);
        TestAssert.AreEqual(3, TestAssert.GetField<int>(bs, "qualificationRank"), "Grand Master Rank III obtained");

        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.SetField(bs, "selectedSolvent", "SPIRIT");
        TestAssert.SetField(bs, "selectedMechTool", "MICRO_SPATULA");
        TestAssert.SetField(bs, "selectedStyle", "PATINA");
        TestAssert.SetField(bs, "cleanProgress", 90);
        TestAssert.SetField(bs, "grindProgress", 90);
        TestAssert.SetField(bs, "clickFixProgress", 100);
        TestAssert.SetField(bs, "finishProgress", 100f);
        TestAssert.InvokeMethod(bs, "EvaluateCompletedOrder");
        TestAssert.InvokeMethod(bs, "OnModalButtonClick", 1);

        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", 0);
        TestAssert.SetField(bs, "selectedSolvent", "SPIRIT");
        TestAssert.SetField(bs, "selectedMechTool", "MICRO_SPATULA");
        TestAssert.SetField(bs, "selectedStyle", "PATINA");
        TestAssert.SetField(bs, "cleanProgress", 95);
        TestAssert.SetField(bs, "grindProgress", 95);
        TestAssert.SetField(bs, "clickFixProgress", 100);
        TestAssert.SetField(bs, "finishProgress", 100f);
        TestAssert.InvokeMethod(bs, "EvaluateCompletedOrder");
        TestAssert.InvokeMethod(bs, "OnModalButtonClick", 1);

        TestAssert.AreEqual(4, TestAssert.GetField<int>(bs, "completedOrdersCount"), "All 4 orders completed");
        TestAssert.AreEqual("FINALE", TestAssert.GetField<string>(bs, "viewState"), "Final shift state must be FINALE");

        Text title = TestAssert.GetField<Text>(bs, "modalTitle");
        TestAssert.IsTrue(title.text.Contains("ЗОЛОТОЙ ДИПЛОМ ГИЛЬДИИ"), "Finale title awards Gold Diploma");

        Text body = TestAssert.GetField<Text>(bs, "modalBody");
        TestAssert.IsTrue(body.text.Contains("Гранд-мастер"), "Finale body confirms Grand Master status");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Scenario2_HistoricalSecretCompartmentCipher_PreservedVsBreached()
    {
        LuaBootstrapper bsPreserve = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bsPreserve, "SelectOrderFromWindow", 0);
        TestAssert.SetField(bsPreserve, "viewState", "TWIST");
        TestAssert.InvokeMethod(bsPreserve, "OnModalButtonClick", 2);

        TestAssert.IsFalse(TestAssert.GetField<bool>(bsPreserve, "secretViolated"), "Cipher was preserved");
        TestAssert.AreEqual(100, TestAssert.GetField<int>(bsPreserve, "trust"), "Trust preserved at max 100");
        TestAssert.DestroyBootstrapper(bsPreserve);

        LuaBootstrapper bsBreach = TestAssert.CreateBootstrapper();
        TestAssert.InvokeMethod(bsBreach, "SelectOrderFromWindow", 0);
        TestAssert.SetField(bsBreach, "viewState", "TWIST");
        TestAssert.InvokeMethod(bsBreach, "OnModalButtonClick", 1);

        TestAssert.IsTrue(TestAssert.GetField<bool>(bsBreach, "secretViolated"), "Cipher was violated");
        TestAssert.AreEqual(40, TestAssert.GetField<int>(bsBreach, "trust"), "Trust dropped by 60 to 40");

        TestAssert.SetField(bsBreach, "selectedSolvent", "SPIRIT");
        TestAssert.SetField(bsBreach, "selectedMechTool", "RING_GRINDER");
        TestAssert.SetField(bsBreach, "selectedStyle", "PATINA");
        TestAssert.InvokeMethod(bsBreach, "EvaluateCompletedOrder");
        TestAssert.InvokeMethod(bsBreach, "OnModalButtonClick", 1);

        System.Collections.IList choices = (System.Collections.IList)TestAssert.GetField<object>(bsBreach, "currentWindowChoices");
        object firstChoice = choices[0];
        string title = (string)firstChoice.GetType().GetField("title").GetValue(firstChoice);
        TestAssert.IsTrue(title.Contains("Латунный перстень"), "Breaching cipher locks player into cheap brass orders");

        TestAssert.DestroyBootstrapper(bsBreach);
    }

    public void Test_Scenario3_DiplomaticChangeOfMindTelegram_HandledCorrectly()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();
        TestAssert.SetField(bs, "qualificationRank", 2);
        TestAssert.InvokeMethod(bs, "GenerateWindowOrderChoices");

        System.Collections.IList choices = (System.Collections.IList)TestAssert.GetField<object>(bs, "currentWindowChoices");
        int royalIdx = -1;
        for (int i = 0; i < choices.Count; i++)
        {
            string shape = (string)choices[i].GetType().GetField("shapeType").GetValue(choices[i]);
            if (shape == "LOCKET") { royalIdx = i; break; }
        }
        TestAssert.IsTrue(royalIdx >= 0, "Faberge Locket order must be present in choices");

        TestAssert.InvokeMethod(bs, "SelectOrderFromWindow", royalIdx);
        TestAssert.SetField(bs, "viewState", "TWIST");

        TestAssert.InvokeMethod(bs, "OnModalButtonClick", 1);

        object activeOrder = TestAssert.GetField<object>(bs, "activeOrder");
        string preferred = (string)activeOrder.GetType().GetField("preferredStyle").GetValue(activeOrder);
        TestAssert.AreEqual("NEW", preferred, "Preferred style changed to NEW by telegram");
        TestAssert.AreEqual("NEW", TestAssert.GetField<string>(bs, "selectedStyle"), "Selected style updated to NEW");

        TestAssert.SetField(bs, "selectedSolvent", "SPIRIT");
        TestAssert.SetField(bs, "selectedMechTool", "MICRO_SPATULA");
        TestAssert.InvokeMethod(bs, "EvaluateCompletedOrder");

        Text modalBody = TestAssert.GetField<Text>(bs, "modalBody");
        TestAssert.IsTrue(modalBody.text.Contains("ВЫБОР СТИЛЯ: Полное попадание в ожидания клиента"), "Style match bonus credited for telegram request");

        TestAssert.DestroyBootstrapper(bs);
    }

    public void Test_Scenario4_AuthenticityCertificatesGeneration_MatchesHistoricalLore()
    {
        LuaBootstrapper bs = TestAssert.CreateBootstrapper();

        string[] expectedTokens = { "1812 года", "Санкт-Петербурга", "Фаберже", "Царьград" };
        string[] expectedNumbers = { "№01-1926", "№02-1926", "№03-1926", "№04-1926" };

        for (int i = 0; i < 4; i++)
        {
            TestAssert.InvokeMethod(bs, "ShowRelicCertificate", i);

            TestAssert.AreEqual("CERTIFICATE", TestAssert.GetField<string>(bs, "viewState"), "View state set to CERTIFICATE");

            GameObject seal = TestAssert.GetField<GameObject>(bs, "modalSealGO");
            TestAssert.IsTrue(seal.activeSelf, "Modal wax seal must be active");
            TestAssert.AreEqual("ЭТАЛОН", TestAssert.GetField<Text>(bs, "modalSealText").text, "Seal text must be ETALON");

            Text body = TestAssert.GetField<Text>(bs, "modalBody");
            TestAssert.IsTrue(body.text.Contains(expectedTokens[i]), $"Certificate {i} must mention {expectedTokens[i]}");
            TestAssert.IsTrue(body.text.Contains(expectedNumbers[i]), $"Certificate {i} must have registry number {expectedNumbers[i]}");
        }

        TestAssert.DestroyBootstrapper(bs);
    }
}
