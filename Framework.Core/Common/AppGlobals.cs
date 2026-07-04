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

        public static string MachineName = "ZS1A";
    }

}
