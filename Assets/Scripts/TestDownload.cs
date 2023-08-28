using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    public class TestDownload : MonoBehaviour
    {

        public Text content;
        public Action updateCb;
        public Action quitCb;
        
        

        public void InitView(string contentText,Action onUpdate,Action onQuit)
        {
            content.text = contentText;
            updateCb = onUpdate;
            quitCb = onQuit;
        }

        public void OnUpdateButtonClick()
        {
            updateCb?.Invoke();
            Destroy(this.gameObject);
        }

        public void OnQuitClick()
        {
            quitCb?.Invoke();
            Destroy(this.gameObject);
        }


        // Start is called before the first frame update
        // void Start()
        // {
        //     var resMgr = ResManager.Instance;
        //     resMgr.InitHotAssetManager();
        //     resMgr.HotUpdateAsset("AAA",
        //         name => { Debug.Log($"开始下载！{name}"); },
        //         name => { Debug.Log($"{name}下载完成！"); },
        //         name => { Debug.Log($"{name}等待下载！"); },
        //         true
        //     );
        // }
    }
}