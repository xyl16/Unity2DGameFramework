using System;
using System.Text;
using UnityEngine;

public class LoginController : BaseController
{
    private const ushort LOGIN_REQUEST = 1001;
    private const ushort LOGIN_RESPONSE = 1002;
    private const ushort LOGOUT_REQUEST = 1003;

    public override void Init()
    {
        base.Init();

        Logger.Instance.LogInfo("Initializing LoginController", "Login");

        NetworkManager.Instance.RegisterMessageHandler(LOGIN_RESPONSE, OnLoginResponse);

        if (model == null)
        {
            model = new LoginModel();
            model.Init();
        }

        EventManager.Instance.AddListener("EnterLoginState", OnEnterLoginState);
    }

    private void OnEnterLoginState(object data)
    {
        Logger.Instance.LogInfo("Entering login state", "Login");
    }

    public void Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowMessage("Username and password cannot be empty");
            return;
        }

        LoginModel loginModel = model as LoginModel;
        if (loginModel != null)
        {
            loginModel.Username = username;
            loginModel.Password = password;

            LoginRequestData requestData = new LoginRequestData
            {
                username = username,
                password = password,
                timestamp = DateTime.UtcNow.Ticks
            };

            string json = JsonUtility.ToJson(requestData);
            byte[] data = Encoding.UTF8.GetBytes(json);

            NetworkManager.Instance.SendMessage(LOGIN_REQUEST, data);

            ShowMessage("Logging in...");
            Logger.Instance.LogInfo($"Login attempt for user: {username}", "Login");
        }
    }

    private void OnLoginResponse(byte[] data)
    {
        try
        {
            LoginResponseData responseData = JsonUtility.FromJson<LoginResponseData>(
                Encoding.UTF8.GetString(data, 2, data.Length - 2));

            if (responseData.success)
            {
                LoginModel loginModel = model as LoginModel;
                if (loginModel != null)
                {
                    loginModel.IsLoggedIn = true;
                    loginModel.UserId = responseData.userId;
                    loginModel.Token = responseData.token;
                }

                ShowMessage("Login success!");
                Logger.Instance.LogInfo($"Login successful for user: {responseData.userId}", "Login");

                DataManager.Instance.SaveData("UserSession", loginModel);

                GameManager.Instance.OnLoginSuccess();
            }
            else
            {
                string errorMessage = string.IsNullOrEmpty(responseData.message) ?
                    "Login failed" : responseData.message;

                ShowMessage(errorMessage);
                Logger.Instance.LogWarning($"Login failed: {errorMessage}", "Login");
                GameManager.Instance.OnLoginFailed(errorMessage);
            }
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"Failed to parse login response: {e.Message}", "Login");
            ShowMessage("Login failed: Invalid response");
            GameManager.Instance.OnLoginFailed("Invalid response");
        }
    }

    public void Logout()
    {
        LoginModel loginModel = model as LoginModel;
        if (loginModel != null && loginModel.IsLoggedIn)
        {
            NetworkManager.Instance.SendMessage(LOGOUT_REQUEST, Encoding.UTF8.GetBytes(loginModel.Token));

            loginModel.IsLoggedIn = false;
            loginModel.UserId = "";
            loginModel.Token = "";

            Logger.Instance.LogInfo("User logged out", "Login");
        }
    }

    private void ShowMessage(string message)
    {
        if (view != null)
        {
            (view as LoginView).ShowMessage(message);
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        NetworkManager.Instance.UnregisterMessageHandler(LOGIN_RESPONSE);
        EventManager.Instance.RemoveListener("EnterLoginState", OnEnterLoginState);

        Logger.Instance.LogInfo("LoginController disposed", "Login");
    }
}

[System.Serializable]
public class LoginRequestData
{
    public string username;
    public string password;
    public long timestamp;
}

[System.Serializable]
public class LoginResponseData
{
    public bool success;
    public string userId;
    public string token;
    public string message;
}