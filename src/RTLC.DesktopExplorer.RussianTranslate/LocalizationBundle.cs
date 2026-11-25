using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal sealed class LocalizationBundle : IDisposable
    {
        private readonly AssetBundle bundle;

        internal LocalizationBundle(string path)
        {
            bundle = AssetBundle.LoadFromFile(path);
            if (!bundle) throw new InvalidDataException("Cannot load localization bundle: " + path);
        }

        internal IEnumerable<string> Names => bundle.GetAllAssetNames();

        internal T Require<T>(string name) where T : UnityEngine.Object =>
            bundle.LoadAsset<T>(name.ToLowerInvariant()) ??
            throw new InvalidDataException("Missing " + typeof(T).Name + " in rtlc.bundle: " + name);

        internal TextAsset FindText(string name) => bundle.LoadAsset<TextAsset>(name.ToLowerInvariant());

        public void Dispose()
        {
            if (bundle) bundle.Unload(true);
        }
    }
}
