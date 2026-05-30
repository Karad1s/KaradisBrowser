using System;

namespace Kar
{
    public interface IDispatcherService
    {
        void Invoke(Action action);
        bool CheckAccess();
    }
}
