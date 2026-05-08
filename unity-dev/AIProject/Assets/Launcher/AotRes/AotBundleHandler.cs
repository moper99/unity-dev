using UnityEngine;

namespace Launcher
{
    public class AotBundleHandler
    {
        public AssetBundle bundle;
        public int refCount = 0;

        public AotBundleHandler(AssetBundle bun)
        {
            bundle = bun;
        }
    }
}