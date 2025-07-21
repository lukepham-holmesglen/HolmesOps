using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiSystem : MonoSingleton<UiSystem>
{
    public GameObject ContainerCanvas;

    [SerializeField] private UiView defaultView;
    
    private UiView[] views;
    public UiView currentView;
    private readonly Stack<UiView> history = new Stack<UiView>();

    private UiPopup[] popups;

    public static T GetView<T>() where T : UiView
    {
        for (int i = 0; i < Instance.views.Length; i++)
        {
            if (Instance.views[i] is T view)
            {
                return view;
            }
        }

        return null;
    }

    public static void Show<T>(bool remember = true) where T : UiView
    {
        if (Instance.currentView == GetView<T>()) return;

        if (Instance.currentView != null)
        {
            if (remember)
            {
                Instance.history.Push(Instance.currentView);
            }
            Instance.currentView.Hide();
        }

        Instance.currentView = GetView<T>();
        Instance.currentView.Show();

    }

    public static T GetPopup<T>() where T : UiPopup
    {
        for (int i = 0; i < Instance.popups.Length; i++)
        {
            if (Instance.popups[i] is T view)
            {
                return view;
            }
        }

        return null;
    }

    public static void ShowPopup<T>(string title = null, string message = null, Action callback = null, string dismissText = "No", string confirmText = "Yes") where T : UiPopup
    {
        GetPopup<T>().Show(title, message, callback, dismissText, confirmText);
    }

    public static void HidePopup<T>() where T : UiPopup
    {
        GetPopup<T>().Hide();
    }

    public static void Show(UiView view, bool remember = true)
    {
        if (Instance.currentView != null)
        {
            if (remember)
            {
                Instance.history.Push(Instance.currentView);
            }
            Instance.currentView.Hide();
        }

        view.Show();
        Instance.currentView = view;
    }

    public static void Back()
    {
        if (Instance.history.Count != 0)
        {
            Show(Instance.history.Pop(), false);
        }
    }

    private void Start()
    {
        views = ContainerCanvas.GetComponentsInChildren<UiView>(true);
        popups = ContainerCanvas.GetComponentsInChildren<UiPopup>(true);

        bool skipGameScreenHide = GameMan.Instance != null && GameMan.Instance.IsAutoStarting;

        foreach (var view in views)
        {
            view.Initialise();

            // Skip hiding the GameScreen if auto-starting
            if (skipGameScreenHide && view is GameScreen)
                continue;

            view.Hide();
        }

        foreach (var popup in popups)
        {
            popup.Initialise();
            popup.Hide();
        }

        if (defaultView != null && (!skipGameScreenHide || defaultView is not GameScreen))
        {
            Show(defaultView);
        }
    }

}
