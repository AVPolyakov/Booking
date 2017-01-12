using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using Ploeh.Samples.Booking.DomainModel;
using Ploeh.Samples.Booking.JsonAntiCorruption;
using Ploeh.Samples.Booking.Persistence.FileSystem;
using Ploeh.Samples.Booking.PersistenceModel;
using Ploeh.Samples.Booking.WebModel;

namespace Ploeh.Samples.Booking.WebUI
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            var namespaces = new[] { typeof(HomeController).Namespace };

            routes.MapRoute(
                name: "Post",
                url: "{Controller}/{id}",
                defaults: new { controller = "Home", action = "Post" },
                constraints: new { httpMethod = new HttpMethodConstraint("POST") },
                namespaces: namespaces);
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{id}",
                defaults: new { controller = "Home", action = "Get", id = UrlParameter.Optional },
                namespaces: namespaces);
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            ControllerBuilder.Current.SetControllerFactory(new CompositionRoot());
        }

        private class CompositionRoot : DefaultControllerFactory
        {
            private static readonly Dictionary<Type, Func<IController>> controllers = new[] {
                GetControllerTuple(() => new HomeController(FileMonthViewStore)),
                GetControllerTuple(() => new BookingController(JsonCapacityRepository, Channel<RequestReservationCommand>())),
                GetControllerTuple(() => new DisabledDatesController(FileMonthViewStore))
            }.ToDictionary(_ => _.Item1, _ => _.Item2);

            private static FileDateStore FileDateStore => new FileDateStore(SsotDirectory, Extension);
            private static JsonCapacityRepository JsonCapacityRepository => new JsonCapacityRepository(FileDateStore, FileDateStore, Quickenings);
            private static IChannel<T> Channel<T>() where T : IMessage => new JsonChannel<T>(StoreWriter<T>());
            private static IStoreWriter<T> StoreWriter<T>() where T : IMessage => new FileQueueWriter<T>(QueueDirectory, Extension);
            private static FileMonthViewStore FileMonthViewStore => new FileMonthViewStore(ViewStoreDirectory, Extension);
            private static string Extension => "txt";
            private static DirectoryInfo ViewStoreDirectory => new DirectoryInfo(HostingEnvironment.MapPath("~/Queue")).CreateIfAbsent();
            private static DirectoryInfo QueueDirectory => new DirectoryInfo(HostingEnvironment.MapPath("~/Queue")).CreateIfAbsent();
            private static DirectoryInfo SsotDirectory => new DirectoryInfo(HostingEnvironment.MapPath("~/SSoT")).CreateIfAbsent();
            private static IQuickening[] Quickenings => new IQuickening[] {
                new CapacityReservedEvent.Quickening(),
                new RequestReservationCommand.Quickening(),
                new ReservationAcceptedEvent.Quickening(),
                new ReservationRejectedEvent.Quickening(),
                new SoldOutEvent.Quickening()
            };

            protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
                => controllers[controllerType]();

            public static Tuple<Type, Func<IController>> GetControllerTuple<T>(Func<T> func) where T : IController
                => Tuple.Create<Type, Func<IController>>(typeof (T), () => func());
        }
    }
}