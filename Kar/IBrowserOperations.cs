using System;

namespace Kar
{
    public interface IBrowserOperations
    {
        bool CanGoBack { get; }
        bool CanGoForward { get; }
        void GoBack();
        void GoForward();
        void Reload();
        event EventHandler<string> AddressChanged;
    }
}
