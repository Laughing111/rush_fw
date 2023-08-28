using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    public abstract class BaseDecompress
    {
        
        /// <summary>
        /// 需要解压的总大小
        /// </summary>
        public float TotalSizeM { get; protected set; }
        
        /// <summary>
        /// 已经解压的大小
        /// </summary>
        public float AlreadyDecompressSizeM { get; protected set; }

        /// <summary>
        /// 是否开始解压
        /// </summary>
        public bool IsStartDecompress
        {
            get;
            protected set;
        }

        /// <summary>
        /// 开始解压内嵌资源补丁
        /// </summary>
        /// <returns></returns>
        public abstract BaseDecompress StartDecompressBuiltinPatch(string moduleName,Action onStart);
        
        /// <summary>
        /// 获取解压进度
        /// </summary>
        /// <returns></returns>
        public abstract float GetDecompressProgress();
    }
}

