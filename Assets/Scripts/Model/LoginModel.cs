public class LoginModel : BaseModel
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string UserId { get; set; }
    public string Token { get; set; }
    public bool IsLoggedIn { get; set; }
    public System.DateTime LastLoginTime { get; set; }

    public override void Init()
    {
        base.Init();
        IsLoggedIn = false;
        UserId = "";
        Token = "";
        LastLoginTime = System.DateTime.MinValue;
    }

    public void ClearSensitiveData()
    {
        Password = "";
    }
}