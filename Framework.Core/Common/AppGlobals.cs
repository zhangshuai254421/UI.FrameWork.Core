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
        /// <summary>日志数据库文件绝对路径：应用基目录拼上相对路径。</summary>
        private static string LogDbConnectionStr
        {
            get
            {
                return Path.Combine(AppBaseDirectory, LogDbRelativePath);
            }
        }

        /// <summary>日志数据库相对应用基目录的文件路径。</summary>
        public static string LogDbRelativePath
        {
            get
            {
                return "logs\\SerilogHistory.db";
            }
        }

        /// <summary>配方数据库相对应用基目录的文件路径。</summary>
        private static string RecipeDbRelativePath
        {
            get
            {
                return "Recipe\\RecipeDomain.db";
            }
        }

        /// <summary>日志数据库连接字符串（Data Source 形式）。</summary>
        public static string LogDbFilePath
        {
            get
            {
                return $"Data Source={LogDbConnectionStr}";
            }
        }

        /// <summary>配方数据库连接字符串（Data Source 形式）。</summary>
        public static string RecipeDbFilePath { 
            get
            {
                return $"Data Source={Path.Combine(AppBaseDirectory, RecipeDbRelativePath)}";
            }
        }

        /// <summary>应用基目录，默认为当前应用域基目录；可重定向便于调试。</summary>
        public static string AppBaseDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>设备数据库相对应用基目录的文件路径。</summary>
        private static string DeviceDbRelativePath
        {
            get
            {
                return "Device\\DeviceDomain.db";
            }
        }

        /// <summary>设备数据库连接字符串（Data Source 形式）。</summary>
        public static string DeviceDbFilePath
        {
            get
            {
                return $"Data Source={Path.Combine(AppBaseDirectory, DeviceDbRelativePath)}";
            }
        }

        /// <summary>报警数据库相对应用基目录的文件路径。</summary>
        private static string AlarmDbRelativePath
        {
            get
            {
                return "Alarm\\AlarmDomain.db";
            }
        }

        /// <summary>报警数据库连接字符串（Data Source 形式）。</summary>
        public static string AlarmDbFilePath
        {
            get
            {
                return $"Data Source={Path.Combine(AppBaseDirectory, AlarmDbRelativePath)}";
            }
        }

        /// <summary>机台名称，默认 ZS1A。</summary>
        public static string MachineName = "ZS1A";

        /// <summary>目标检测模型相对应用基目录的文件路径。</summary>
        private static string YoloModelRelativePath
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
                return Path.Combine(AppBaseDirectory, YoloModelRelativePath);
            }
        }

        /// <summary>姿态估计模型相对应用基目录的文件路径。</summary>
        private static string YoloPoseModelRelativePath
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
                return Path.Combine(AppBaseDirectory, YoloPoseModelRelativePath);
            }
        }

        /// <summary>实例分割模型相对应用基目录的文件路径。</summary>
        private static string YoloSegmentModelRelativePath
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
                return Path.Combine(AppBaseDirectory, YoloSegmentModelRelativePath);
            }
        }

        /// <summary>图像分类模型相对应用基目录的文件路径。</summary>
        private static string YoloClassifyModelRelativePath
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
                return Path.Combine(AppBaseDirectory, YoloClassifyModelRelativePath);
            }
        }

        /// <summary>旋转框检测模型相对应用基目录的文件路径。</summary>
        private static string YoloObbModelRelativePath
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
                return Path.Combine(AppBaseDirectory, YoloObbModelRelativePath);
            }
        }
    }

}
