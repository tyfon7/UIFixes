using System;

using TMPro;

using UnityEngine;

namespace UIFixes;

public class SearchKeyListener : MonoBehaviour
{
    private Action onSearch;

    public void Init(Action onSearch)
    {
        this.onSearch = onSearch;
    }

    public void Update()
    {
        if (Settings.SearchKeyBind.Value.IsDown())
        {
            if (onSearch != null)
            {
                onSearch();
            }
            else
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
}