using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Core.Common
{
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

        public static string LogDbFilePathNoDebug
        {
            get
            {
                return "logs\\SerilogHistory.db";
            }
        }

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

        public static string RecipeDbFliePath { 
            get
            {
                return $"Data Source={Path.Combine(AppDebug, RecipeDbFilePathNoDebug)}";
            }
        }

        public static string AppDebug { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        private static string DeviceDbFilePathNoDebug
        {
            get
            {
                return "Device\\DeviceDomain.db";
            }
        }

        public static string DeviceDbFliePath
        {
            get
            {
                return $"Data Source={Path.Combine(AppDebug, DeviceDbFilePathNoDebug)}";
            }
        }

        public static string MachineName = "ZS1A";

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
