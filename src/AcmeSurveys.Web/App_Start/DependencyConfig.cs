using System.Reflection;
using System.Web.Http;
using System.Web.Mvc;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Application.Auth;
using AcmeSurveys.Application.Responses;
using AcmeSurveys.Application.Results;
using AcmeSurveys.Application.Surveys;
using AcmeSurveys.Domain.FieldValues;
using AcmeSurveys.Infrastructure.Logging;
using AcmeSurveys.Infrastructure.Persistence;
using AcmeSurveys.Infrastructure.Security;
using AcmeSurveys.Infrastructure.Time;
using AcmeSurveys.Web.Common;
using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;

namespace AcmeSurveys.Web
{
    public static class DependencyConfig
    {
        public static IContainer Build(AppSettings settings)
        {
            var builder = new ContainerBuilder();
            var webAssembly = Assembly.GetExecutingAssembly();

            builder.RegisterControllers(webAssembly);
            builder.RegisterApiControllers(webAssembly);

            builder.RegisterType<CookieTempDataProvider>().As<ITempDataProvider>().SingleInstance();

            builder.RegisterInstance(settings);

            builder.RegisterType<SystemClock>().As<IClock>().SingleInstance();
            builder.RegisterType<CryptoRandomIdentifierGenerator>().As<IIdentifierGenerator>().SingleInstance();
            builder.RegisterType<TraceSecurityAuditLog>().As<ISecurityAuditLog>().SingleInstance();
            builder.Register(c => new Pbkdf2PasswordHasher(settings.PasswordHashIterations)).As<IPasswordHasher>().SingleInstance();
            builder.Register(c => new AesHmacFieldValueProtector(settings.DataProtectionKeyRing)).As<IFieldValueProtector>().SingleInstance();
            builder.Register(c => new JwtAccessTokenService(settings.Jwt, c.Resolve<IClock>())).As<IAccessTokenService>().SingleInstance();
            builder.Register(c => FieldValueHandlerRegistry.CreateDefault()).SingleInstance();
            builder.RegisterInstance(settings.Lockout);
            builder.Register(c => new SqlConnectionFactory(settings.ConnectionString)).SingleInstance();

            builder.RegisterType<UserRepository>().As<IUserRepository>().InstancePerRequest();
            builder.RegisterType<SurveyRepository>().As<ISurveyRepository>().InstancePerRequest();
            builder.RegisterType<ResponseRepository>().As<IResponseRepository>().InstancePerRequest();

            builder.RegisterType<AuthenticationService>().InstancePerRequest();
            builder.RegisterType<SurveyManagementService>().InstancePerRequest();
            builder.RegisterType<PublicSurveyService>().InstancePerRequest();
            builder.RegisterType<SurveyResultsService>().InstancePerRequest();

            var container = builder.Build();

            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            return container;
        }
    }
}
