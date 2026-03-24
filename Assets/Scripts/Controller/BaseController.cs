using System;

public class BaseController
{
    protected BaseModel model;
    protected BaseView view;

    public void SetModel(BaseModel mdl)
    {
        model = mdl;
    }

    public void SetView(BaseView vw)
    {
        view = vw;
        view.SetController(this);
    }

    public virtual void Init()
    {
        model?.Init();
        view?.Init();
    }

    public virtual void Update()
    {
        view?.UpdateView();
    }

    public virtual void Dispose()
    {
        model?.Dispose();
        view?.Dispose();
    }
}