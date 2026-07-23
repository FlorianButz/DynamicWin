using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Halo.Widgets;

internal static class AudioSpectrum
{
    public const int BandCount = 9;
    private const int Ch = 5;
    private static readonly float[] _bands = new float[BandCount];
    public static volatile bool Available;

    private static Thread? _thread;
    private static long _until;

    public static float[] Bands()
    {
        _until = Environment.TickCount64 + 5000;
        if (_thread == null)
        {
            _thread = new Thread(Loop) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
            _thread.Start();
        }
        lock (_bands) return (float[])_bands.Clone();
    }

    private const int N = 1024;
    private static readonly float[] _ringL = new float[N * 2], _ringR = new float[N * 2];
    private static int _ringPos;

    private static void Loop()
    {
        while (true)
        {
            try
            {
                if (Environment.TickCount64 > _until) { Available = false; Thread.Sleep(300); continue; }
                Capture();
            }
            catch { Available = false; }
            Thread.Sleep(500);
        }
    }

    private static void Capture()
    {
        var en = (IMMDeviceEnumerator)new MMDeviceEnumerator();
        if (en.GetDefaultAudioEndpoint(0, 1, out var dev) != 0 || dev == null) return;
        var acid = typeof(IAudioClient).GUID;
        if (dev.Activate(ref acid, 23, IntPtr.Zero, out var aco) != 0 || aco is not IAudioClient ac) return;
        if (ac.GetMixFormat(out IntPtr fmtPtr) != 0) return;
        try
        {
            int channels = Marshal.ReadInt16(fmtPtr, 2);
            int rate = Marshal.ReadInt32(fmtPtr, 4);
            int bits = Marshal.ReadInt16(fmtPtr, 14);
            if (bits != 32 || channels < 1 || rate < 8000) return;

            const uint LOOPBACK = 0x00020000;
            if (ac.Initialize(0, LOOPBACK, 2_000_000, 0, fmtPtr, IntPtr.Zero) != 0) return;
            var ccid = typeof(IAudioCaptureClient).GUID;
            if (ac.GetService(ref ccid, out var cco) != 0 || cco is not IAudioCaptureClient cc) return;
            if (ac.Start() != 0) return;
            Available = true;

            var win = Hann();
            long nextFft = 0;
            while (Environment.TickCount64 <= _until)
            {
                while (cc.GetNextPacketSize(out uint pkt) == 0 && pkt > 0)
                {
                    if (cc.GetBuffer(out IntPtr data, out uint frames, out uint flags, out _, out _) != 0) break;
                    bool silent = (flags & 2) != 0;
                    unsafe
                    {
                        float* p = (float*)data;
                        for (uint f = 0; f < frames; f++)
                        {
                            float l = 0, r = 0;
                            if (!silent)
                            {
                                l = p[f * channels];
                                r = channels > 1 ? p[f * channels + 1] : l;
                            }
                            _ringL[_ringPos] = l;
                            _ringR[_ringPos] = r;
                            _ringPos = (_ringPos + 1) % _ringL.Length;
                        }
                    }
                    cc.ReleaseBuffer(frames);
                }

                long now = Environment.TickCount64;
                if (now >= nextFft)
                {
                    nextFft = now + 25;
                    lock (_bands) ComputeBands(win, rate);
                }
                Thread.Sleep(5);
            }
            try { ac.Stop(); } catch { }
        }
        finally { Marshal.FreeCoTaskMem(fmtPtr); Available = false; }
    }

    private static float[] Hann()
    {
        var w = new float[N];
        for (int i = 0; i < N; i++) w[i] = 0.5f - 0.5f * MathF.Cos(MathF.Tau * i / (N - 1));
        return w;
    }

    private static readonly float[] _reL = new float[N], _imL = new float[N];
    private static readonly float[] _reR = new float[N], _imR = new float[N];

    private static void ComputeBands(float[] win, int rate)
    {
        LoadFft(_ringL, _reL, _imL, win);
        LoadFft(_ringR, _reR, _imR, win);

        Span<float> target = stackalloc float[BandCount];
        int nb = Ch - 1;
        double fMin = 55, fMax = Math.Min(12000, rate / 2.0 - 1);
        for (int b = 0; b < nb; b++)
        {
            double lo = fMin * Math.Pow(fMax / fMin, b / (double)nb);
            double hi = fMin * Math.Pow(fMax / fMin, (b + 1) / (double)nb);
            float tilt = b * 3.2f;
            target[nb - 1 - b] = BandValue(_reL, _imL, rate, lo, hi, tilt);
            target[BandCount - nb + b] = BandValue(_reR, _imR, rate, lo, hi, tilt);
        }

        {
            int i0 = Math.Max(1, 150 * N / rate), i1 = Math.Max(i0 + 1, 3500 * N / rate);
            double sum = 0;
            for (int i = i0; i < i1 && i < N / 2; i++)
            {
                float mr = (_reL[i] + _reR[i]) / 2f, mi = (_imL[i] + _imR[i]) / 2f;
                float sr = (_reL[i] - _reR[i]) / 2f, si = (_imL[i] - _imR[i]) / 2f;
                double p = (mr * mr + mi * mi) - (sr * sr + si * si);
                if (p > 0) sum += p;
            }
            double rms = Math.Sqrt(sum / Math.Max(1, i1 - i0)) / N;
            float db = 20f * MathF.Log10((float)rms + 1e-9f);
            target[BandCount / 2] = Math.Clamp((db + 62f) / 35f, 0f, 1f);
        }

        float max = 0f;
        for (int b = 0; b < BandCount; b++) if (b != BandCount / 2 && target[b] > max) max = target[b];
        if (max > 0.04f)
            for (int b = 0; b < BandCount; b++)
                if (b != BandCount / 2)
                    target[b] = MathF.Pow(target[b] / max, 1.6f) * (0.30f + 0.70f * max);
        if (max <= 0.04f)
            for (int b = 0; b < BandCount; b++) if (b != BandCount / 2) target[b] = 0f;

        for (int b = 0; b < BandCount; b++)
        {
            float v = target[b];

            _bands[b] = v > _bands[b] ? _bands[b] + (v - _bands[b]) * 0.75f : _bands[b] + (v - _bands[b]) * 0.28f;
        }
    }

    private static void LoadFft(float[] ring, float[] re, float[] im, float[] win)
    {
        int start = _ringPos;
        for (int i = 0; i < N; i++)
        {
            re[i] = ring[(start + ring.Length - N + i) % ring.Length] * win[i];
            im[i] = 0;
        }
        Fft(re, im);
    }

    private static float BandValue(float[] re, float[] im, int rate, double lo, double hi, float tiltDb)
    {
        int i0 = Math.Max(1, (int)(lo * N / rate)), i1 = Math.Max(i0 + 1, (int)(hi * N / rate));
        double sum = 0;
        for (int i = i0; i < i1 && i < N / 2; i++) sum += re[i] * re[i] + im[i] * im[i];
        double rms = Math.Sqrt(sum / Math.Max(1, i1 - i0)) / N;
        float db = 20f * MathF.Log10((float)rms + 1e-9f);
        return Math.Clamp((db + 55f + tiltDb) / 40f, 0f, 1f);
    }

    private static void Fft(float[] re, float[] im)
    {
        int n = re.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j |= bit;
            if (i < j) { (re[i], re[j]) = (re[j], re[i]); (im[i], im[j]) = (im[j], im[i]); }
        }
        for (int len = 2; len <= n; len <<= 1)
        {
            float ang = -MathF.Tau / len;
            float wr = MathF.Cos(ang), wi = MathF.Sin(ang);
            for (int i = 0; i < n; i += len)
            {
                float cr = 1, ci = 0;
                for (int k = 0; k < len / 2; k++)
                {
                    int a = i + k, b = i + k + len / 2;
                    float tr = re[b] * cr - im[b] * ci, ti = re[b] * ci + im[b] * cr;
                    re[b] = re[a] - tr; im[b] = im[a] - ti;
                    re[a] += tr; im[a] += ti;
                    (cr, ci) = (cr * wr - ci * wi, cr * wi + ci * wr);
                }
            }
        }
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint clsCtx, IntPtr activationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object iface);
    }

    [ComImport, Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
        [PreserveSig] int Initialize(int shareMode, uint streamFlags, long bufferDuration, long periodicity,
            IntPtr format, IntPtr audioSessionGuid);
        [PreserveSig] int GetBufferSize(out uint size);
        [PreserveSig] int GetStreamLatency(out long latency);
        [PreserveSig] int GetCurrentPadding(out uint padding);
        [PreserveSig] int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closestMatch);
        [PreserveSig] int GetMixFormat(out IntPtr format);
        [PreserveSig] int GetDevicePeriod(out long defaultPeriod, out long minPeriod);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr handle);
        [PreserveSig] int GetService(ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
    }

    [ComImport, Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioCaptureClient
    {
        [PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out uint flags, out long devPos, out long qpcPos);
        [PreserveSig] int ReleaseBuffer(uint frames);
        [PreserveSig] int GetNextPacketSize(out uint frames);
    }
}
