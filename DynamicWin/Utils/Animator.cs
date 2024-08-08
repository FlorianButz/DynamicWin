using DynamicWin.Main;
using DynamicWin.UI;
using SkiaSharp;

namespace DynamicWin.Utils
{
    public class Animator : UIObject
    {
        public Action<float> onAnimationUpdate;

        public Action onAnimationStart;
        public Action onAnimationEnd;
        public Action onAnimationInterrupt;

        public int animationDuration;
        public int animationInterval;

        private bool isRunning;
        public bool IsRunning { get { return isRunning; } }

        public Animator(int animationDuration, int animationInterval = 1) : base(null, Vec2.zero, Vec2.zero)
        {
            this.animationDuration = animationDuration;
            this.animationInterval = animationInterval;
        }

        public override void Draw(SKCanvas canvas)
        {

        }

        public void Interrupt()
        {
            onAnimationInterrupt?.Invoke();
            Stop();
        }

        public void Start()
        {
            isRunning = true;
            elapsed = 0;
        }

        float elapsed = 0;

        public override void Update(float deltaTime)
        {
            if (!isRunning) return;

            elapsed += deltaTime * 1000;

            if (elapsed >= animationDuration)
            {
                Stop();

                return;
            }

            float progress = elapsed / (float)animationDuration;

            onAnimationUpdate?.Invoke(progress);
        }

        public void Stop(bool trigggerStopEvent = true)
        {
            isRunning = false;

            if(trigggerStopEvent)
                onAnimationEnd?.Invoke();
        }
    }
}
