using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    public static class ListPool
    {
        private static Dictionary<Type, Stack<IList>> reusablePool = new ();

        public static List<T> Get<T>()
        {
            Type type = typeof(T);
            Stack<IList> listPool = null;
            if (reusablePool.TryGetValue(type, out listPool))
            {
                if (listPool.Count > 0)
                {
                    return listPool.Pop() as List<T>;
                }
            }
            return new List<T>();
        }

        public static void Put<T>(List<T> list,int maxNum = 5)
        {
            if (list == null) return;
            Type type = typeof(T);
            Stack<IList> listPool = null;
            if (!reusablePool.TryGetValue(type, out listPool))
            {
                listPool = new Stack<IList>();
                reusablePool[type] = listPool;
            }
            if (listPool.Count < maxNum)
            {
                list.Clear();
                listPool.Push(list);
            }
        }

        public static void Reset()
        {
            reusablePool.Clear();
        }
    }
}
