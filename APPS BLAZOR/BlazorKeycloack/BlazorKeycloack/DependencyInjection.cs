using BlazorKeycloack.Services.Layout;
using BlazorKeycloack.Services.UserPreferences;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MudBlazor.Services;
using System.Net.Http.Headers;

namespace BlazorKeycloack
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServerUI(this IServiceCollection services, IConfiguration config)
        {
            services.AddRazorComponents().AddInteractiveServerComponents().AddHubOptions(options => options.MaximumReceiveMessageSize = 64 * 1024);
            services.AddCascadingAuthenticationState();

            services.AddMudServices(config =>
            {
                MudGlobal.InputDefaults.ShrinkLabel = true;
                //MudGlobal.InputDefaults.Variant = Variant.Outlined;
                //MudGlobal.ButtonDefaults.Variant = Variant.Outlined;
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
                config.SnackbarConfiguration.NewestOnTop = false;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 3000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
                config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;


                // the project is set to treat warnings as errors.
#pragma warning disable 0618
                config.SnackbarConfiguration.PreventDuplicates = false;
#pragma warning restore 0618
            });
            services.AddMudPopoverService();
            services.AddMudBlazorSnackbar();
            services.AddMudBlazorDialog();
            services.AddScoped<LayoutService>().AddScoped<IUserPreferencesService, UserPreferencesService>();
            services.AddHttpClient("ocr", c =>
            {
                c.BaseAddress = new Uri("http://10.33.1.150:8000/ocr/predict-by-file");
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });


            services.AddControllers();






            return services;
        }
    }
}
