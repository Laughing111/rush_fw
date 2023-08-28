using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    public class TestRoot : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            HotUpdateManager.Instance.HotUpdateAndUnpackBundle("AAA");
        }

    }
}

