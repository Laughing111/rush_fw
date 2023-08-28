using System;
using UnityEngine;

namespace Game.Runtime
{
    public class SingletonMono<T> : SingletonMonoBase where T :SingletonMonoBase
    {
        private static T ins;
        public static T Instance
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return null;
                }
#endif
                
                if (ins == null)
                {
                    ins = FindObjectOfType(typeof(T),true) as T;
                    if (ins == null)
                    {
                        var gameObj = new GameObject(typeof(T).Name);
                        ins = gameObj.AddComponent<T>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(ins);
                        }

                        ins.Init();
                    }
                }
                
                return ins;
            }
        }

        
    }

    public class SingletonMonoBase : MonoBehaviour
    {

        public virtual void Init()
        {
            
        }

        public virtual void Unload()
        {
            this.OnUnload();
        }

        protected virtual void OnUnload() {}
    }
}

