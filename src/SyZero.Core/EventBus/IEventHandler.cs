using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SyZero.EventBus
{
    public interface IEventHandler<in TEvent> : IEventHandler
    {
        Task HandleAsync(TEvent @event);
    }

    public interface IEventHandler
    {
    }
}
