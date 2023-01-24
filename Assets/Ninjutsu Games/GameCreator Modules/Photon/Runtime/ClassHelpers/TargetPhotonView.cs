namespace NJG.PUN
{
    using GameCreator.Core;
    using UnityEngine;
    using GameCreator.Core.Hooks;
    using GameCreator.Characters;
    using GameCreator.Variables;
    using Photon.Pun;

    [System.Serializable]
    public class TargetPhotonView
    {
        public enum Target
        {
            Player,
            Camera,
            Invoker,
            PhotonView,
            LocalVariable,
            GlobalVariable,
            ListVariable
        }
        // PROPERTIES: ----------------------------------------------------------------------------

        public Target target = Target.PhotonView;
        public PhotonView photonView;
        public HelperLocalVariable local = new HelperLocalVariable();
        public HelperGlobalVariable global = new HelperGlobalVariable();
        public HelperGetListVariable list = new HelperGetListVariable();

        private int cacheInstanceID;
        private PhotonView cachedView;

        // INITIALIZERS: --------------------------------------------------------------------------

        public TargetPhotonView() { }

        public TargetPhotonView(Target target)
        {
            this.target = target;
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public PhotonView GetView(GameObject invoker)
        {
            switch (this.target)
            {
                case Target.Player:
                    if (HookPlayer.Instance && HookPlayer.Instance.GetInstanceID() != this.cacheInstanceID)
                    {
                        this.cachedView = HookPlayer.Instance.Get<PhotonView>();
                        CacheInstanceID();
                    }
                    break;
                case Target.Camera:
                    if (HookCamera.Instance && HookCamera.Instance.GetInstanceID() != this.cacheInstanceID)
                    {
                        this.cachedView = HookCamera.Instance.Get<PhotonView>();
                        CacheInstanceID();
                    }
                    break;
                case Target.Invoker:
                    if (!invoker)
                    {
                        this.cachedView = null;
                        break;
                    }

                    if (!this.cachedView || invoker.GetInstanceID() != this.cacheInstanceID)
                    {
                        this.cachedView = invoker.GetComponentInChildren<PhotonView>();
                        CacheInstanceID();
                    }

                    if (!this.cachedView || invoker.GetInstanceID() != this.cacheInstanceID)
                    {
                        this.cachedView = invoker.GetComponentInParent<PhotonView>();
                        CacheInstanceID();
                    }
                    break;

                case Target.PhotonView:
                    if (this.photonView && photonView.GetInstanceID() != cacheInstanceID)
                    {
                        this.cachedView = this.photonView;
                        CacheInstanceID();
                    }
                    break;

                case Target.LocalVariable:
                    GameObject localResult = this.local.Get(invoker) as GameObject;
                    if (localResult && localResult.GetInstanceID() != this.cacheInstanceID)
                    {
                        this.cachedView = localResult.GetComponentInChildren<PhotonView>();
                        CacheInstanceID();
                    }
                    break;

                case Target.GlobalVariable:
                    GameObject globalResult = this.global.Get(invoker) as GameObject;
                    if (globalResult && globalResult.GetInstanceID() != this.cacheInstanceID)
                    {
                        this.cachedView = globalResult.GetComponentInChildren<PhotonView>();
                        CacheInstanceID();
                    }
                    break;

                case Target.ListVariable:
                    GameObject listResult = this.list.Get(invoker) as GameObject;
                    if (listResult && listResult.GetInstanceID() != this.cacheInstanceID)
                    {
                        this.cachedView = listResult.GetComponentInChildren<PhotonView>();
                        CacheInstanceID();
                    }
                    break;
            }

            return this.cachedView;
        }

        private void CacheInstanceID()
        {
            this.cacheInstanceID = (this.cachedView
                ? this.cachedView.gameObject.GetInstanceID()
                : 0
            );
        }

        // UTILITIES: -----------------------------------------------------------------------------

        public override string ToString()
        {
            string result = "(unknown)";
            switch (this.target)
            {
                case Target.Player: result = "Player"; break;
                case Target.Invoker: result = "Invoker"; break;
                case Target.Camera: result = "Camera"; break;
                case Target.PhotonView:
                    result = (this.photonView == null
                        ? "(none)"
                        : this.photonView.gameObject.name
                    );
                    break;
                case Target.LocalVariable: result = this.local.ToString(); break;
                case Target.GlobalVariable: result = this.global.ToString(); break;
                case Target.ListVariable: result = this.list.ToString(); break;
            }

            return result;
        }
    }
}