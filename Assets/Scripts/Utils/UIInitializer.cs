using UnityEngine;
using UnityEngine.UI;

public class UIInitializer : MonoBehaviour
{
    private void Start()
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 创建登录面板
        GameObject loginPanel = new GameObject("LoginPanel");
        loginPanel.transform.SetParent(canvasObj.transform);
        RectTransform panelRect = loginPanel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 300);
        panelRect.anchoredPosition = Vector2.zero;

        // 添加背景
        Image background = loginPanel.AddComponent<Image>();
        background.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);

        // 创建用户名输入框
        GameObject usernameObj = new GameObject("UsernameInput");
        usernameObj.transform.SetParent(loginPanel.transform);
        RectTransform usernameRect = usernameObj.AddComponent<RectTransform>();
        usernameRect.sizeDelta = new Vector2(200, 30);
        usernameRect.anchoredPosition = new Vector2(0, 50);
        InputField usernameInput = usernameObj.AddComponent<InputField>();

        // 创建密码输入框
        GameObject passwordObj = new GameObject("PasswordInput");
        passwordObj.transform.SetParent(loginPanel.transform);
        RectTransform passwordRect = passwordObj.AddComponent<RectTransform>();
        passwordRect.sizeDelta = new Vector2(200, 30);
        passwordRect.anchoredPosition = new Vector2(0, 0);
        InputField passwordInput = passwordObj.AddComponent<InputField>();
        passwordInput.contentType = InputField.ContentType.Password;

        // 创建登录按钮
        GameObject loginBtnObj = new GameObject("LoginButton");
        loginBtnObj.transform.SetParent(loginPanel.transform);
        RectTransform loginBtnRect = loginBtnObj.AddComponent<RectTransform>();
        loginBtnRect.sizeDelta = new Vector2(100, 40);
        loginBtnRect.anchoredPosition = new Vector2(0, -50);
        Button loginButton = loginBtnObj.AddComponent<Button>();
        Text loginText = loginBtnObj.AddComponent<Text>();
        loginText.text = "Login";
        loginText.alignment = TextAnchor.MiddleCenter;

        // 创建消息文本
        GameObject messageObj = new GameObject("MessageText");
        messageObj.transform.SetParent(loginPanel.transform);
        RectTransform messageRect = messageObj.AddComponent<RectTransform>();
        messageRect.sizeDelta = new Vector2(300, 30);
        messageRect.anchoredPosition = new Vector2(0, -100);
        Text messageText = messageObj.AddComponent<Text>();
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = Color.red;

        // 添加LoginView脚本
        LoginView loginView = loginPanel.AddComponent<LoginView>();
        loginView.usernameInput = usernameInput;
        loginView.passwordInput = passwordInput;
        loginView.loginButton = loginButton;
        loginView.messageText = messageText;

        // 初始化控制器
        LoginController loginController = new LoginController();
        loginController.SetView(loginView);
        loginController.Init();

        // 设置模型
        LoginModel loginModel = new LoginModel();
        loginModel.Init();
        loginController.SetModel(loginModel);
    }
}