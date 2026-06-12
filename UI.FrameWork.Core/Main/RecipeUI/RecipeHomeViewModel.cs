using Recipe.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UI.FrameWork.Core.Main.View;

namespace UI.FrameWork.Core.Main.RecipeUI
{
    public class RecipeHomeViewModel : BaseViewModel
    {
        public RecipeHomeViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }


        private ObservableCollection<Recipe> recipes;

        public ObservableCollection<Recipe> Recipes
        {
            get { return recipes; }
            set
            {
                recipes = value;
                OnPropertyChanged("Recipes");
            }
        }

        private ObservableCollection<string> _groupNames;


        public ObservableCollection<string> GroupNames
        {
            get { return _groupNames; }
            set
            {
                _groupNames = value;
                OnPropertyChanged("GroupNames");
            }
        }

        public ObservableCollection<string> RecipeNames
        {
            get => _recipeNames;
            set
            {
                if (Equals(value, _recipeNames)) return;
                _recipeNames = value;
                OnPropertyChanged(nameof(RecipeNames));
            }
        }

        public Dictionary<string, string> RecipeSelectCache = new Dictionary<string, string>();

        private string _selectGroupName;
        private ObservableCollection<string> _recipeNames;
        private string _selectRecipeName;
        private int _recipeShowCount;

        /// <summary>
        /// 选择分组
        /// </summary>
        public string SelectGroupName
        {
            get => _selectGroupName;
            set
            {
                if (value == _selectGroupName) return;
                _selectGroupName = value;

                RecipeNames = new ObservableCollection<string>(RecipeService.GetAll().Where(p => p.MachineName == "FSD1A" && p.GroupName == value && p.RecipeName != string.Empty)
                    .Select(p => p.RecipeName));
                RecipeShowCount = RecipeNames.Count;

                if (!RecipeSelectCache.ContainsKey(value))
                {
                    RecipeSelectCache.Add(value, string.Empty);
                }

                SelectRecipeName = RecipeSelectCache[SelectGroupName];
                if (SelectRecipeName == string.Empty)
                {
                    SelectRecipeName = RecipeNames.FirstOrDefault();
                }
                OnPropertyChanged(nameof(SelectGroupName));
            }
        }

        /// <summary>
        /// 选择配方
        /// </summary>
        public string SelectRecipeName
        {
            get => _selectRecipeName;
            set
            {
                if (value == _selectRecipeName) return;
                _selectRecipeName = value;

                if (RecipeSelectCache.ContainsKey(SelectGroupName) && value != null)
                {
                    RecipeSelectCache[SelectGroupName] = value;
                }
                OnPropertyChanged(nameof(SelectRecipeName));
            }
        }

        public int RecipeShowCount
        {
            get => _recipeShowCount;
            set
            {
                if (value == _recipeShowCount) return;
                _recipeShowCount = value;
                OnPropertyChanged(nameof(RecipeShowCount));
            }
        }

        public int RecipeCount { get; set; }

        public Grpc.Client.Shared.Implement.Greeter.GreeterClient GreeterClient { get; set; }
        public RecipeService RecipeService { get; set; }
        public RecipeHomeViewModel()
        {
            if (ProjectStaticResources.IsDemo)
            {
                return;
            }

            IPAddress ipAddress = IPAddress.Loopback;
            GreeterClient = new global::Grpc.Client.Shared.Implement.Greeter.GreeterClient(GrpcClientHandler
               .GetInstance(ipAddress, 5001).Channel);
            RecipeService = new RecipeService(GreeterClient, "FSD1A");
            GroupNames = new ObservableCollection<string>(RecipeService.GetAll().Where(p => p.MachineName == "FSD1A").Select(p => p.GroupName).Distinct());

            RecipeCount = RecipeService.GetAll().Count(p => p.MachineName == "FSD1A" && p.RecipeName != string.Empty);
        }

        /// <summary>
        /// 刷新显示列表
        /// </summary>
        public void RefreshSelectGroupName()
        {
            GroupNames = new ObservableCollection<string>(RecipeService.GetAll().Where(p => p.MachineName == "FSD1A").Select(p => p.GroupName).Distinct());
            RecipeNames = new ObservableCollection<string>(RecipeService.GetAll().Where(p => p.MachineName == "FSD1A" && p.GroupName == SelectGroupName && p.RecipeName != string.Empty)
                .Select(p => p.RecipeName));
            RecipeShowCount = RecipeNames.Count;

            if (SelectGroupName != null && !RecipeSelectCache.ContainsKey(SelectGroupName))
            {
                RecipeSelectCache.Add(SelectGroupName, string.Empty);
            }

            if (SelectGroupName != null) SelectRecipeName = RecipeSelectCache[SelectGroupName];
            if (SelectRecipeName == string.Empty)
            {
                SelectRecipeName = RecipeNames.FirstOrDefault();
            }
        }

    }
}
