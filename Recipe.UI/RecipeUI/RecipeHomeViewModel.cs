
using Framework.Core.Common;
using Recipe.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Recipe.UI.RecipeUI
{
    public class RecipeHomeViewModel : BaseViewModel, INavigationAware
    {
        private readonly IRecipeService _recipeService;
        private readonly IDialogService _dialogService;

        private DelegateCommand _renameCmd = null!;
        public DelegateCommand RenameCmd => _renameCmd ?? new DelegateCommand(() =>
        {
            var result= _dialogService.ShowDialogAsync(nameof(RenameRecipeDialogView));

            if (result.Result.Result == ButtonResult.OK)
            {
                // 用户点了确认
                //var name = result.Parameters.GetValue<string>("UserName");
                // 使用返回的数据...
            }
            else if (result.Result.Result == ButtonResult.Cancel)
            {
                // 用户点了取消
            }
        
        });


        public RecipeHomeViewModel(IEventAggregator eventAggregator, IRecipeService recipeService, IDialogService dialogService) : base(eventAggregator)
        {
            _recipeService = recipeService;
            _dialogService = dialogService;
        }

        private ObservableCollection<Recipe.Domain.Recipe> recipes;

        public ObservableCollection<Recipe.Domain.Recipe> Recipes
        {
            get =>recipes;
            set => SetProperty(ref recipes, value);
        }

        private ObservableCollection<string> _groupNames;


        public ObservableCollection<string> GroupNames
        {
            get => _groupNames;
            set => SetProperty(ref _groupNames, value);
        }

        public ObservableCollection<string> RecipeNames
        {
            get => _recipeNames;
            set => SetProperty(ref _recipeNames, value);
        }

        public Dictionary<string, string> RecipeSelectCache = new Dictionary<string, string>();

        private string _selectGroupName ;
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


                RecipeNames = new ObservableCollection<string>(_recipeService.GetListAsync(p => p.GroupName == value && p.MachineName == AppGlobals.MachineName && p.RecipeName != string.Empty).Result.Select(p => p.RecipeName));

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
                RaisePropertyChanged(nameof(SelectGroupName));
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
                RaisePropertyChanged(nameof(SelectRecipeName));
            }
        }

        public int RecipeShowCount
        {
            get => _recipeShowCount;
            set => SetProperty(ref _recipeShowCount, value);
        }

        public int RecipeCount { get; set; }




        /// <summary>
        /// 刷新显示列表
        /// </summary>
        public void RefreshSelectGroupName()
        {
            GroupNames = new ObservableCollection<string>(_recipeService.GetListAsync(p => p.MachineName == AppGlobals.MachineName).Result.Select(p => p.GroupName).Distinct());
            RecipeNames = new ObservableCollection<string> (_recipeService.GetListAsync(p => p.MachineName == AppGlobals.MachineName && p.RecipeName != string.Empty).Result.Select(p => p.RecipeName));
            RecipeShowCount = RecipeNames.Count;

            if (SelectGroupName == null)
            {
                SelectGroupName = GroupNames.FirstOrDefault();
            }
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

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            RefreshSelectGroupName();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
           return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
           
        }
    }
}
