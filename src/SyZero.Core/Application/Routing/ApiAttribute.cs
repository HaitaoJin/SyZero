using System;
using System.Collections.Generic;
using System.Text;

namespace SyZero.Application.Routing
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public class ApiAttribute : Attribute
    {

        public ApiAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}
