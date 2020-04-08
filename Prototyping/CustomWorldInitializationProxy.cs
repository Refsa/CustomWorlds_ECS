using UnityEngine;

namespace Refsa.CustomWorld.Prototype
{
    /// <summary>
    /// The script mediates the specific deinitialization order allowing to destroy world after all scripts are disabled.
    /// It is private and hidden, but has executionOrder: 10000 in the meta file,
    /// and has to be executed after all proxy MonoBehaviours are disabled (e.g. ComponentDataProxyBase.OnDisable).
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("")]
    [DefaultExecutionOrder(10000)]
    class CustomWorldInitializationProxy : MonoBehaviour
    {
        public bool IsActive;

        public void OnEnable() 
        {
            if (!IsActive)
                return;

            IsActive = false;
            DestroyImmediate(gameObject);
        }

        public void OnDisable()
        {
            if (IsActive)
                CustomWorldInitialization.DomainUnloadOrPlayModeChangeShutdown();
        }
    }
}