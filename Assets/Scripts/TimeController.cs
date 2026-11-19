using System;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    public static TimeController Instance { get; private set; }

    public const float TotalShiftDuration = 720f;
    public const float PhaseDuration = 180f;

    private float shiftTimeSeconds = 0f;
    private bool isPaused = false;
    private string lastReportedPhase = "Утро";

    public Action<string> OnPhaseTransition;

    public float ShiftTimeSeconds
    {
        get => shiftTimeSeconds;
        set
        {
            string oldPhase = CurrentPhase;
            shiftTimeSeconds = Mathf.Clamp(value, 0f, TotalShiftDuration);
            string newPhase = CurrentPhase;
            if (oldPhase != newPhase)
            {
                lastReportedPhase = newPhase;
                OnPhaseTransition?.Invoke(newPhase);
            }
        }
    }

    public string CurrentPhase
    {
        get
        {
            if (shiftTimeSeconds < 180f) return "Утро";
            if (shiftTimeSeconds < 360f) return "День";
            if (shiftTimeSeconds < 540f) return "Закат";
            return "Вечер";
        }
    }

    public int CurrentPhaseIndex
    {
        get
        {
            if (shiftTimeSeconds < 180f) return 0;
            if (shiftTimeSeconds < 360f) return 1;
            if (shiftTimeSeconds < 540f) return 2;
            return 3;
        }
    }

    public float PhaseProgress
    {
        get
        {
            if (shiftTimeSeconds >= TotalShiftDuration) return 1f;
            float localTime = shiftTimeSeconds % PhaseDuration;
            return Mathf.Clamp01(localTime / PhaseDuration);
        }
    }

    public bool IsShiftComplete => shiftTimeSeconds >= TotalShiftDuration;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        lastReportedPhase = CurrentPhase;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    public void AdvanceTime(float dt)
    {
        if (!isPaused && shiftTimeSeconds < TotalShiftDuration)
        {
            string oldPhase = CurrentPhase;
            shiftTimeSeconds = Mathf.Min(TotalShiftDuration, shiftTimeSeconds + dt);
            string newPhase = CurrentPhase;
            if (oldPhase != newPhase)
            {
                lastReportedPhase = newPhase;
                OnPhaseTransition?.Invoke(newPhase);
            }
        }
    }

    public void AdvancePhase()
    {
        int curIdx = CurrentPhaseIndex;
        int nextIdx = (curIdx + 1) % 4;
        shiftTimeSeconds = nextIdx * PhaseDuration + 2f;
        string newPhase = CurrentPhase;
        lastReportedPhase = newPhase;
        OnPhaseTransition?.Invoke(newPhase);
    }

    public void SyncWithOrderCount(int completedOrders)
    {
        int targetPhaseIdx = Mathf.Clamp(completedOrders, 0, 3);
        float phaseStartTime = targetPhaseIdx * PhaseDuration;
        if (shiftTimeSeconds < phaseStartTime)
        {
            string oldPhase = CurrentPhase;
            shiftTimeSeconds = phaseStartTime;
            string newPhase = CurrentPhase;
            if (oldPhase != newPhase)
            {
                lastReportedPhase = newPhase;
                OnPhaseTransition?.Invoke(newPhase);
            }
        }
    }

    public void ResetShift()
    {
        shiftTimeSeconds = 0f;
        isPaused = false;
        lastReportedPhase = "Утро";
    }

    public Color GetAmbientColor(string phase, float progress)
    {
        Color c0 = GetBaseAmbientColor(phase);
        string next = GetNextPhase(phase);
        Color c1 = GetBaseAmbientColor(next);
        return Color.Lerp(c0, c1, Mathf.Clamp01(progress));
    }

    public Color GetWindowColor(string phase, float progress)
    {
        Color c0 = GetBaseWindowColor(phase);
        string next = GetNextPhase(phase);
        Color c1 = GetBaseWindowColor(next);
        return Color.Lerp(c0, c1, Mathf.Clamp01(progress));
    }

    public static Color GetBaseWindowColor(string phase)
    {
        switch (phase)
        {
            case "Утро":
                return new Color(0.85f, 0.95f, 1f, 0.36f);
            case "День":
                return new Color(1f, 0.98f, 0.80f, 0.42f);
            case "Закат":
                return new Color(1f, 0.60f, 0.30f, 0.46f);
            case "Вечер":
            default:
                return new Color(0.24f, 0.35f, 0.65f, 0.38f);
        }
    }

    public static Color GetBaseAmbientColor(string phase)
    {
        switch (phase)
        {
            case "Утро":
                return new Color(1f, 0.92f, 0.70f, 0.28f);
            case "День":
                return new Color(1f, 0.90f, 0.65f, 0.32f);
            case "Закат":
                return new Color(1f, 0.82f, 0.48f, 0.38f);
            case "Вечер":
            default:
                return new Color(1f, 0.75f, 0.32f, 0.48f);
        }
    }

    private static string GetNextPhase(string phase)
    {
        switch (phase)
        {
            case "Утро": return "День";
            case "День": return "Закат";
            case "Закат": return "Вечер";
            case "Вечер":
            default:
                return "Вечер";
        }
    }
}
