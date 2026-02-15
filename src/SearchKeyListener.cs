using System;
using TMPro;
using UnityEngine;

namespace UIFixes;

public class SearchKeyListener : MonoBehaviour
{
    private Action _onSearch;

    public void Init(Action onSearch)
    {
        _onSearch = onSearch;
    }

    public void Update()
    {
        if (Settings.SearchKeyBind.Value.IsDown())
        {
            if (_onSearch != null)
            {
                _onSearch();
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