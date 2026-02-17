# com.core.initializer

A Unity package that instantiates and initializes all controllers implementing `IController` before any scene loads. Controllers are registered by type so you can access them anywhere via `ControllerHandler.GetController<T>()`.

## What It Does

- **Early initialization**: Runs at `RuntimeInitializeLoadType.BeforeSceneLoad`, so all controllers exist and are initialized before the first scene loads.
- **Central access**: Stores each controller by its type and exposes them through a type-safe getter.
- **Completion signaling**: Exposes a static event (`ControllersInitialized`) and a `UniTaskCompletionSource` (`InitializationCompleted`) so you can run code only after all controllers have finished initializing.

This gives you a single, consistent way to define “global” controllers (e.g. game state, input, audio, services) and use them from any script without scene references or manual wiring.

## How It Works

1. **Startup**: `ControllerHandler.Initialize()` is invoked by Unity via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` before any scene is loaded.

2. The package uses reflection (via the `Creator` helper) to:
   - Scan all loaded assemblies for types that implement `IController`.
   - Exclude interfaces and abstract classes.
   - Instantiate each concrete type with `Activator.CreateInstance`.

3. **Initialization**: For each instance, `Initialize()` is called (async via `UniTask`), and the controller is stored in a static dictionary keyed by its type.

4. **Access**: Any script can request a controller with `ControllerHandler.GetController<MyController>()`, which returns the registered instance or throws if that type was never registered.

## Dependencies

- **Unity** (6000.3 or compatible)
- **com.cysharp.unitask** (2.5.10) – used by `IController.Initialize()` for async initialization support

## Sample Use Case: Custom Controller and Access

### 1. Define a controller

Implement `IController` and add your logic in `Initialize()`:

```csharp
using com.core.initializer;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStateController : IController
{
    public bool IsInitialized { get; private set; }

    public async UniTask Initialize()
    {
        // Load config, setup state, etc.
        await LoadGameConfigAsync();
        IsInitialized = true;
    }

    private async UniTask LoadGameConfigAsync()
    {
        await UniTask.Delay(100); // placeholder for real async work
    }

    public int CurrentScore { get; set; }
    public void AddScore(int points) => CurrentScore += points;
}
```

### 2. Access the controller from anywhere

No scene references or dependency injection needed—just ask for the type:

```csharp
using com.core.initializer;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    private void Start()
    {
        var myController = ControllerHandler.GetController<GameStateController>();
        myController.AddScore(100);
        Debug.Log($"Score: {myController.CurrentScore}");
    }
}
```

## Waiting for Initialization to Complete

All controllers are initialized asynchronously before the first scene loads. If your code needs to run only after every controller has finished initializing, use the **event**, the **UniTaskCompletionSource**, or an **init/boot scene** that waits then loads the main scene.

### Option 1: Subscribe to the event

Use the static event `ControllerHandler.ControllersInitialized` for a callback when initialization is done:

```csharp
using com.core.initializer;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private void OnEnable()
    {
        ControllerHandler.ControllersInitialized += OnControllersInitialized;
    }

    private void OnDisable()
    {
        ControllerHandler.ControllersInitialized -= OnControllersInitialized;
    }

    private void OnControllersInitialized()
    {
        // All controllers are ready; safe to use GetController<T>() here.
        var gameState = ControllerHandler.GetController<GameStateController>();
        gameState.AddScore(0); // e.g. start game
    }
}
```

### Option 2: Await via UniTaskCompletionSource

Use `ControllerHandler.InitializationCompleted` to await completion in async code (e.g. in an async method or UniTask flow):

```csharp
using com.core.initializer;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    private async void Start()
    {
        // Wait until all controllers have finished initializing.
        await ControllerHandler.InitializationCompleted.Task;

        var gameState = ControllerHandler.GetController<GameStateController>();
        Debug.Log($"Ready. Score: {gameState.CurrentScore}");
    }
}
```

Or from a non-MonoBehaviour context (e.g. another static initializer or service):

```csharp
// Await in any async method
await ControllerHandler.InitializationCompleted.Task;
var myController = ControllerHandler.GetController<MyController>();
```

**Note:** `InitializationCompleted` is created when `ControllerHandler.Initialize()` runs (before the first scene). Awaiting its `Task` before that point would throw. In normal use (e.g. from `Start()` or later), initialization has already completed or is in progress, and awaiting will complete when ready.

### Option 3: Init / boot scene

Use a small initial scene (e.g. "Boot") as the first scene in Build Settings. In that scene, a script waits until all controllers are initialized, then loads your main scene. This keeps boot logic in one place and guarantees the main scene only runs after initialization is done.

```csharp
using com.core.initializer;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    [SerializeField] private string _mainSceneName = "Main";

    private async void Start()
    {
        await ControllerHandler.InitializationCompleted.Task;
        await SceneManager.LoadSceneAsync(_mainSceneName);
    }
}
```

Set "Boot" (or your init scene) as scene index 0 in **File → Build Settings**, and your main scene as the next. When the game starts, the boot scene loads, waits for controller initialization, then loads the main scene.

## Summary

| Step | Action |
|------|--------|
| 1 | Create a class that implements `IController` and implement `Initialize()` and `IsInitialized`. |
| 2 | Let the package run at startup; it will find, create, and initialize all such controllers. |
| 3 | Use `var myController = ControllerHandler.GetController<MyController>();` wherever you need that controller. |

No manual registration or scene setup is required—any concrete `IController` in your project is picked up automatically.
