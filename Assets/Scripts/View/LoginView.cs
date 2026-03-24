using UnityEngine;
using UnityEngine.UI;

public class LoginView : BaseView
{
    public InputField usernameInput;
    public InputField passwordInput;
    public Button loginButton;
    public Text messageText;

    public override void Init()
    {
        base.Init();

        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClick);
        }
    }

    private void OnLoginButtonClick()
    {
        if (controller != null)
        {
            string username = usernameInput.text;
            string password = passwordInput.text;
            (controller as LoginController).Login(username, password);
        }
    }

    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners();
        }
    }
}