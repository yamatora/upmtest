using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
//using UniHumanoid;
using UnityEngine;
using UnityEngine.UIElements;

public static class Clipboard {
    #region DLL
    [DllImport("libscrcam")] private static extern bool hasClipboardImage();
    [DllImport("libscrcam")] private static extern void getClipboardImageSize(ref int width, ref int height, ref int bitsPerPixel);
    [DllImport("libscrcam")] private static extern bool getClipboardImage(IntPtr buffer);
    [DllImport("libscrcam")] private static extern bool setClipboardImage(IntPtr data, int width, int height, int bitsPerPixel = 32);
    #endregion

    public static bool GetClipboardImage(out Texture2D tex) {
        //  Get Info
        int width = 0, height = 0, bitsPerPixel = 0;
        getClipboardImageSize(ref width, ref height, ref bitsPerPixel);

        //  Check size
        if (width * height < 1) {
            Debug.Log(string.Format("{0}, {1}, {2}", width, height, bitsPerPixel));
            throw new Exception("[Clipboard] Invalid image size");
        }
        int ch = bitsPerPixel / 8;
        if (ch != 3 && ch != 4) {
            Debug.Log(ch);
            throw new Exception("[Clipboard] Invalid image ch size");
        }

        //  Get Image
        byte[] buffer = new byte[width * height * ch];
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        IntPtr pBuffer = handle.AddrOfPinnedObject();
        bool success = getClipboardImage(pBuffer);
        handle.Free();

        //  Create texture
        if (success) {
            tex = new Texture2D(width, height, TextureFormat.BGRA32, false);
            if (ch == 4) {
                tex.LoadRawTextureData(buffer);
            } else if (ch == 3) {
                Color32[] cols = new Color32[width * height];
                for (int i = 0; i < cols.Length; i++) {
                    cols[i].b = buffer[ch * i + 0];
                    cols[i].g = buffer[ch * i + 1];
                    cols[i].r = buffer[ch * i + 2];
                    cols[i].a = 0xff;
                }
                tex.SetPixels32(cols);
                cols = null;
            }
            tex.Apply();
        } else {
            tex = null;
        }
        return success;
    }
    public static bool SetClipboardImage(Texture2D tex) {
        //  Prepare Data
        byte[] btex = tex.GetRawTextureData();
        for (int i = 0; i < btex.Length; i += 4) {
            //  RGBA -> BGRA
            byte tmp = btex[i + 0];
            btex[i + 0] = btex[i + 2];
            btex[i + 2] = tmp;
        }
        //  Set Data
        IntPtr pData = Marshal.AllocHGlobal(btex.Length);
        Marshal.Copy(btex, 0, pData, btex.Length);
        return setClipboardImage(pData, tex.width, tex.height, 32);
    }
