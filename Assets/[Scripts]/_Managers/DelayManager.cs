using UnityEngine;
using UnityEngine.Events;

namespace GAITemplate
{
    public class DelayManager : MonoBehaviour
    {
        #region Singleton
        public static DelayManager instance = null;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                DestroyImmediate(this);
            }
        }

        #endregion

        private UnityAction onCompleteWithDuration;
        private UnityAction onCompleteWithFrameCount;
        private float duration;
        private int frameCount;

        public void Delay(float _duration, UnityAction _onComplete) => DelayWithDuration(_duration, _onComplete);
        public void Delay(int _frameCount, UnityAction _onComplete) => DelayWithFrameCount(_frameCount, _onComplete);

        public void DelayWithDuration(float _duration, UnityAction _onComplete)
        {
            duration = _duration;
            onCompleteWithDuration = _onComplete;
        }

        public void DelayWithFrameCount(int _frameCount, UnityAction _onComplete)
        {
            frameCount = _frameCount;
            onCompleteWithFrameCount = _onComplete;
        }

        public void CancelDuration()
        {
            onCompleteWithDuration = null;
            duration = 0f;
        }

        public void CancelFrameCount()
        {
            onCompleteWithFrameCount = null;
            frameCount = 0;
        }

        public void CancelAll()
        {
            onCompleteWithDuration = null;
            onCompleteWithFrameCount = null;
            duration = 0f;
            frameCount = 0;
        }

        private void Update()
        {
            if (onCompleteWithDuration != null)
            {
                duration -= Time.deltaTime;
                if (duration <= 0f) OnDurationEnd();
            }

            if (onCompleteWithFrameCount != null)
            {
                frameCount--;
                if (frameCount <= 0) OnFrameCountEnd();
            }
        }

        private void OnDurationEnd()
        {
            onCompleteWithDuration?.Invoke();
            onCompleteWithDuration = null;
            duration = 0f;
        }

        private void OnFrameCountEnd()
        {
            onCompleteWithFrameCount?.Invoke();
            onCompleteWithFrameCount = null;
            frameCount = 0;
        }
    }
}