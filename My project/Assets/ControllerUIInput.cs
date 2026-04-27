using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ControllerUIInput : MonoBehaviour
{
    private Selectable[] selectables;
    private int currentIndex = 0;
    private bool thumbstickMoved = false;
    private Color highlightColor = new Color(0.3f, 0.7f, 1f, 1f);
    private Dictionary<int, Color> originalColors = new Dictionary<int, Color>();

    void Start()
    {
        selectables = FindObjectsOfType<Selectable>();
        for (int i = 0; i < selectables.Length; i++)
        {
            var img = selectables[i].GetComponent<Image>();
            if (img != null)
                originalColors[i] = img.color;
        }
        if (selectables.Length > 0)
        {
            selectables[0].Select();
            Highlight(0);
        }
    }

    void Highlight(int index)
    {
        for (int i = 0; i < selectables.Length; i++)
        {
            var img = selectables[i].GetComponent<Image>();
            if (img != null && originalColors.ContainsKey(i))
                img.color = originalColors[i];
        }
        var currentImg = selectables[index].GetComponent<Image>();
        if (currentImg != null)
            currentImg.color = highlightColor;
    }

    void Update()
    {
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        if (!thumbstickMoved)
        {
            if (thumbstick.y > 0.5f)
            {
                currentIndex = (currentIndex - 1 + selectables.Length) % selectables.Length;
                selectables[currentIndex].Select();
                Highlight(currentIndex);
                thumbstickMoved = true;
            }
            else if (thumbstick.y < -0.5f)
            {
                currentIndex = (currentIndex + 1) % selectables.Length;
                selectables[currentIndex].Select();
                Highlight(currentIndex);
                thumbstickMoved = true;
            }
        }

        if (Mathf.Abs(thumbstick.y) < 0.2f)
            thumbstickMoved = false;

        if (OVRInput.GetDown(OVRInput.RawButton.RHandTrigger))
        {
            var current = selectables[currentIndex];
            if (current is TMP_InputField inputField)
            {
                inputField.Select();
                inputField.ActivateInputField();
                TouchScreenKeyboard.Open(inputField.text, TouchScreenKeyboardType.Default);
            }
            else if (current is Button)
            {
                ExecuteEvents.Execute(current.gameObject,
                    new PointerEventData(EventSystem.current),
                    ExecuteEvents.pointerClickHandler);
            }
        }
    }
}