using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Core.Common
{
    /// <summary>
    /// 应用全局路径常量：集中存放日志库、配方库、设备库与 YOLO 各模型文件的路径，均基于应用基目录拼接。
    /// </summary>
    public static class AppGlobals
    {
        /// <summary>
        /// D:\\CodeSource\\01-UI框架\\UI.FrameWork\\bin\\Debug\\net8.0-windows\\logs\\log.db
        /// </summary>
        private static string LogDbConnectionStr
        {
            get
            {
                return Path.Combine(AppDebug, LogDbFilePathNoDebug);
            }
        }

        /// <summary>日志数据库相对应用基目录的文件路径。</summary>
        public static string LogDbFilePathNoDebug
        {
            get
            {
                return "logs\\SerilogHistory.db";
            }
        }

        /// <summary>配方数据库相对应用基目录的文件路径。</summary>
        private static string RecipeDbFilePathNoDebug
        {
            get
            {
                return "Recipe\\RecipeDomain.db";
            }
        }

        /// <summary>
        /// Data Source=D:\\CodeSource\\01-UI框架\\UI.FrameWork\\bin\\Debug\\net8.0-windows\\logs\\log.db
        /// </summary>
        public static string LogDFliePath
        {
            get
            {
                return $"Data Source={LogDbConnectionStr}";
            }
        }

        /// <summary>配方数据库连接字符串（Data Source 形式）。</summary>
        public static string RecipeDbFliePath { 
            get
            {
                return $"Data Source={Path.Combine(AppDebug, RecipeDbFilePathNoDebug)}";
            }
        }

        /// <summary>应用基目录，默认为当前应用域基目录；可重定向便于调试。</summary>
        public static string AppDebug { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>设备数据库相对应用基目录的文件路径。</summary>
        private static string DeviceDbFilePathNoDebug
        {
            get
            {
                return "Device\\DeviceDomain.db";
            }
        }

        /// <summary>设备数据库连接字符串（Data Source 形式）。</summary>
        public static string DeviceDbFliePath
        {
            get
            {
                return $"Data Source={Path.Combine(AppDebug, DeviceDbFilePathNoDebug)}";
            }
        }

        /// <summary>机台名称，默认 ZS1A。</summary>
        public static string MachineName = "ZS1A";

        /// <summary>目标检测模型相对应用基目录的文件路径。</summary>
        private static string YoloModelFilePathNoDebug
        {
            get
            {
                return "Models\\Yolo\\yolo26n.onnx";
            }
        }

        /// <summary>
        /// YOLO 目标检测模型（ONNX）路径。
        /// </summary>
        public static string YoloModelFilePath
        {
            get
            {
                return Path.Combine(AppDebug, YoloModelFilePathNoDebug);
            }
        }

        /// <summary>姿态估计模型相对应用基目录的文件路径。</summary>
        private static string YoloPoseModelFilePathNoDebug
        {
            get
            {
                return "Models\\Yolo\\yolo26n-pose.onnx";
            }
        }

        /// <summary>
        /// YOLO 姿态估计模型（ONNX）路径。
        /// </summary>
        public static string YoloPoseModelFilePath
        {
            get
            {
                return Path.Combine(AppDebug, YoloPoseModelFilePathNoDebug);
            }
        }

        /// <summary>实例分割模型相对应用基目录的文件路径。</summary>
        private static string YoloSegmentModelFilePathNoDebug
        {
            get
            {
                return "Models\\Yolo\\yolo26n-seg.onnx";
            }
        }

        /// <summary>
        /// YOLO 实例分割模型（ONNX）路径。
        /// </summary>
        public static string YoloSegmentModelFilePath
        {
            get
            {
                return Path.Combine(AppDebug, YoloSegmentModelFilePathNoDebug);
            }
        }

        /// <summary>图像分类模型相对应用基目录的文件路径。</summary>
        private static string YoloClassifyModelFilePathNoDebug
        {
            get
            {
                return "Models\\Yolo\\yolo26n-cls.onnx";
            }
        }

        /// <summary>
        /// YOLO 图像分类模型（ONNX）路径。
        /// </summary>
        public static string YoloClassifyModelFilePath
        {
            get
            {
                return Path.Combine(AppDebug, YoloClassifyModelFilePathNoDebug);
            }
        }

        /// <summary>旋转框检测模型相对应用基目录的文件路径。</summary>
        private static string YoloObbModelFilePathNoDebug
        {
            get
            {
                return "Models\\Yolo\\yolo26n-obb.onnx";
            }
        }

        /// <summary>
        /// YOLO 旋转框检测模型（ONNX）路径。
        /// </summary>
        public static string YoloObbModelFilePath
        {
            get
            {
                return Path.Combine(AppDebug, YoloObbModelFilePathNoDebug);
            }
        }
    }

}
