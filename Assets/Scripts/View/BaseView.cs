using UnityEngine;

public class BaseView : MonoBehaviour
{
    protected BaseController controller;

    public void SetController(BaseController ctrl)
    {
        controller = ctrl;
    }

    public virtual void Init() { }
    public virtual void UpdateView() { }
    public virtual void Show() { gameObject.SetActive(true); }
    public virtual void Hide() { gameObject.SetActive(false); }
    public virtual void Dispose() { }

    protected virtual void OnDestroy()
    {
        Dispose();
    }
}