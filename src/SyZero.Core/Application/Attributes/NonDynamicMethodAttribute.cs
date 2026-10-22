using System;

namespace SyZero.Application.Attributes
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
    public class NonDynamicMethodAttribute : Attribute
    {

    }
}
