using System;
using System.Linq;
using Ploeh.Samples.Booking.Persistence.FileSystem;
using System.IO;
using Ploeh.Samples.Booking.PersistenceModel;
using System.Threading;
using Ploeh.Samples.Booking.DomainModel;
using System.Threading.Tasks;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace Ploeh.Samples.Booking.Daemon
{
    class Program
    {
        static void Main()
        {
            using (var container = new WindsorContainer().Install(new DaemonWindsorInstaller()))
            {
                var q = container.Resolve<QueueConsumer>();

                RunUntilStopped(q);
            }
        }

        #region Console stuff
        private static void RunUntilStopped(QueueConsumer q)
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            var task = Task.Factory.StartNew(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    q.ConsumeAll();
                    Thread.Sleep(500);
                }
            }, tokenSource.Token);

            Console.WriteLine("Type \"quit\" or \"exit\" to exit.");
            do
            {
                Console.Write("> ");
            } while (DoNotExit());

            tokenSource.Cancel();
        }

        private static bool DoNotExit()
        {
            var line = Console.ReadLine().ToUpperInvariant();
            return line != "QUIT"
                && line != "EXIT";
        }
        #endregion
    }

    public class DaemonWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.AddFacility<CommandHandlerConvention>();
            container.AddFacility<EventHandlerConvention>();

            container.Register(Component
                .For<IObserver<object>>()
                .ImplementedBy<CompositeObserver<object>>());

            container.Register(Classes
                .FromAssemblyInDirectory(new AssemblyFilter(".").FilterByName(an => an.Name.StartsWith("Ploeh.Samples.Booking")))
                .Where(Accepted)
                .WithServiceAllInterfaces());

            container.Kernel.Resolver.AddSubResolver(new ExtensionConvention());
            container.Kernel.Resolver.AddSubResolver(new DirectoryConvention(container.Kernel));
            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));

            #region Manual configuration that requires maintenance
            container.Register(Component
                .For<DirectoryInfo>()
                .UsingFactoryMethod(() =>
                    new DirectoryInfo(@"..\..\..\BookingWebUI\Queue").CreateIfAbsent())
                .Named("queueDirectory"));
            container.Register(Component
                .For<DirectoryInfo>()
                .UsingFactoryMethod(() =>
                    new DirectoryInfo(@"..\..\..\BookingWebUI\SSoT").CreateIfAbsent())
                .Named("ssotDirectory"));
            container.Register(Component
                .For<DirectoryInfo>()
                .UsingFactoryMethod(() =>
                    new DirectoryInfo(@"..\..\..\BookingWebUI\ViewStore").CreateIfAbsent())
                .Named("viewStoreDirectory"));
            #endregion

            GuardAgainstMismatchedCommands(container);
        }

        private static bool Accepted(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(EventDispatcher<>))
                return false;

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(CommandDispatcher<>))
                return false;

            return true;
        }

        private void GuardAgainstMismatchedCommands(IWindsorContainer container)
        {
            var handlerTypes = from h in container.Kernel.GetHandlers(typeof(IMessage))
                               where h.ComponentModel.Implementation.Name.EndsWith("Command")
                               select typeof(ICommandHandler<>).MakeGenericType(h.ComponentModel.Implementation);
            foreach (var h in handlerTypes)
            {
                var count = container.Kernel.GetHandlers(h).Count();
                if(count != 1)
                    throw new InvalidOperationException(string.Format("Exactly one implementation of {0} was expected, but {1} were found.", h, count));
            }
        }
    }

    public class CommandHandlerConvention : AbstractFacility
    {
        protected override void Init()
        {
            this.Kernel.HandlerRegistered += this.OnHandlerRegistered;
        }

        private void OnHandlerRegistered(IHandler handler, ref bool stateChanged)
        {
            var messageTypes = from t in handler.ComponentModel.Services
                               where t.IsInterface
                               && t.IsGenericType
                               && t.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                               select t.GetGenericArguments().Single();

            foreach (var t in messageTypes)
            {
                this.Kernel.Register(Component
                    .For<IObserver<object>>()
                    .ImplementedBy(typeof(CommandDispatcher<>).MakeGenericType(t)));
            }
        }
    }

    public class DirectoryConvention : ISubDependencyResolver
    {
        private readonly IKernel kernel;

        public DirectoryConvention(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public bool CanResolve(CreationContext context, ISubDependencyResolver contextHandlerResolver, ComponentModel model, DependencyModel dependency)
        {
            return dependency.TargetType == typeof(DirectoryInfo);
        }

        public object Resolve(CreationContext context, ISubDependencyResolver contextHandlerResolver, ComponentModel model, DependencyModel dependency)
        {
            return this.kernel.Resolve(dependency.DependencyKey, typeof(DirectoryInfo));
        }
    }

    public class EventHandlerConvention : AbstractFacility
    {
        protected override void Init()
        {
            this.Kernel.HandlerRegistered += this.OnHandlerRegistered;
        }

        private void OnHandlerRegistered(IHandler handler, ref bool stateChanged)
        {
            var messageTypes = from t in handler.ComponentModel.Services
                               where t.IsInterface
                               && t.IsGenericType
                               && t.GetGenericTypeDefinition() == typeof(IEventHandler<>)
                               select t.GetGenericArguments().Single();

            foreach (var t in messageTypes)
            {
                this.Kernel.Register(Component
                    .For<IObserver<object>>()
                    .ImplementedBy(typeof(EventDispatcher<>).MakeGenericType(t)));
            }
        }
    }

    public class ExtensionConvention : ISubDependencyResolver
    {
        public bool CanResolve(CreationContext context, ISubDependencyResolver contextHandlerResolver, ComponentModel model, DependencyModel dependency)
        {
            return dependency.TargetType == typeof(string)
                && dependency.DependencyKey == "extension";
        }

        public object Resolve(CreationContext context, ISubDependencyResolver contextHandlerResolver, ComponentModel model, DependencyModel dependency)
        {
            return "txt";
        }
    }
}