namespace VATyakov
{
    // §1.3 on the CPU: f = (t − t0)·fps_eff·speed + offset. The driver writes Frame(t) into _VatFrame every frame.
    public sealed class VatPlayback
    {
        double _t0;
        double _offset;

        public VatPlayback(VatClip clip, double time, float speed = 1f, double offset = 0.0)
        {
            Clip = clip;
            _t0 = time;
            Speed = speed;
            _offset = offset;
        }

        public VatClip Clip { get; }

        // 0 is a pause, negative plays backwards: a loop wraps, a one-shot stops on frame 0.
        public float Speed { get; private set; }

        public double Position(double time) => (time - _t0) * Clip.FrameRate * Speed + _offset;

        public UnityEngine.Vector4 Frame(double time) => Clip.Frame(Position(time));

        // The anchor moves to `time`, so the frame shown right now stays.
        public void SetSpeed(double time, float speed)
        {
            _offset = Clip.Wrap(Position(time));
            _t0 = time;
            Speed = speed;
        }
    }
}
