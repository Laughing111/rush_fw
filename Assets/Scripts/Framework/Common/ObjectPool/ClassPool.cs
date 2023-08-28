using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    public interface IReusableClass
    {
        //最大保留数量
        uint MaxStore { get; }
        //重置
        void ReSet();
    }
    
    /// <summary>
    /// 对象池
    /// </summary>
    public class ClassPool
    {
        private static Dictionary<Type, Stack<IReusableClass>> reusablePool = new ();
    
        public static T Get<T>() where T : class, IReusableClass, new()
        {
            Type type = typeof(T);
            if(reusablePool.TryGetValue(type,out Stack<IReusableClass> classPool))
            {
                if (classPool.Count > 0)
                {
                    return classPool.Pop() as T;
                }
            }
            return new T();
        }

        public static void Put<T>(T data) where T : class, IReusableClass, new()
        {
            if (data == null) return;
            Type type = typeof(T);
            if (!reusablePool.TryGetValue(type, out Stack<IReusableClass> classPool))
            {
                classPool = new Stack<IReusableClass>();
                reusablePool[type] = classPool;
            }
            if(classPool.Count < data.MaxStore)
            {
                data.ReSet();
                classPool.Push(data);
            }
        }

        public void Reset()
        {
            reusablePool.Clear();
        }
    
#if UNITY_EDITOR
        public static int GetPoolCacheCount<T>()where T : class, IReusableClass
        {
            Type type = typeof(T);
            if(reusablePool.TryGetValue(type,out var classPool))
            {
                return classPool.Count;
            }
            return 0;
        }
#endif
    }
}

