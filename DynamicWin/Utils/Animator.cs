namespace DynamicWin.Utils
{
    internal class Animator
    {
        public Action<float> onAnimationUpdate;

        public Action onAnimationStart;
        public Action onAnimationEnd;
        public Action onAnimationInterrupt;

        public int animationDuration;
        public int animationInterval;

        private System.Timers.Timer animationTimer;

        private bool isRunning;
        public bool IsRunning { get { return isRunning; } }

        public Animator(int animationDuration, int animationInterval = 1) 
        {
            this.animationDuration = animationDuration;
            this.animationInterval = animationInterval;
        }

        public void Interrupt()
        {
            onAnimationInterrupt?.Invoke();
            Stop();
        }

        public void Start()
        {
            if (animationTimer != null)
            {
                Stop(false);

                animationTimer = null;
            }

            isRunning = true;

            animationTimer = new System.Timers.Timer(animationInterval); // Tick every millisecond
            float elapsed = 0;

            animationTimer.Elapsed += (sender, e) =>
            {
                if (animationTimer == null) return;
                elapsed += (float)animationTimer.Interval;

                if (elapsed >= animationDuration)
                {
                    Stop();

                    return;
                }

                float progress = elapsed / (float)animationDuration;

                onAnimationUpdate?.Invoke(progress);
            };

            if(animationTimer != null)
                animationTimer.Start();
        }

        public void Stop(bool trigggerStopEvent = true)
        {
            isRunning = false;

            if(trigggerStopEvent)
                onAnimationEnd?.Invoke();

            if (animationTimer != null)
            {
                animationTimer.Stop();
                animationTimer = null;
            }
        }
    }
}
