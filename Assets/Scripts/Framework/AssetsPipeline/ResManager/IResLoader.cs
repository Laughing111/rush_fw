using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Runtime
{
    public interface IResLoader
    {
        void Init();

        void PreLoadObj(string path, int count = 1);

        void PreLoadRes<T>(string path) where T : UnityEngine.Object;

        GameObject Instantiate(string path, Transform parent, Vector3 localPos, Vector3 localScale, Quaternion rotation, uint crc = 0);
        
        void InstantiateAsync(string path,Action<GameObject,Object,Object> onCompleted,Object param1 = null,Object param2 = null);
        
        long InstantiateAndLoad(string path, Action<GameObject, Object, Object> onInstantiateCompleted,Action onLoading, Object param1 = null, Object param2 = null);
        
        void RemoveObjectLoadCallBack(long loadId);

        void Release(GameObject obj, bool destroy = false);

        void Release(Texture texture);

        Sprite LoadSprite(string path);

        Texture LoadTexture(string path);

        TextAsset LoadTextAsset(string path);

        Sprite LoadAtlasSprite(string atlasPath, string spriteName);

        long LoadTextureAsync(string path, Action<Texture, Object> onLoadCompleted, Object param1);

        long LoadSpriteAsync(string path, Image image, bool setNativeSize = false, Action<Sprite> onLoadCompleted = null);

        void ClearAllAsyncLoadTask();

        void ClearResourcesAssets(bool absoluteClean);//是否深度清理
    }
}

