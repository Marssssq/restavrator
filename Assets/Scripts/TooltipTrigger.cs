using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string title;
    [SerializeField] private string description;
    [SerializeField] private string hotkeyHint;
    [SerializeField] private string databaseKey;

    public void SetContent(string newTitle, string newDescription, string newHotkeyHint = null)
    {
        title = newTitle;
        description = newDescription;
        hotkeyHint = newHotkeyHint;
    }

    public void SetKey(string key)
    {
        databaseKey = key;
        if (TooltipDatabase.TryGet(databaseKey, out TooltipDatabase.Entry entry))
        {
            title = entry.Title;
            description = entry.Description;
            hotkeyHint = entry.HotkeyHint;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hide();
    }

    private void OnMouseEnter()
    {
        Show();
    }

    private void OnMouseExit()
    {
        Hide();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void Show()
    {
        if (!string.IsNullOrEmpty(databaseKey) && TooltipDatabase.TryGet(databaseKey, out TooltipDatabase.Entry entry))
        {
            title = entry.Title;
            description = entry.Description;
            hotkeyHint = entry.HotkeyHint;
        }

        if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(description))
        {
            if (TooltipManager.Instance != null)
            {
                TooltipManager.Instance.ShowTooltip(title, description, hotkeyHint);
            }
        }
    }

    private void Hide()
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

    public static TooltipTrigger Attach(GameObject go, string key)
    {
        TooltipTrigger trigger = go.GetComponent<TooltipTrigger>();
        if (trigger == null) trigger = go.AddComponent<TooltipTrigger>();
        trigger.SetKey(key);
        return trigger;
    }

    public static TooltipTrigger Attach(GameObject go, string customTitle, string customDesc, string customHotkey = null)
    {
        TooltipTrigger trigger = go.GetComponent<TooltipTrigger>();
        if (trigger == null) trigger = go.AddComponent<TooltipTrigger>();
        trigger.SetContent(customTitle, customDesc, customHotkey);
        return trigger;
    }
}
