using System;

namespace SyZero.Application.Attributes
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
    public class NonDynamicApiAttribute : Attribute
    {

    }
}