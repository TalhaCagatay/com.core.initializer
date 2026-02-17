using Cysharp.Threading.Tasks;

namespace com.core.initializer
{
    public interface IController
    {
        bool IsInitialized { get; }

        UniTask Initialize();
    }
}