
using Framework.Core.Common;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Recipe.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
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
            if (string.IsNullOrWhiteSpace(SelectRecipeName))
            {
                return; // 没有选中配方，不弹框
            }

            // 将当前选中的配方名传入弹框
            var parameters = new DialogParameters
            {
                { "RecipeName", SelectRecipeName }
            };

            var task = _dialogService.ShowDialogAsync(nameof(RenameRecipeDialogView), parameters);
            var dialogResult = task.Result; // 同步等待弹框关闭

            if (dialogResult.Result == ButtonResult.OK)
            {
                // 用户点了确认 — 获取新配方名并执行重命名
                var newRecipeName = dialogResult.Parameters.GetValue<string>("NewRecipeName");
                if (!string.IsNullOrWhiteSpace(newRecipeName) && newRecipeName != SelectRecipeName)
                {
                    // TODO: 调用 _recipeService 执行重命名逻辑
     

                    _recipeService.UpdateRangeAsync(p => p.GroupName == SelectGroupName && p.RecipeName == SelectRecipeName, recipe => recipe.RecipeName = newRecipeName);
                    // _recipeService.RenameAsync(SelectGroupName, SelectRecipeName, newRecipeName);
                    RefreshSelectGroupName(); // 刷新列表
                }
            }
            // Cancel 则什么都不做
        });


        public RecipeHomeViewModel(IEventAggregator eventAggregator, IRecipeService recipeService, IDialogService dialogService) : base(eventAggregator)
        {
            _recipeService = recipeService;
            _dialogService = dialogService;
           
        }

        private ObservableCollection<Recipe.Domain.Recipe> recipes = new ObservableCollection<Recipe.Domain.Recipe>();

        /// <summary>
        /// 配方数据源   
        /// </summary>
        public ObservableCollection<Recipe.Domain.Recipe> Recipes
        {
            get =>recipes;
            set => SetProperty(ref recipes, value);
        }
        private ObservableCollection<string> _recipeWithGroupName = new ObservableCollection<string>();
        public ObservableCollection<string> RecipeWithGroupName 
        {
            get => _recipeWithGroupName;
            set => SetProperty(ref _recipeWithGroupName, value);
        }

        private ObservableCollection<string> _recipeWithRecipeName =new ObservableCollection<string>();
        public ObservableCollection<string> RecipeWithRecipeName
        {
            get => _recipeWithRecipeName;
            set => SetProperty(ref _recipeWithRecipeName, value);
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


                if (!RecipeSelectCache.ContainsKey(value))
                {
                    RecipeSelectCache.Add(value, string.Empty);
                }

                SelectRecipeName = RecipeSelectCache[SelectGroupName];
                if (SelectRecipeName == string.Empty)
                {
                    SelectRecipeName = RecipeWithRecipeName.FirstOrDefault();
                }
                RefreshSelectGroupName();
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

        private int _recipeCount;
        public int RecipeCount 
        {
            get {  return _recipeCount; }
            set {  SetProperty(ref _recipeCount, value);}
        }




        /// <summary>
        /// 刷新显示列表
        /// </summary>
        public void RefreshSelectGroupName()
        {
            Recipes = new ObservableCollection<Recipe.Domain.Recipe>(_recipeService.GetListAsync(p => p.MachineName == AppGlobals.MachineName).Result);
            RecipeWithGroupName = new ObservableCollection<string>(Recipes.Select(p => p.GroupName).Distinct().ToList());
            RecipeCount = Recipes.Count;
            if (SelectGroupName == null)
            {
                SelectGroupName = RecipeWithGroupName.FirstOrDefault();
            }
            if (SelectGroupName != null && !RecipeSelectCache.ContainsKey(SelectGroupName))
            {
                RecipeSelectCache.Add(SelectGroupName, string.Empty);
            }
            if (SelectGroupName != null) SelectRecipeName = RecipeSelectCache[SelectGroupName];


            RecipeWithRecipeName = new ObservableCollection<string>(Recipes.Where(P => P.GroupName == SelectGroupName).Select(p => p.RecipeName));
            RecipeShowCount = RecipeWithRecipeName.Count;
            if (SelectRecipeName == string.Empty)
            {
                SelectRecipeName = RecipeWithRecipeName.FirstOrDefault();
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
