using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.core.initializer
{
    public class ControllerHandler
    {
        public static readonly UniTaskCompletionSource InitializationCompleted = new();

        public static event Action ControllersInitialized;

        private static Dictionary<Type, IController> _controllers = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static async void Initialize()
        {
            var controllers = Creator.CreateInstancesOfType<IController>(typeof(MonoBehaviour)).ToList();
            controllers.AddRange(Creator.GetMonoControllers<IController>());

            Debug.Log("Initializing controller");

            foreach (var controller in controllers)
            {
                Debug.Log($"<color=yellow>Initializing {controller.GetType().Name}</color>");
                await controller.Initialize();
                _controllers.Add(controller.GetType(), controller);
                Debug.Log($"<color=green>Initialized {controller.GetType().Name}</color>");
            }

            Debug.Log("<color=green>All controllers are initialized</color>");
            ControllersInitialized?.Invoke();
            InitializationCompleted?.TrySetResult();
        }

        public static T GetController<T>() where T : class, IController => _controllers[typeof(T)] as T;
    }
}