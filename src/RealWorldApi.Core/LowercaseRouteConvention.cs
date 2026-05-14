using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace RealWorldApi.Core;

public class LowercaseRouteConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                if (selector.AttributeRouteModel?.Template != null)
                {
                    selector.AttributeRouteModel.Template =
                        selector.AttributeRouteModel.Template
                            .Replace("[controller]", controller.ControllerName.ToLowerInvariant());
                }
            }
        }
    }
}