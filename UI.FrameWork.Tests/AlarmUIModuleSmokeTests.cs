using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Framework.Alarm;
using Framework.Alarm.Domain;
using Framework.Alarm.Infrastructure;
using Framework.Alarm.UI;
using SemiAppliaction.Views;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation.Regions;
using PrismUI.Core;
using Recipe.Infrastructure.Contracts;
using UI.FrameWork.Core.Main;

namespace UI.FrameWork.Tests
{
    /// <summary>
    /// 报警 UI 模块装载冒烟测试：按宿主同款容器（DryIoc 适配器 + 报警服务注册）走一遍
    /// AlarmUIModule 的 RegisterTypes + OnInitialized 接线。顶栏桥、引擎首构造、页头三件套
    /// 必须在容器里立得住——宿主登录后炸模块的问题在这里先红先修。
    /// </summary>
    public class AlarmUIModuleSmokeTests
    {
        /// <summary>与宿主 CreateContainerExtension 同款：DryIoc + DefaultRules + MS 适配器。</summary>
        private static (IContainerRegistry Registry, IContainerProvider Provider) BuildHostLikeContainer()
        {
            var services = new ServiceCollection();
            AddAlarmServicesLike(services);   // 与 AddAlarmServices 同形，库换成测试内存库
            services.AddAlarmEngine();

            var container = new DryIocContainerExtension(
                new DryIoc.Container(DryIocContainerExtension.DefaultRules)
                    .WithDependencyInjectionAdapter(services));

            // 宿主由 PrismApplicationBase 代注册的必需类型，测试自己补
            container.RegisterSingleton<IEventAggregator, EventAggregator>();
            // FrameworkModule / UIModule 的相关注册（与生产同款）
            container.RegisterSingleton<IRecipeService, StubRecipeService>();
            container.RegisterSingleton<INavigationService, StubNavigationService>();
            container.RegisterSingleton<HeaderViewModel>();
            container.RegisterSingleton<SimulatedConditionSource>();
            container.RegisterSingleton(typeof(IConditionSource), sp => sp.Resolve<SimulatedConditionSource>());
            return (container, container);
        }

        /// <summary>AddAlarmServices 的测试翻版：三个注册不变，选项换成开好连接、过了迁移的内存库。</summary>
        private static void AddAlarmServicesLike(IServiceCollection services)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open(); // 内存库随连接存活
            var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(connection).Options;
            using (var context = new DataContext(options))
            {
                context.Database.Migrate(); // 与生产同一条建表路径（AlarmDatabase.Initialize 也是 Migrate）
            }

            services.AddSingleton(_ => options);
            services.AddSingleton<IAlarmDefinitionStore, AlarmDefinitionStore>();
            services.AddSingleton<IEventStore, EventStore>();
        }

        /// <summary>模块装载全流程（RegisterTypes → OnInitialized）不炸，页头报警按钮亮出。</summary>
        [Fact]
        public void ModuleLoad_connects_bridge_and_lights_header()
        {
            var (registry, provider) = BuildHostLikeContainer();

            var module = new AlarmUIModule();
            module.RegisterTypes(registry); // 与 Prism 装载顺序一致：先注册后初始化
            module.OnInitialized(provider);

            var header = provider.Resolve<HeaderViewModel>();
            Assert.True(header.IsShowAlarm); // 桥接通 + 按钮亮出，两行接线都走通了
        }

        /// <summary>IRecipeService 测试替身：标题刷新拿 null 配方，走「未设置」分支。</summary>
        private sealed class StubRecipeService : IRecipeService
        {
            public Task<IReadOnlyList<RecipeListItemDto>> GetRecipeListAsync(string machineName, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<RecipeListItemDto>>([]);

            public Task<IReadOnlyList<string>> GetGroupListAsync(string machineName, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<string>>([]);

            public Task<RecipeListItemDto?> GetCurrentRecipeAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<RecipeListItemDto?>(null);

            public Task<bool> CreateGroupAsync(string groupName, string machineName, CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<bool> DeleteGroupAsync(string groupName, string machineName, CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<bool> CopyRecipeAsync(string groupName, string recipeName, string newRecipeName, string machineName, string? targetGroupName = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<bool> RenameRecipeAsync(string groupName, string recipeName, string newRecipeName, CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<bool> DeleteRecipeAsync(string groupName, string recipeName, CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<bool> SetCurrentRecipeAsync(int recipeId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        }

        /// <summary>INavigationService 测试替身：顶栏按钮的跳转在冒烟测试里不发生。</summary>
        private sealed class StubNavigationService : INavigationService
        {
            public Task NavigateToAsync(string viewName, string? regionName = null, NavigationParameters? parameters = null) => Task.CompletedTask;
            public Task<bool> ConfirmNavigationAsync() => Task.FromResult(true);
            public void GoBack(string? regionName = null) { }
            public void GoForward(string? regionName = null) { }
        }
    }
}
