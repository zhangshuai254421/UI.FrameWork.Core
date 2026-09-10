
using Baksteen.Extensions.DeepCopy;
using EFCore.Repository;
using Framework.Core.Common;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using PrismUI.Core;
using Recipe.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Linq;

namespace Recipe.UI.RecipeUI
{
    public class RecipeHomeViewModel : BaseViewModel, INavigationAware
    {
        #region 字段

        private readonly IRecipeService _recipeService;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator EventAggregator;

        private DelegateCommand _renameCmd = null!;
        private DelegateCommand _copyRecipeCmd = null!;
        private DelegateCommand _deleteRecipeCmd = null!;
        private DelegateCommand _applyRecipeCmd = null!;

        private ObservableCollection<Recipe.Domain.Recipe> recipes = new ObservableCollection<Recipe.Domain.Recipe>();
        private ObservableCollection<string> _recipeWithGroupName = new ObservableCollection<string>();
        private ObservableCollection<string> _recipeWithRecipeName = new ObservableCollection<string>();

        private string _selectGroupName;
        private ObservableCollection<string> _recipeNames;
        private string _selectRecipeName;
        private int _recipeShowCount;
        private int _recipeCount;

        public Dictionary<string, string> RecipeSelectCache = new Dictionary<string, string>();

        #endregion

        #region 构造函数

        public RecipeHomeViewModel(IRecipeService recipeService, IDialogService dialogService) 
        {
            _recipeService = recipeService;
            _dialogService = dialogService;
        }

        #endregion

        #region 属性

        /// <summary>
        /// 配方数据源
        /// </summary>
        public ObservableCollection<Recipe.Domain.Recipe> Recipes
        {
            get => recipes;
            set => SetProperty(ref recipes, value);
        }

        public ObservableCollection<string> RecipeWithGroupName
        {
            get => _recipeWithGroupName;
            set => SetProperty(ref _recipeWithGroupName, value);
        }

        public ObservableCollection<string> RecipeWithRecipeName
        {
            get => _recipeWithRecipeName;
            set => SetProperty(ref _recipeWithRecipeName, value);
        }

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

        public int RecipeCount
        {
            get { return _recipeCount; }
            set { SetProperty(ref _recipeCount, value); }
        }

        #endregion

        #region 命令

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

        public DelegateCommand CopyRecipeCmd => _copyRecipeCmd ?? new DelegateCommand(() =>
        {
            if (string.IsNullOrWhiteSpace(SelectRecipeName))
            {
                return;
            }

            // 将当前选中的配方名传入弹框
            var parameters = new DialogParameters
            {
                { "RecipeName", SelectRecipeName }
            };

            var task = _dialogService.ShowDialogAsync(nameof(CopyRecipeDialogView), parameters);
            var dialogResult = task.Result;

            if (dialogResult.Result == ButtonResult.OK)
            {
                var newRecipeName = dialogResult.Parameters.GetValue<string>("NewRecipeName");
                if (!string.IsNullOrWhiteSpace(newRecipeName) && newRecipeName != SelectRecipeName)
                {
                    var s = _recipeService.GetListAsync(p => p.GroupName == SelectGroupName && p.RecipeName == SelectRecipeName).Result.FirstOrDefault()?.Parameters;

                    ICollection<RecipeParameter> parametersToCopy= new List<RecipeParameter>();

                    foreach (var parameter in s ?? new List<RecipeParameter>())
                    {
                        var temp = parameter.DeepCopy();
                        temp.ResetId();
                        parametersToCopy.Add(temp);
                    }

                    // TODO: 调用 _recipeService 执行拷贝逻辑
                    _recipeService.AddAsync(new Recipe.Domain.Recipe()
                    {
                        GroupName = SelectGroupName,
                        RecipeName = newRecipeName,
                        MachineName = AppGlobals.MachineName,
                        //TODO. 这里要用反射 继承的字类  但是不需要
                        Parameters = parametersToCopy
                    });
                    RefreshSelectGroupName();
                }
            }
        });

        public DelegateCommand DeleteRecipeCmd => _deleteRecipeCmd?? new DelegateCommand(() =>
        {
            // 这里可以放置你想要执行的逻辑
            // 例如，打开一个新的窗口，或者执行某个操作
            if (string.IsNullOrWhiteSpace(SelectRecipeName))
            {
                return; // 没有选中配方，不弹框
            }

            var result =  _dialogService.ShowDialogAsync(
            "ConfirmationDialog",
            new DialogParameters
            {
                { "Title", "删除确认" },
                { "Message", "确定要删除选中的用户吗？此操作不可撤销。" }
            });

            if (result.Result.Result == ButtonResult.Yes)
            {
                // 执行删除
                _recipeService.DeleteRangeAsync(p => p.GroupName == SelectGroupName && p.RecipeName == SelectRecipeName);
                RefreshSelectGroupName(); // 刷新列表
            }

        });

        public DelegateCommand ApplyRecipeCmd => _applyRecipeCmd?? new DelegateCommand(() =>
        {
            Logger.LogInformation("ApplyRecipeCmd");
            // 这里可以放置你想要执行的逻辑
            // 例如，打开一个新的窗口，或者执行某个操作
            if (string.IsNullOrWhiteSpace(SelectRecipeName))
            {
                return; // 没有选中配方，不弹框
            }

            //var result = _dialogService.ShowDialogAsync(
            //"ConfirmationDialog",
            //new DialogParameters
            //{
            //    { "Title", "切换配方确认" },
            //    { "Message", "确定要切换到选中的配方吗？此操作不可撤销。" }
            //});

            //if (result.Result.Result == ButtonResult.Yes)
            //{
                // 执行切换配方逻辑
            
                    var selectedRecipe = _recipeService.GetListAsync(p => p.GroupName == SelectGroupName && p.RecipeName == SelectRecipeName).Result.FirstOrDefault();
                    if (selectedRecipe != null)
                    {
                        IoC.Get<IRecipeManagerService>().UpdateRangeAsync( recipe =>true,o=>o.CurrentRecipeId=selectedRecipe.Id);
                        EventAggregator.GetEvent<ChangeRecipeEvent>().Publish(SelectRecipeName);
                    }
                
            //}   
        });


        #endregion

        #region 方法

        public override void EnterCommandExecute()
        {
            //Logger.LogInformation("RecipeHomeViewModel EnterCommandExecute");
            base.EnterCommandExecute();
            ApplyRecipeCmd.Execute();
            IoC.Get<INavigationService>().NavigateToAsync(ViewNames.RecipeContextView, RegionNames.MainRegion);
        }


        /// <summary>
        /// 刷新显示列表
        /// </summary>
        public void RefreshSelectGroupName()
        {

            var s = IoC.Get<IRecipeManagerService>().GetListAsync().Result;

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

        #endregion

        #region INavigationAware

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

        #endregion
    }
}
