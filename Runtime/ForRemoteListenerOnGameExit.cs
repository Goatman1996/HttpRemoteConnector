using System;
using UnityEngine;

namespace HttpRemoteConnector
{
    public class ForRemoteListenerOnGameExit : MonoBehaviour
    {
        public Action OnDestroyEvt;
        private void OnDestroy()
        {
            this.OnDestroyEvt?.Invoke();
        }
    }
}