using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace DOA_API_Exchange_Service_For_Gateway.Middlewares
{
    /// <summary>
    /// Convention สำหรับเพิ่ม Route Prefix ให้กับทุก Controller โดยอ่านค่าจาก appsettings.json (ApiSettings:RoutePrefix)
    /// </summary>
    public class GlobalRoutePrefixConvention : IApplicationModelConvention
    {
        private readonly AttributeRouteModel _routePrefix;

        public GlobalRoutePrefixConvention(string prefix)
        {
            _routePrefix = new AttributeRouteModel(new Microsoft.AspNetCore.Mvc.RouteAttribute(prefix));
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var selector in application.Controllers.SelectMany(c => c.Selectors))
            {
                if (selector.AttributeRouteModel != null)
                {
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(
                        _routePrefix,
                        selector.AttributeRouteModel
                    );
                }
                else
                {
                    selector.AttributeRouteModel = _routePrefix;
                }
            }
        }
    }
}
