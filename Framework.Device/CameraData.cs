namespace Framework.Device
{ 
    public class CameraData
    {
        public CameraData() { }

        public  IntPtr ImageAddr { get; set; }
        public byte[] ImageData { get; set;}

        private uint nBufSize;

        public uint nFrameLen;

        public ushort nWidth;
        public ushort nHeight;
        public PixelType nPixelType;
    }
}
