using RealmShards.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Simple yes/no confirmation overlay for lobby menus.
    /// </summary>
    public sealed class ConfirmDialog : MonoBehaviour
    {
        private GameObject _root;
        private Text _message;
        private System.Action _onConfirm;
        private System.Action _onCancel;
        private Button _yesButton;
        private Button _noButton;
        private MenuNavigator _nav;
        private System.Action _onClosed;

        public bool IsVisible => _root != null && _root.activeSelf;

        public static ConfirmDialog EnsurePresent(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<ConfirmDialog>();
            if (existing != null)
                return existing;

            var go = new GameObject(nameof(ConfirmDialog));
            go.transform.SetParent(parent, false);
            var dialog = go.AddComponent<ConfirmDialog>();
            dialog.Build();
            dialog.Hide();
            return dialog;
        }

        private void Build()
        {
            var canvas = UiFactory.CreateScreenCanvas("ConfirmDialog", 500);
            canvas.transform.SetParent(transform, false);
            _root = canvas.gameObject;

            UiFactory.AddPanel(canvas.transform, "Dim", new Color(0.02f, 0.02f, 0.04f, 0.78f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var box = new GameObject("Box", typeof(RectTransform));
            box.transform.SetParent(canvas.transform, false);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.28f, 0.36f);
            boxRt.anchorMax = new Vector2(0.72f, 0.64f);
            boxRt.offsetMin = Vector2.zero;
            boxRt.offsetMax = Vector2.zero;

            UiFactory.AddPanel(box.transform, "Border", new Color(0.9f, 0.88f, 0.95f, 0.95f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.AddPanel(box.transform, "Background", new Color(0.08f, 0.07f, 0.12f, 0.98f),
                Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            _message = UiFactory.AddText(box.transform, "Message", "Are you sure?", 20, TextAnchor.MiddleCenter,
                Color.white, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);

            _yesButton = UiFactory.AddButton(box.transform, "Yes", "CONFIRM",
                new Vector2(0.1f, 0.1f), new Vector2(0.46f, 0.28f), Vector2.zero, Vector2.zero,
                new Color(0.2f, 0.42f, 0.28f, 0.95f), UiFonts.MenuBold);
            _yesButton.GetComponentInChildren<Text>().fontSize = 18;
            _yesButton.onClick.AddListener(OnConfirm);

            _noButton = UiFactory.AddButton(box.transform, "No", "CANCEL",
                new Vector2(0.54f, 0.1f), new Vector2(0.9f, 0.28f), Vector2.zero, Vector2.zero,
                new Color(0.28f, 0.16f, 0.18f, 0.95f), UiFonts.MenuBold);
            _noButton.GetComponentInChildren<Text>().fontSize = 18;
            _noButton.onClick.AddListener(OnCancel);

            _nav = gameObject.AddComponent<MenuNavigator>();
        }

        public void Show(string message, System.Action onConfirm, System.Action onCancel = null, System.Action onClosed = null)
        {
            _message.text = message;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _onClosed = onClosed;
            _root.SetActive(true);
            gameObject.SetActive(true);
            GameContext.EnsureEventSystem();

            var yesRt = _yesButton.GetComponent<RectTransform>();
            var noRt = _noButton.GetComponent<RectTransform>();
            // Cancel pre-selected for destructive confirms (safer default).
            _nav.Configure(new[]
            {
                new MenuNavigator.Entry
                {
                    Visual = yesRt,
                    Selectable = _yesButton,
                    OnConfirm = OnConfirm
                },
                new MenuNavigator.Entry
                {
                    Visual = noRt,
                    Selectable = _noButton,
                    OnConfirm = OnCancel
                }
            }, onCancel: OnCancel, startIndex: 1);
            _nav.Activate(1);
        }

        public void Hide()
        {
            _nav?.Deactivate();
            if (_root != null)
                _root.SetActive(false);
            _onConfirm = null;
            _onCancel = null;
            var closed = _onClosed;
            _onClosed = null;
            closed?.Invoke();
        }

        private void OnConfirm()
        {
            var callback = _onConfirm;
            Hide();
            callback?.Invoke();
        }

        private void OnCancel()
        {
            var callback = _onCancel;
            Hide();
            callback?.Invoke();
        }
    }
}
