﻿#if UNITY_EDITOR
namespace Bookmark4Unity.Guid
{
    using UnityEngine;
    using System;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [System.Serializable]
    public class GuidData
    {
        public string guid; // base64 string
        public string cachedName;
        public string cachedScene;
    }


    // This call is the type used by any other code to hold a reference to an object by GUID
    // If the target object is loaded, it will be returned, otherwise, NULL will be returned
    // This always works in Game Objects, so calling code will need to use GetComponent<>
    // or other methods to track down the specific objects need by any given system

    // Ideally this would be a struct, but we need the ISerializationCallbackReciever
    [System.Serializable]
    public class GuidReference : ISerializationCallbackReceiver
    {
        // cache the referenced Game Object if we find one for performance
        private GameObject cachedReference;
        private bool isCacheSet;

        // store our GUID in a form that Unity can save
        [SerializeField]
        private byte[] serializedGuid;
        private System.Guid guid;
        public string GuidString => Convert.ToBase64String(guid.ToByteArray());

#if UNITY_EDITOR
        // decorate with some extra info in Editor so we can inform a user of what that GUID means
        [SerializeField]
        private string _cachedName;
        [SerializeField]
        private string _cachedSceneName;
        [SerializeField]
        private SceneAsset _cachedScene;
        public string CachedName => _cachedName;
        public string CachedSceneName => _cachedSceneName;
#endif

        // Set up events to let users register to cleanup their own cached references on destroy or to cache off values
        public event Action<GameObject> OnGuidAdded = delegate (GameObject go) { };
        public event Action OnGuidRemoved = delegate () { };

        // create concrete delegates to avoid boxing. 
        // When called 10,000 times, boxing would allocate ~1MB of GC Memory
        private Action<GameObject> addDelegate;
        private Action removeDelegate;

        // optimized accessor, and ideally the only code you ever call on this class
        public GameObject gameObject
        {
            get
            {
                if (isCacheSet && cachedReference != null)
                {
                    return cachedReference;
                }

                cachedReference = GuidManager.ResolveGuid(guid, addDelegate, removeDelegate);
                isCacheSet = true;
                return cachedReference;
            }

            private set { }
        }

        public GuidReference() { }

#if UNITY_EDITOR
        public GuidReference(GuidData data)
        {
            var bytes = Convert.FromBase64String(data.guid);
            guid = new System.Guid(bytes);
            _cachedName = data.cachedName;
            _cachedSceneName = data.cachedScene;
        }

        public GuidReference(GuidComponent target)
        {
            guid = target.GetGuid();
            _cachedName = target.gameObject.name;
            _cachedSceneName = target.gameObject.scene.name;
        }

        public GuidData ToData()
        {
            return new GuidData()
            {
                guid = GuidString,
                cachedName = CachedName,
                cachedScene = CachedSceneName
            };
        }
#endif

        private void GuidAdded(GameObject go)
        {
            cachedReference = go;
            OnGuidAdded(go);
        }

        private void GuidRemoved()
        {
            cachedReference = null;
            isCacheSet = false;
            OnGuidRemoved();
        }

        //convert system guid to a format unity likes to work with
        public void OnBeforeSerialize()
        {
            serializedGuid = guid.ToByteArray();
        }

        // convert from byte array to system guid and reset state
        public void OnAfterDeserialize()
        {
            cachedReference = null;
            isCacheSet = false;
            if (serializedGuid == null || serializedGuid.Length != 16)
            {
                serializedGuid = new byte[16];
            }
            guid = new System.Guid(serializedGuid);
            addDelegate = GuidAdded;
            removeDelegate = GuidRemoved;
        }
    }
}
#endif