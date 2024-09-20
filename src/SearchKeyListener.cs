using TMPro;
using UnityEngine;

namespace UIFixes;

public class SearchKeyListener : MonoBehaviour
{
    public void Update()
    {
        if (Settings.SearchKeyBind.Value.IsDown())
        {
            TMP_InputField searchField = GetComponent<TMP_InputField>();
            if (searchField != null)
            {
                searchField.ActivateInputField();
                searchField.Select();
            }
        }
    }
}