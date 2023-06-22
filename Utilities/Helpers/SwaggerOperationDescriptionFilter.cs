using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Helpers
{
    public class SwaggerOperationDescriptionFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var controllerActionDescriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;

            if (controllerActionDescriptor != null)
            {
                var methodDescription = controllerActionDescriptor.MethodInfo.GetCustomAttributes(true)
                    .OfType<DescriptionAttribute>()
                    .FirstOrDefault();

                if (methodDescription != null)
                {
                    operation.Description = methodDescription.Description;
                }
            }
        }
    }
}
