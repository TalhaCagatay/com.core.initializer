using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.core.initializer
{
    public static class Creator
    {
        /// <summary>
        /// Creates Instances Of Every Type Which Inherits From T Interface
        /// </summary>
        /// <param name="exceptTypes">Types To Skip Creating Instances Of</param>
        /// <typeparam name="T">Interface Type To Create</typeparam>
        /// <returns></returns>
        public static IEnumerable<T> CreateInstancesOfType<T>(params Type[] exceptTypes)
        {
            var interfaceType = typeof(T);

            var result = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where
                (
                 x => interfaceType.IsAssignableFrom(x) &&
                      !x.IsInterface                    &&
                      !x.IsAbstract                     &&
                      exceptTypes.All(type => !x.IsSubclassOf(type) && x != type)
                ).Select(Activator.CreateInstance);

            return result.Cast<T>();
        }
        
        public static IEnumerable<T> GetMonoControllers<T>() => UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<T>(); 
    }
}