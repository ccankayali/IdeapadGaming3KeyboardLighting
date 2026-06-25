using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace KeyboardLight;

public enum Effect { Static = 1, Breath = 3, Wave = 4, Hue = 6 }

public class KeyboardController : IDisposable
{
    private const int VendorId  = 0x048D;
    private const int ProductId = 0xC963;

    private IntPtr _ctx  = IntPtr.Zero;
    private IntPtr _dev  = IntPtr.Zero;

    // libusb P/Invoke
    [DllImport("libusb-1.0", EntryPoint = "libusb_init")]
    private static extern int libusb_init(ref IntPtr ctx);

    [DllImport("libusb-1.0", EntryPoint = "libusb_exit")]
    private static extern void libusb_exit(IntPtr ctx);

    [DllImport("libusb-1.0", EntryPoint = "libusb_open_device_with_vid_pid")]
    private static extern IntPtr libusb_open_device_with_vid_pid(IntPtr ctx, ushort vid, ushort pid);

    [DllImport("libusb-1.0", EntryPoint = "libusb_close")]
    private static extern void libusb_close(IntPtr dev);

    [DllImport("libusb-1.0", EntryPoint = "libusb_detach_kernel_driver")]
    private static extern int libusb_detach_kernel_driver(IntPtr dev, int iface);

    [DllImport("libusb-1.0", EntryPoint = "libusb_claim_interface")]
    private static extern int libusb_claim_interface(IntPtr dev, int iface);

    [DllImport("libusb-1.0", EntryPoint = "libusb_release_interface")]
    private static extern int libusb_release_interface(IntPtr dev, int iface);

    [DllImport("libusb-1.0", EntryPoint = "libusb_control_transfer")]
    private static extern int libusb_control_transfer(
        IntPtr dev, byte requestType, byte request,
        ushort value, ushort index,
        byte[] data, ushort length, uint timeout);

    public bool Connect()
    {
        if (libusb_init(ref _ctx) < 0) return false;
        _dev = libusb_open_device_with_vid_pid(_ctx, VendorId, ProductId);
        if (_dev == IntPtr.Zero) return false;
        libusb_detach_kernel_driver(_dev, 0);
        libusb_claim_interface(_dev, 0);
        return true;
    }

    public void SendStatic((byte R, byte G, byte B)[] colors, int brightness = 1)
        => Send(BuildPayload(Effect.Static, colors, brightness: brightness));

    public void SendBreath((byte R, byte G, byte B)[] colors, int speed = 1, int brightness = 1)
        => Send(BuildPayload(Effect.Breath, colors, speed, brightness));

    public void SendWave(int speed = 1, bool rtl = false)
        => Send(BuildPayload(Effect.Wave, null, speed, waveRtl: rtl));

    public void SendHue(int speed = 1)
        => Send(BuildPayload(Effect.Hue, null, speed));

    public void SendOff()
    {
        var data = new byte[32];
        data[0] = 0xCC; data[1] = 0x16; data[2] = 0x01;
        Send(data);
    }

    private byte[] BuildPayload(
        Effect effect,
        (byte R, byte G, byte B)[]? colors,
        int speed = 1, int brightness = 1, bool waveRtl = false)
    {
        var d = new List<byte> { 0xCC, 0x16, (byte)effect, (byte)speed, (byte)brightness };

        if (effect == Effect.Static || effect == Effect.Breath)
        {
            for (int i = 0; i < 4; i++)
            {
                var c = (colors != null && colors.Length > 0)
                    ? colors[Math.Min(i, colors.Length - 1)]
                    : (R: (byte)0, G: (byte)0, B: (byte)0);
                d.Add(c.R); d.Add(c.G); d.Add(c.B);
            }
        }
        else { for (int i = 0; i < 12; i++) d.Add(0); }

        d.Add(0);
        d.Add(waveRtl ? (byte)1 : (byte)0);
        d.Add(waveRtl ? (byte)0 : (byte)1);
        for (int i = 0; i < 13; i++) d.Add(0);
        return d.ToArray();
    }

    private void Send(byte[] data)
    {
        int r = libusb_control_transfer(_dev, 0x21, 0x09, 0x03CC, 0x00, data, (ushort)data.Length, 1000);
        if (r < 0) throw new Exception($"USB transfer failed: {r}");
    }

    public void Dispose()
    {
        if (_dev != IntPtr.Zero) { libusb_release_interface(_dev, 0); libusb_close(_dev); }
        if (_ctx != IntPtr.Zero) libusb_exit(_ctx);
    }
}
