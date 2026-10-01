using Framework.Device.Domain;

namespace Framework.Device.HikVision.Tests
{
    /// <summary>
    /// CameraImageFile（CameraData → BMP 落盘）与 CameraBase.SaveImage 的编码测试（不依赖任何硬件）。
    /// </summary>
    public class CameraImageFileTests
    {
        [Fact]
        public void Mono8_保存为8位调色板BMP_行序自下而上()
        {
            var path = TempPath(".bmp");
            var frame = new CameraData
            {
                ImageData = new byte[] { 10, 20, 30, 40 }, // 上行 10,20；下行 30,40
                Width = 2,
                Height = 2,
                PixelFormat = PixelFormat.Mono8,
            };

            Assert.True(CameraImageFile.Save(frame, path));

            var bmp = File.ReadAllBytes(path);
            Assert.Equal(1086, bmp.Length); // 54 头 + 1024 调色板 + 2 行 × 4 字节
            Assert.Equal((byte)'B', bmp[0]);
            Assert.Equal((byte)'M', bmp[1]);
            Assert.Equal(1086, ReadInt32(bmp, 2));  // 文件大小
            Assert.Equal(1078, ReadInt32(bmp, 10)); // 像素数据偏移
            Assert.Equal(2, ReadInt32(bmp, 18));    // 宽
            Assert.Equal(2, ReadInt32(bmp, 22));    // 高
            Assert.Equal(8, ReadUInt16(bmp, 28));   // 位深
            Assert.Equal(0, bmp[54]);               // 调色板首项为黑
            Assert.Equal(255, bmp[54 + 255 * 4]);   // 调色板末项为白
            Assert.Equal(30, bmp[1078]);            // 自下而上：先写下行
            Assert.Equal(40, bmp[1079]);
            Assert.Equal(10, bmp[1082]);            // 再写上行
            Assert.Equal(20, bmp[1083]);
        }

        [Fact]
        public void Mono16_取16位容器高8位()
        {
            var path = TempPath(".bmp");
            var frame = new CameraData
            {
                ImageData = new byte[] { 0x34, 0x12 }, // 小端 0x1234，高字节 0x12
                Width = 1,
                Height = 1,
                PixelFormat = PixelFormat.Mono16,
            };

            Assert.True(CameraImageFile.Save(frame, path));

            var bmp = File.ReadAllBytes(path);
            Assert.Equal(1082, bmp.Length); // 54 头 + 1024 调色板 + 1 行 × 4 字节
            Assert.Equal(0x12, bmp[1078]);
        }

        [Fact]
        public void RGB8_转为BGR序写入()
        {
            var path = TempPath(".bmp");
            var frame = new CameraData
            {
                ImageData = new byte[] { 200, 150, 100 }, // R,G,B
                Width = 1,
                Height = 1,
                PixelFormat = PixelFormat.RGB8,
            };

            Assert.True(CameraImageFile.Save(frame, path));

            var bmp = File.ReadAllBytes(path);
            Assert.Equal(58, bmp.Length); // 54 头 + 1 行 × 4 字节
            Assert.Equal(100, bmp[54]);   // BMP 为 BGR 序
            Assert.Equal(150, bmp[55]);
            Assert.Equal(200, bmp[56]);
        }

        [Fact]
        public void BGR8_通道序直写()
        {
            var path = TempPath(".bmp");
            var frame = new CameraData
            {
                ImageData = new byte[] { 100, 150, 200 }, // B,G,R
                Width = 1,
                Height = 1,
                PixelFormat = PixelFormat.BGR8,
            };

            Assert.True(CameraImageFile.Save(frame, path));

            var bmp = File.ReadAllBytes(path);
            Assert.Equal(100, bmp[54]);
            Assert.Equal(150, bmp[55]);
            Assert.Equal(200, bmp[56]);
        }

        [Fact]
        public void BayerRG8_双线性还原中心像素()
        {
            var path = TempPath(".bmp");
            var frame = new CameraData
            {
                // RGGB 3×3：R G R / G B G / R G R
                ImageData = new byte[] { 100, 50, 110, 60, 10, 70, 120, 55, 130 },
                Width = 3,
                Height = 3,
                PixelFormat = PixelFormat.BayerRG8,
            };

            Assert.True(CameraImageFile.Save(frame, path));

            var bmp = File.ReadAllBytes(path);
            int stride = 12; // 3 像素 × 3 字节对齐到 4 字节
            int center = 54 + stride + 3; // 源 (1,1) 为 B 像素，写入 BMP 第 1 行
            Assert.Equal(10, bmp[center]);      // B 取自身
            Assert.Equal(58, bmp[center + 1]);  // G = (50+70+60+55)/4
            Assert.Equal(115, bmp[center + 2]); // R = (100+110+120+130)/4
        }

        [Fact]
        public void 空帧_保存失败()
        {
            Assert.False(CameraImageFile.Save(new CameraData(), TempPath(".bmp")));
        }

        [Fact]
        public void 像素格式Undefined_保存失败()
        {
            var frame = new CameraData
            {
                ImageData = new byte[] { 1 },
                Width = 1,
                Height = 1,
                PixelFormat = PixelFormat.Undefined,
            };

            Assert.False(CameraImageFile.Save(frame, TempPath(".bmp")));
        }

        [Fact]
        public void 目录不存在_自动创建()
        {
            var path = Path.Combine(
                Path.GetTempPath(), "framework-device-tests", Guid.NewGuid().ToString("N"), "a.bmp");
            var frame = new CameraData
            {
                ImageData = new byte[] { 7 },
                Width = 1,
                Height = 1,
                PixelFormat = PixelFormat.Mono8,
            };

            Assert.True(CameraImageFile.Save(frame, path));
            Assert.True(File.Exists(path));
        }

        [Fact]
        public void 无扩展名_自动补bmp()
        {
            var path = Path.Combine(Path.GetTempPath(), "framework-device-tests", Guid.NewGuid().ToString("N"));
            var frame = new CameraData
            {
                ImageData = new byte[] { 7 },
                Width = 1,
                Height = 1,
                PixelFormat = PixelFormat.Mono8,
            };

            Assert.True(CameraImageFile.Save(frame, path));
            Assert.True(File.Exists(path + ".bmp"));
        }

        [Fact]
        public void CameraBase_SaveImage_取一帧并落盘()
        {
            var path = TempPath(".bmp");
            var camera = new FakeCamera();
            camera.SetNextFrame(new CameraData
            {
                ImageData = new byte[] { 1, 2, 3, 4 }, // 上行 1,2；下行 3,4
                Width = 2,
                Height = 2,
                PixelFormat = PixelFormat.Mono8,
            });

            Assert.True(camera.SaveImage(path));

            var bmp = File.ReadAllBytes(path);
            Assert.Equal(1086, bmp.Length);
            Assert.Equal(3, bmp[1078]); // 帧的下行先写
            Assert.Equal(4, bmp[1079]);
        }

        private static string TempPath(string extension) =>
            Path.Combine(Path.GetTempPath(), "framework-device-tests", Guid.NewGuid().ToString("N") + extension);

        private static int ReadInt32(byte[] b, int offset) =>
            b[offset] | b[offset + 1] << 8 | b[offset + 2] << 16 | b[offset + 3] << 24;

        private static int ReadUInt16(byte[] b, int offset) => b[offset] | b[offset + 1] << 8;
    }
}
